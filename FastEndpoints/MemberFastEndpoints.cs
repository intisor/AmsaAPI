using AmsaAPI.Common;
using AmsaAPI.Data;
using AmsaAPI.DTOs;
using AmsaAPI.Extensions;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace AmsaAPI.FastEndpoints;

// Get All Members Endpoint
public sealed class GetAllMembersEndpoint(AmsaDbContext db) : Endpoint<EmptyRequest, List<MemberDetailResponse>>
{
    public override void Configure()
    {
        Get("/api/members");
        AllowAnonymous();
        Summary(s => s.Summary = "Get all members with complete details");
    }

    public override async Task HandleAsync(EmptyRequest req, CancellationToken ct)
    {
        var membersWithAllData = await db.Members
            .Include(m => m.Unit.State.National)
            .Include(m => m.MemberLevelDepartments)
                .ThenInclude(mld => mld.LevelDepartment.Department)
            .Include(m => m.MemberLevelDepartments)
                .ThenInclude(mld => mld.LevelDepartment.Level)
            .AsNoTracking()
            .ToListAsync(ct);

        var response = membersWithAllData.Select(member =>
            member.ToDetailResponseWithRoles(member.MemberLevelDepartments.ToList())
        ).ToList();

        await Send.OkAsync(response, ct);
    }
}

// Get Member By ID Endpoint - Uses Result pattern for validation only
public sealed class GetMemberByIdEndpoint(AmsaDbContext db) : Endpoint<GetMemberByIdRequest, MemberDetailResponse>
{
    public override void Configure()
    {
        Get("/api/members/{id}");
        AllowAnonymous();
        Summary(s => s.Summary = "Get member by ID with full details");
    }

    public override async Task HandleAsync(GetMemberByIdRequest req, CancellationToken ct)
    {
        // Use Result pattern for input validation
        var validationResult = ValidateRequest(req);
        if (!validationResult.IsSuccess)
        {
            await Send.ResultAsync(Results.BadRequest(validationResult.ErrorMessage));
            return;
        }

        // Use default FastEndpoints methods for business logic
        var member = await db.Members
            .Include(m => m.Unit.State.National)
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.MemberId == req.Id, ct);

        if (member == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var rolesData = await db.MemberLevelDepartments
            .Where(mld => mld.MemberId == req.Id)
            .Include(mld => mld.LevelDepartment.Department)
            .Include(mld => mld.LevelDepartment.Level)
            .AsNoTracking()
            .ToListAsync(ct);

        var response = member.ToDetailResponseWithRoles(rolesData);
        await Send.OkAsync(response, ct);
    }

    private static Result<bool> ValidateRequest(GetMemberByIdRequest req)
    {
        if (req.Id <= 0)
            return Result.Validation<bool>("Invalid member ID. ID must be greater than 0.");
        
        return Result.Success(true);
    }
}

// Get Member By MKAN ID Endpoint
public sealed class GetMemberByMkanIdEndpoint(AmsaDbContext db) : Endpoint<GetMemberByMkanIdRequest, MemberDetailResponse>
{
    public override void Configure()
    {
        Get("/api/members/mkan/{mkanid}");
        AllowAnonymous();
        Summary(s => s.Summary = "Get member by MKAN ID with full details");
    }

    public override async Task HandleAsync(GetMemberByMkanIdRequest req, CancellationToken ct)
    {
        // Simple validation with Result pattern
        if (req.MkanId <= 0)
        {
            await Send.ResultAsync(Results.BadRequest("Invalid MKAN ID. ID must be greater than 0."));
            return;
        }

        // Default FastEndpoints methods for business logic
        var member = await db.Members
            .Include(m => m.Unit.State.National)
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Mkanid == req.MkanId, ct);

        if (member == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var rolesData = await db.MemberLevelDepartments
            .Where(mld => mld.MemberId == member.MemberId)
            .Include(mld => mld.LevelDepartment.Department)
            .Include(mld => mld.LevelDepartment.Level)
            .AsNoTracking()
            .ToListAsync(ct);

        var response = member.ToDetailResponseWithRoles(rolesData);
        await Send.OkAsync(response, ct);
    }
}

// Get Members By Unit Endpoint
public sealed class GetMembersByUnitEndpoint(AmsaDbContext db) : Endpoint<GetMembersByUnitRequest, List<MemberSummaryResponse>>
{
    public override void Configure()
    {
        Get("/api/members/unit/{unitid}");
        AllowAnonymous();
        Summary(s => s.Summary = "Get all members in a specific unit");
    }

    public override async Task HandleAsync(GetMembersByUnitRequest req, CancellationToken ct)
    {
        // Simple validation
        if (req.UnitId <= 0)
        {
            await Send.ResultAsync(Results.BadRequest("Invalid unit ID. ID must be greater than 0."));
            return;
        }

        var members = await db.Members
            .Where(m => m.UnitId == req.UnitId)
            .Select(m => new MemberSummaryResponse
            {
                MemberId = m.MemberId,
                FirstName = m.FirstName,
                LastName = m.LastName,
                Email = m.Email,
                Phone = m.Phone,
                Mkanid = m.Mkanid
            })
            .ToListAsync(ct);

        // Use FastEndpoints default methods - empty list is still OK response
        await Send.OkAsync(members, ct);
    }
}

// Get Members By Department Endpoint
public sealed class GetMembersByDepartmentEndpoint(AmsaDbContext db) : Endpoint<GetMembersByDepartmentRequest, List<MemberSummaryResponse>>
{
    public override void Configure()
    {
        Get("/api/members/department/{departmentid}");
        AllowAnonymous();
        Summary(s => s.Summary = "Get all members in a specific department");
    }

    public override async Task HandleAsync(GetMembersByDepartmentRequest req, CancellationToken ct)
    {
        // Simple validation
        if (req.DepartmentId <= 0)
        {
            await Send.ResultAsync(Results.BadRequest("Invalid department ID. ID must be greater than 0."));
            return;
        }

        var members = await db.Members
            .Where(m => m.MemberLevelDepartments.Any(mld =>
                mld.LevelDepartment.DepartmentId == req.DepartmentId))
            .Select(m => new MemberSummaryResponse
            {
                MemberId = m.MemberId,
                FirstName = m.FirstName,
                LastName = m.LastName,
                Email = m.Email,
                Phone = m.Phone,
                Mkanid = m.Mkanid
            })
            .ToListAsync(ct);

        await Send.OkAsync(members, ct);
    }
}

// Search Members By Name Endpoint - Uses Result pattern for complex validation
public sealed class SearchMembersByNameEndpoint(AmsaDbContext db) : Endpoint<SearchMembersByNameRequest, List<MemberSummaryResponse>>
{
    public override void Configure()
    {
        Get("/api/members/search/{name}");
        AllowAnonymous();
        Summary(s => s.Summary = "Search members by first or last name");
    }

    public override async Task HandleAsync(SearchMembersByNameRequest req, CancellationToken ct)
    {
        // Use Result pattern for complex validation rules
        var validationResult = ValidateSearchRequest(req);
        if (!validationResult.IsSuccess)
        {
            await Send.ResultAsync(Results.BadRequest(validationResult.ErrorMessage));
            return;
        }

        var members = await db.Members
            .Where(m => m.FirstName.Contains(req.Name) || m.LastName.Contains(req.Name))
            .Select(m => new MemberSummaryResponse
            {
                MemberId = m.MemberId,
                FirstName = m.FirstName,
                LastName = m.LastName,
                Email = m.Email,
                Phone = m.Phone,
                Mkanid = m.Mkanid
            })
            .ToListAsync(ct);

        await Send.OkAsync(members, ct);
    }

    private static Result<bool> ValidateSearchRequest(SearchMembersByNameRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
            return Result.Validation<bool>("Search name cannot be empty.");
        
        if (req.Name.Length < 2)
            return Result.Validation<bool>("Search name must be at least 2 characters long.");
        
        if (req.Name.Length > 50)
            return Result.Validation<bool>("Search name cannot exceed 50 characters.");
        
        return Result.Success(true);
    }
}