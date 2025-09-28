using AmsaAPI.Common;
using AmsaAPI.Data;
using AmsaAPI.DTOs;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace AmsaAPI.FastEndpoints;

// Private record DTOs for raw SQL projections - minimal and functional
file record UnitDetailRawDto(int UnitId, string UnitName, int StateId, string StateName, int NationalId, string NationalName, int MemberCount);
file record UnitMemberRawDto(int MemberId, string FirstName, string LastName, string? Email, string? Phone, int Mkanid);
file record UnitExcoRawDto(string FirstName, string LastName, int Mkanid, string DepartmentName, string LevelType);
file record NationalSummaryRawDto(int NationalId, string NationalName, int StateCount, int UnitCount, int MemberCount, int ExcoCount);

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
        // Use Result pattern for input validation
        var validationResult = OrganizationValidationMethods.ValidateUnitRequest(req);
        if (!validationResult.IsSuccess)
        {
            await Send.ResultAsync(Results.BadRequest(validationResult.ErrorMessage));
            return;
        }

        // Single comprehensive raw SQL query for unit details
        var unitDetailQuery = """
            SELECT u.UnitId, u.UnitName, u.StateId, s.StateName, s.NationalId, n.NationalName,
                   (SELECT COUNT(*) FROM Members m WHERE m.UnitId = u.UnitId) as MemberCount
            FROM Units u
            INNER JOIN States s ON u.StateId = s.StateId
            INNER JOIN Nationals n ON s.NationalId = n.NationalId
            WHERE u.UnitId = {0}
            """;
        
        var unitDetails = await db.Database.SqlQueryRaw<UnitDetailRawDto>(unitDetailQuery, req.Id)
            .FirstOrDefaultAsync(ct);

        if (unitDetails == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        // Get members for this unit with raw SQL
        var membersQuery = """
            SELECT MemberId, FirstName, LastName, Email, Phone, Mkanid
            FROM Members
            WHERE UnitId = {0}
            ORDER BY FirstName, LastName
            """;
        
        var membersRaw = await db.Database.SqlQueryRaw<UnitMemberRawDto>(membersQuery, req.Id)
            .ToListAsync(ct);

        // Get EXCO roles for this unit with raw SQL
        var excoQuery = """
            SELECT m.FirstName, m.LastName, m.Mkanid, d.DepartmentName, l.LevelType
            FROM MemberLevelDepartments mld
            INNER JOIN Members m ON mld.MemberId = m.MemberId
            INNER JOIN LevelDepartments ld ON mld.LevelDepartmentId = ld.LevelDepartmentId
            INNER JOIN Departments d ON ld.DepartmentId = d.DepartmentId
            INNER JOIN Levels l ON ld.LevelId = l.LevelId
            WHERE l.UnitId = {0}
            """;
        
        var excoRaw = await db.Database.SqlQueryRaw<UnitExcoRawDto>(excoQuery, req.Id)
            .ToListAsync(ct);

        // Map raw SQL results to DTOs
        var unit = new UnitDetailDto
        {
            UnitId = unitDetails.UnitId,
            UnitName = unitDetails.UnitName,
            StateId = unitDetails.StateId,
            StateName = unitDetails.StateName,
            NationalId = unitDetails.NationalId,
            NationalName = unitDetails.NationalName,
            MemberCount = unitDetails.MemberCount
        };

        var members = membersRaw.Select(m => new UnitMemberDto
        {
            MemberId = m.MemberId,
            FirstName = m.FirstName,
            LastName = m.LastName,
            Email = m.Email,
            Phone = m.Phone,
            Mkanid = m.Mkanid
        }).ToList();

        var excoMembers = excoRaw.Select(e => new UnitExcoDto
        {
            FirstName = e.FirstName,
            LastName = e.LastName,
            Mkanid = e.Mkanid,
            DepartmentName = e.DepartmentName,
            LevelType = e.LevelType
        }).ToList();

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
        // Use Result pattern for input validation
        var validationResult = OrganizationValidationMethods.ValidateStateRequest(req);
        if (!validationResult.IsSuccess)
        {
            await Send.ResultAsync(Results.BadRequest(validationResult.ErrorMessage));
            return;
        }

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
        // Use Result pattern for input validation
        var validationResult = OrganizationValidationMethods.ValidateStateIdRequest(req);
        if (!validationResult.IsSuccess)
        {
            await Send.ResultAsync(Results.BadRequest(validationResult.ErrorMessage));
            return;
        }

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

        var state = await query.FirstOrDefaultAsync(ct);
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
        // Single aggregated raw SQL query for all nationals with counts
        var nationalsQuery = """
            SELECT 
                n.NationalId,
                n.NationalName,
                (SELECT COUNT(*) FROM States s WHERE s.NationalId = n.NationalId) as StateCount,
                (SELECT COUNT(*) FROM Units u INNER JOIN States s ON u.StateId = s.StateId WHERE s.NationalId = n.NationalId) as UnitCount,
                (SELECT COUNT(*) FROM Members m INNER JOIN Units u ON m.UnitId = u.UnitId INNER JOIN States s ON u.StateId = s.StateId WHERE s.NationalId = n.NationalId) as MemberCount,
                (SELECT COUNT(*) FROM MemberLevelDepartments mld 
                 INNER JOIN LevelDepartments ld ON mld.LevelDepartmentId = ld.LevelDepartmentId 
                 INNER JOIN Levels l ON ld.LevelId = l.LevelId 
                 WHERE l.NationalId = n.NationalId) as ExcoCount
            FROM Nationals n
            ORDER BY n.NationalName
            """;
        
        var nationalsRaw = await db.Database.SqlQueryRaw<NationalSummaryRawDto>(nationalsQuery)
            .ToListAsync(ct);

        var nationals = nationalsRaw.Select(n => new NationalSummaryDto
        {
            NationalId = n.NationalId,
            NationalName = n.NationalName,
            StateCount = n.StateCount,
            UnitCount = n.UnitCount,
            MemberCount = n.MemberCount,
            ExcoCount = n.ExcoCount
        }).ToList();

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
        // Use Result pattern for input validation
        var validationResult = OrganizationValidationMethods.ValidateNationalRequest(req);
        if (!validationResult.IsSuccess)
        {
            await Send.ResultAsync(Results.BadRequest(validationResult.ErrorMessage));
            return;
        }

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

// Validation methods for OrganizationFastEndpoints
public static class OrganizationValidationMethods
{
    public static Result<bool> ValidateUnitRequest(GetUnitByIdRequest req)
    {
        if (req.Id <= 0)
            return Result.Validation<bool>("Invalid unit ID. ID must be greater than 0.");
        
        return Result.Success(true);
    }

    public static Result<bool> ValidateStateRequest(GetUnitsByStateRequest req)
    {
        if (req.StateId <= 0)
            return Result.Validation<bool>("Invalid state ID. ID must be greater than 0.");
        
        return Result.Success(true);
    }

    public static Result<bool> ValidateStateIdRequest(GetStateByIdRequest req)
    {
        if (req.Id <= 0)
            return Result.Validation<bool>("Invalid state ID. ID must be greater than 0.");
        
        return Result.Success(true);
    }

    public static Result<bool> ValidateNationalRequest(GetNationalByIdRequest req)
    {
        if (req.Id <= 0)
            return Result.Validation<bool>("Invalid national ID. ID must be greater than 0.");
        
        return Result.Success(true);
    }
}