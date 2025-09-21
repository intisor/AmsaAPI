using AmsaAPI.Data;
using AmsaAPI.DTOs;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace AmsaAPI.FastEndpoints;

// Get All Units Endpoint
public sealed class GetAllUnitsEndpoint(AmsaDbContext db) : Endpoint<EmptyRequest, List<UnitSummaryDto>>
{
    public override void Configure()
    {
        Get("/api/units");
        AllowAnonymous();
        Summary(s => s.Summary = "Get all units with summary information");
    }

    public override async Task HandleAsync(EmptyRequest req, CancellationToken ct)
    {

        var units = await db.Units
            .AsNoTracking()
            .Select(u => new UnitSummaryDto
            {
                UnitId = u.UnitId,
                UnitName = u.UnitName,
                StateId = u.StateId,
                StateName = u.State.StateName,
                NationalName = u.State.National.NationalName,
                MemberCount = u.Members.Count()
            })
            .ToListAsync(ct);
        await Send.OkAsync(units, ct);
    }
}

// Get Unit By ID Endpoint
public sealed class GetUnitByIdEndpoint(AmsaDbContext db) : Endpoint<GetUnitByIdRequest, UnitDetailResponse>
{
    public override void Configure()
    {
        Get("/api/units/{id}");
        AllowAnonymous();
        Summary(s => s.Summary = "Get unit details with members and EXCO roles");
    }

    public override async Task HandleAsync(GetUnitByIdRequest req, CancellationToken ct)
    {
        var unit = await db.Database.SqlQueryRaw<UnitDetailDto>("""
            SELECT u.UnitId, u.UnitName, s.StateId, s.StateName, 
                   n.NationalId, n.NationalName,
                   COUNT(DISTINCT m.MemberId) as MemberCount
            FROM Units u
            INNER JOIN States s ON u.StateId = s.StateId
            INNER JOIN Nationals n ON s.NationalId = n.NationalId
            LEFT JOIN Members m ON u.UnitId = m.UnitId
            WHERE u.UnitId = {0}
            GROUP BY u.UnitId, u.UnitName, s.StateId, s.StateName, n.NationalId, n.NationalName
            """, req.Id).ToListAsync(ct);

        var unitDetail = unit.FirstOrDefault();
        if (unitDetail == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        // Get members for this unit
        var members = await db.Database.SqlQueryRaw<UnitMemberDto>("""
            SELECT MemberId, FirstName, LastName, Email, Phone, Mkanid
            FROM Members
            WHERE UnitId = {0}
            ORDER BY FirstName, LastName
            """, req.Id).ToListAsync(ct);

        // Get EXCO roles for this unit
        var excoRoles = await db.Database.SqlQueryRaw<UnitExcoDto>("""
            SELECT m.FirstName, m.LastName, m.Mkanid, d.DepartmentName, l.LevelType
            FROM Members m
            INNER JOIN MemberLevelDepartments mld ON m.MemberId = mld.MemberId
            INNER JOIN LevelDepartments ld ON mld.LevelDepartmentId = ld.LevelDepartmentId
            INNER JOIN Departments d ON ld.DepartmentId = d.DepartmentId
            INNER JOIN Levels lv ON ld.LevelId = lv.LevelId
            WHERE lv.UnitId = {0}
            ORDER BY d.DepartmentName
            """, req.Id).ToListAsync(ct);

        var response = new UnitDetailResponse
        {
            Unit = unitDetail,
            Members = members,
            ExcoRoles = excoRoles
        };

        await Send.OkAsync(response, ct);
    }
}

// Get Units By State Endpoint
public sealed class GetUnitsByStateEndpoint(AmsaDbContext db) : Endpoint<GetUnitsByStateRequest, List<UnitStateDto>>
{
    public override void Configure()
    {
        Get("/api/units/state/{stateid}");
        AllowAnonymous();
        Summary(s => s.Summary = "Get all units in a specific state");
    }

    public override async Task HandleAsync(GetUnitsByStateRequest req, CancellationToken ct)
    {
        var units = await db.Database.SqlQueryRaw<UnitStateDto>("""
            SELECT u.UnitId, u.UnitName, 
                   COUNT(DISTINCT m.MemberId) as MemberCount,
                   COUNT(DISTINCT mld.MemberLevelDepartmentId) as ExcoCount
            FROM Units u
            LEFT JOIN Members m ON u.UnitId = m.UnitId
            LEFT JOIN Levels l ON u.UnitId = l.UnitId
            LEFT JOIN LevelDepartments ld ON l.LevelId = ld.LevelId
            LEFT JOIN MemberLevelDepartments mld ON ld.LevelDepartmentId = mld.LevelDepartmentId
            WHERE u.StateId = {0}
            GROUP BY u.UnitId, u.UnitName
            ORDER BY u.UnitName
            """, req.StateId).ToListAsync(ct);

        await Send.OkAsync(units, ct);
    }
}

// Get All States Endpoint
public sealed class GetAllStatesEndpoint(AmsaDbContext db) : Endpoint<EmptyRequest, List<StateSummaryDto>>
{
    public override void Configure()
    {
        Get("/api/states");
        AllowAnonymous();
        Summary(s => s.Summary = "Get all states with summary statistics");
    }

    public override async Task HandleAsync(EmptyRequest req, CancellationToken ct)
    {
        var states = await db.Database.SqlQueryRaw<StateSummaryDto>("""
            SELECT s.StateId, s.StateName, n.NationalName,
                   COUNT(DISTINCT u.UnitId) as UnitCount,
                   COUNT(DISTINCT m.MemberId) as MemberCount,
                   COUNT(DISTINCT mld.MemberLevelDepartmentId) as ExcoCount
            FROM States s
            INNER JOIN Nationals n ON s.NationalId = n.NationalId
            LEFT JOIN Units u ON s.StateId = u.StateId
            LEFT JOIN Members m ON u.UnitId = m.UnitId
            LEFT JOIN Levels l ON s.StateId = l.StateId
            LEFT JOIN LevelDepartments ld ON l.LevelId = ld.LevelId
            LEFT JOIN MemberLevelDepartments mld ON ld.LevelDepartmentId = mld.LevelDepartmentId
            GROUP BY s.StateId, s.StateName, n.NationalName
            ORDER BY s.StateName
            """).ToListAsync(ct);

        await Send.OkAsync(states, ct);
    }
}

// Get State By ID Endpoint
public sealed class GetStateByIdEndpoint(AmsaDbContext db) : Endpoint<GetStateByIdRequest, StateDetailDto>
{
    public override void Configure()
    {
        Get("/api/states/{id}");
        AllowAnonymous();
        Summary(s => s.Summary = "Get state details with statistics");
    }

    public override async Task HandleAsync(GetStateByIdRequest req, CancellationToken ct)
    {
        var state = await db.Database.SqlQueryRaw<StateDetailDto>("""
            SELECT s.StateId, s.StateName, n.NationalId, n.NationalName,
                   COUNT(DISTINCT u.UnitId) as UnitCount,
                   COUNT(DISTINCT m.MemberId) as MemberCount
            FROM States s
            INNER JOIN Nationals n ON s.NationalId = n.NationalId
            LEFT JOIN Units u ON s.StateId = u.StateId
            LEFT JOIN Members m ON u.UnitId = m.UnitId
            WHERE s.StateId = {0}
            GROUP BY s.StateId, s.StateName, n.NationalId, n.NationalName
            """, req.Id).ToListAsync(ct);

        var stateDetail = state.FirstOrDefault();
        if (stateDetail == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.OkAsync(stateDetail, ct);
    }
}

// Get All Nationals Endpoint
public sealed class GetAllNationalsEndpoint(AmsaDbContext db) : Endpoint<EmptyRequest, List<NationalSummaryDto>>
{
    public override void Configure()
    {
        Get("/api/nationals");
        AllowAnonymous();
        Summary(s => s.Summary = "Get all nationals with full statistics");
    }

    public override async Task HandleAsync(EmptyRequest req, CancellationToken ct)
    {
        var nationals = await db.Database.SqlQueryRaw<NationalSummaryDto>("""
            SELECT n.NationalId, n.NationalName,
                   COUNT(DISTINCT s.StateId) as StateCount,
                   COUNT(DISTINCT u.UnitId) as UnitCount,
                   COUNT(DISTINCT m.MemberId) as MemberCount,
                   COUNT(DISTINCT mld.MemberLevelDepartmentId) as ExcoCount
            FROM Nationals n
            LEFT JOIN States s ON n.NationalId = s.NationalId
            LEFT JOIN Units u ON s.StateId = u.StateId
            LEFT JOIN Members m ON u.UnitId = m.UnitId
            LEFT JOIN Levels l ON n.NationalId = l.NationalId
            LEFT JOIN LevelDepartments ld ON l.LevelId = ld.LevelId
            LEFT JOIN MemberLevelDepartments mld ON ld.LevelDepartmentId = mld.LevelDepartmentId
            GROUP BY n.NationalId, n.NationalName
            ORDER BY n.NationalName
            """).ToListAsync(ct);

        await Send.OkAsync(nationals, ct);
    }
}

// Get National By ID Endpoint
public sealed class GetNationalByIdEndpoint(AmsaDbContext db) : Endpoint<GetNationalByIdRequest, NationalDetailDto>
{
    public override void Configure()
    {
        Get("/api/nationals/{id}");
        AllowAnonymous();
        Summary(s => s.Summary = "Get national details with statistics");
    }

    public override async Task HandleAsync(GetNationalByIdRequest req, CancellationToken ct)
    {
        var national = await db.Database.SqlQueryRaw<NationalDetailDto>("""
            SELECT n.NationalId, n.NationalName,
                   COUNT(DISTINCT s.StateId) as StateCount,
                   COUNT(DISTINCT u.UnitId) as UnitCount,
                   COUNT(DISTINCT m.MemberId) as MemberCount
            FROM Nationals n
            LEFT JOIN States s ON n.NationalId = s.NationalId
            LEFT JOIN Units u ON s.StateId = u.StateId
            LEFT JOIN Members m ON u.UnitId = m.UnitId
            WHERE n.NationalId = {0}
            GROUP BY n.NationalId, n.NationalName
            """, req.Id).ToListAsync(ct);

        var nationalDetail = national.FirstOrDefault();
        if (nationalDetail == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.OkAsync(nationalDetail, ct);
    }
}