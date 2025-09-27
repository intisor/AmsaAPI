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
        var query = db.Units
            .AsNoTracking()
            .Include(u => u.State)
                .ThenInclude(s => s.National)
            .Include(u => u.Members)
            .Where(u => u.UnitId == req.Id)
            .Select(u => new UnitDetailDto
            {
                UnitId = u.UnitId,
                UnitName = u.UnitName,
                StateId = u.State.StateId,
                StateName = u.State.StateName,
                NationalId = u.State.National.NationalId,
                NationalName = u.State.National.NationalName,
                MemberCount = u.Members.Count()
            });
        var unit = await query.FirstOrDefaultAsync(ct);


        if (unit == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        // Get members for this unit
        var members = await db.Members
            .AsNoTracking()
            .Where(m => m.UnitId == req.Id)
            .Select(m => new UnitMemberDto
            {
                MemberId = m.MemberId,
                FirstName = m.FirstName,
                LastName = m.LastName,
                Email = m.Email,
                Phone = m.Phone,
                Mkanid = m.Mkanid
            })
            .OrderBy(m => m.FirstName).ThenBy(m => m.LastName)
            .ToListAsync(ct);

        // Get EXCO roles for this unit
        var excoQuery = db.MemberLevelDepartments
            .AsNoTracking()
            .Where(mld => mld.LevelDepartment.Level.UnitId == req.Id)
            .Select(mld => new UnitExcoDto
            {
                FirstName = mld.Member.FirstName,
                LastName = mld.Member.LastName,
                Mkanid = mld.Member.Mkanid,
                DepartmentName = mld.LevelDepartment.Department.DepartmentName,
                LevelType = mld.LevelDepartment.Level.LevelType
            });

        var excoMembers = await excoQuery.ToListAsync(ct);
        var response = new UnitDetailResponse
        {
            Unit = unit,
            Members = members,
            ExcoRoles = excoMembers
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

        var query = db.Units
            .AsNoTracking()
            .Where(u => u.StateId == req.StateId)
            .Select(u => new UnitStateDto
            {
                UnitId = u.UnitId,
                UnitName = u.UnitName,
                MemberCount = u.Members.Count(),
                ExcoCount = db.MemberLevelDepartments
                              .Count(mld => mld.LevelDepartment.Level.UnitId == u.UnitId)
            })
            .OrderBy(u => u.UnitName);      
        var units = await query.ToListAsync(ct);

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
        var query = db.States
            .AsNoTracking()
            .Select(s => new StateSummaryDto
            {
                StateId = s.StateId,
                StateName = s.StateName,
                NationalName = s.National.NationalName,
                UnitCount = s.Units.Count(),
                MemberCount = db.Members.Count(m => m.Unit.StateId == s.StateId),
                ExcoCount = db.MemberLevelDepartments
                              .Count(mld => mld.LevelDepartment.Level.StateId == s.StateId)
            })
            .OrderBy(s => s.StateId); 
        var states = await query.ToListAsync(ct);

        await Send.OkAsync(states, ct);
    }
}

// Get State By ID Endpoint
public sealed class GetStateByIdEndpoint(AmsaDbContext db) : Endpoint<GetStateByIdRequest, StateSummaryDto>
{
    public override void Configure()
    {
        Get("/api/states/{id}");
        AllowAnonymous();
        Summary(s => s.Summary = "Get state details with statistics");
    }

    public override async Task HandleAsync(GetStateByIdRequest req, CancellationToken ct)
    {
        var query = db.States
            .AsNoTracking()
            .Where(s => s.StateId == req.Id)
            .Select(s => new StateSummaryDto
            {
                StateId = s.StateId,
                StateName = s.StateName,
                NationalName = s.National.NationalName,
                UnitCount = s.Units.Count(),
                MemberCount = db.Members.Count(m => m.Unit.StateId == s.StateId),
                ExcoCount = db.MemberLevelDepartments
                              .Count(mld => mld.LevelDepartment.Level.StateId == s.StateId)
            });

        var state = await query.FirstOrDefaultAsync();
        if (state == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.OkAsync(state, ct);
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
        var query = db.Nationals
            .AsNoTracking()
            .Select(n => new NationalSummaryDto
            {
                NationalId = n.NationalId,
                NationalName = n.NationalName,
                StateCount = n.States.Count(),
                UnitCount = n.States.SelectMany(s => s.Units).Count(),
                MemberCount = db.Members.Count(m => m.Unit.State.NationalId == n.NationalId),
                ExcoCount = db.MemberLevelDepartments
                              .Count(mld => mld.LevelDepartment.Level.NationalId == n.NationalId)
            });
        var nationals = await query.ToListAsync(ct);

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
        var query = db.Nationals
            .AsNoTracking()
            .Where(n => n.NationalId == req.Id)
            .Select(n => new NationalDetailDto
            {
                NationalId = n.NationalId,
                NationalName = n.NationalName,
                StateCount = n.States.Count(),
                UnitCount = n.States.SelectMany(s => s.Units).Count(),
                MemberCount = db.Members.Count(m => m.Unit.State.NationalId == n.NationalId)
            });

        var nationalDetail = await query.FirstOrDefaultAsync(ct);
        if (nationalDetail == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.OkAsync(nationalDetail, ct);
    }
}