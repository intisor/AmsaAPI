using AmsaAPI.Common;
using AmsaAPI.Data;
using AmsaAPI.DTOs;
using AmsaAPI.Extensions;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace AmsaAPI.FastEndpoints;

// Private record DTOs for raw SQL projections - minimal and functional
file record MemberDetailRawDto(int MemberId, string FirstName, string LastName, string? Email, string? Phone, int Mkanid, int UnitId, string UnitName, int StateId, string StateName, int NationalId, string NationalName, string? DepartmentName, string? LevelType);
file record MemberSummaryRawDto(int MemberId, string FirstName, string LastName, string? Email, string? Phone, int Mkanid);

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
        // Single flattened raw SQL query to get all member data
        var membersQuery = """
            SELECT m.MemberId, m.FirstName, m.LastName, m.Email, m.Phone, m.Mkanid,
                   u.UnitId, u.UnitName, s.StateId, s.StateName, n.NationalId, n.NationalName,
                   d.DepartmentName, l.LevelType
            FROM Members m
            INNER JOIN Units u ON m.UnitId = u.UnitId
            INNER JOIN States s ON u.StateId = s.StateId  
            INNER JOIN National n ON s.NationalId = n.NationalId
            LEFT JOIN MemberLevelDepartments mld ON m.MemberId = mld.MemberId
            LEFT JOIN LevelDepartments ld ON mld.LevelDepartmentId = ld.LevelDepartmentId
            LEFT JOIN Departments d ON ld.DepartmentId = d.DepartmentId
            LEFT JOIN Levels l ON ld.LevelId = l.LevelId
            ORDER BY m.MemberId
            """;
        
        var membersRaw = await db.Database.SqlQueryRaw<MemberDetailRawDto>(membersQuery)
            .ToListAsync(ct);

        // Group results by MemberId and build response structure
        var memberGroups = membersRaw.GroupBy(m => m.MemberId);
        var response = new List<MemberDetailResponse>();

        foreach (var group in memberGroups)
        {
            var firstMember = group.First();
            var roles = group.Where(m => m.DepartmentName != null)
                .Select(m => new DepartmentAtLevelDto
                {
                    DepartmentName = m.DepartmentName!,
                    LevelType = m.LevelType!
                }).ToList();

            response.Add(new MemberDetailResponse
            {
                MemberId = firstMember.MemberId,
                FirstName = firstMember.FirstName,
                LastName = firstMember.LastName,
                Email = firstMember.Email,
                Phone = firstMember.Phone,
                Mkanid = firstMember.Mkanid,
                Unit = new UnitHierarchyDto
                {
                    UnitId = firstMember.UnitId,
                    UnitName = firstMember.UnitName,
                    State = new StateHierarchyDto
                    {
                        StateId = firstMember.StateId,
                        StateName = firstMember.StateName,
                        National = new NationalDto
                        {
                            NationalId = firstMember.NationalId,
                            NationalName = firstMember.NationalName
                        }
                    }
                },
                Roles = roles
            });
        }

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

        // Single comprehensive raw SQL query for member with roles
        var memberQuery = """
            SELECT m.MemberId, m.FirstName, m.LastName, m.Email, m.Phone, m.Mkanid,
                   u.UnitId, u.UnitName, s.StateId, s.StateName, n.NationalId, n.NationalName,
                   d.DepartmentName, l.LevelType
            FROM Members m
            INNER JOIN Units u ON m.UnitId = u.UnitId
            INNER JOIN States s ON u.StateId = s.StateId  
            INNER JOIN National n ON s.NationalId = n.NationalId
            LEFT JOIN MemberLevelDepartments mld ON m.MemberId = mld.MemberId
            LEFT JOIN LevelDepartments ld ON mld.LevelDepartmentId = ld.LevelDepartmentId
            LEFT JOIN Departments d ON ld.DepartmentId = d.DepartmentId
            LEFT JOIN Levels l ON ld.LevelId = l.LevelId
            WHERE m.MemberId = {0}
            ORDER BY m.MemberId
            """;
        
        var memberRaw = await db.Database.SqlQueryRaw<MemberDetailRawDto>(memberQuery, req.Id)
            .ToListAsync(ct);

        if (!memberRaw.Any())
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var firstMember = memberRaw.First();
        var roles = memberRaw.Where(m => m.DepartmentName != null)
            .Select(m => new DepartmentAtLevelDto
            {
                DepartmentName = m.DepartmentName!,
                LevelType = m.LevelType!
            }).ToList();

        var response = new MemberDetailResponse
        {
            MemberId = firstMember.MemberId,
            FirstName = firstMember.FirstName,
            LastName = firstMember.LastName,
            Email = firstMember.Email,
            Phone = firstMember.Phone,
            Mkanid = firstMember.Mkanid,
            Unit = new UnitHierarchyDto
            {
                UnitId = firstMember.UnitId,
                UnitName = firstMember.UnitName,
                State = new StateHierarchyDto
                {
                    StateId = firstMember.StateId,
                    StateName = firstMember.StateName,
                    National = new NationalDto
                    {
                        NationalId = firstMember.NationalId,
                        NationalName = firstMember.NationalName
                    }
                }
            },
            Roles = roles
        };

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
        // Use Result pattern for input validation
        var validationResult = MemberValidationMethods.ValidateMkanIdRequest(req);
        if (!validationResult.IsSuccess)
        {
            await Send.ResultAsync(Results.BadRequest(validationResult.ErrorMessage));
            return;
        }

        // Single comprehensive raw SQL query for member by MKANID
        var memberQuery = """
            SELECT m.MemberId, m.FirstName, m.LastName, m.Email, m.Phone, m.Mkanid,
                   u.UnitId, u.UnitName, s.StateId, s.StateName, n.NationalId, n.NationalName,
                   d.DepartmentName, l.LevelType
            FROM Members m
            INNER JOIN Units u ON m.UnitId = u.UnitId
            INNER JOIN States s ON u.StateId = s.StateId  
            INNER JOIN National n ON s.NationalId = n.NationalId
            LEFT JOIN MemberLevelDepartments mld ON m.MemberId = mld.MemberId
            LEFT JOIN LevelDepartments ld ON mld.LevelDepartmentId = ld.LevelDepartmentId
            LEFT JOIN Departments d ON ld.DepartmentId = d.DepartmentId
            LEFT JOIN Levels l ON ld.LevelId = l.LevelId
            WHERE m.Mkanid = {0}
            ORDER BY m.MemberId
            """;
        
        var memberRaw = await db.Database.SqlQueryRaw<MemberDetailRawDto>(memberQuery, req.MkanId)
            .ToListAsync(ct);

        if (!memberRaw.Any())
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var firstMember = memberRaw.First();
        var roles = memberRaw.Where(m => m.DepartmentName != null)
            .Select(m => new DepartmentAtLevelDto
            {
                DepartmentName = m.DepartmentName!,
                LevelType = m.LevelType!
            }).ToList();

        var response = new MemberDetailResponse
        {
            MemberId = firstMember.MemberId,
            FirstName = firstMember.FirstName,
            LastName = firstMember.LastName,
            Email = firstMember.Email,
            Phone = firstMember.Phone,
            Mkanid = firstMember.Mkanid,
            Unit = new UnitHierarchyDto
            {
                UnitId = firstMember.UnitId,
                UnitName = firstMember.UnitName,
                State = new StateHierarchyDto
                {
                    StateId = firstMember.StateId,
                    StateName = firstMember.StateName,
                    National = new NationalDto
                    {
                        NationalId = firstMember.NationalId,
                        NationalName = firstMember.NationalName
                    }
                }
            },
            Roles = roles
        };

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
        // Use Result pattern for input validation
        var validationResult = MemberValidationMethods.ValidateUnitIdRequest(req);
        if (!validationResult.IsSuccess)
        {
            await Send.ResultAsync(Results.BadRequest(validationResult.ErrorMessage));
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
        // Use Result pattern for input validation
        var validationResult = MemberValidationMethods.ValidateDepartmentIdRequest(req);
        if (!validationResult.IsSuccess)
        {
            await Send.ResultAsync(Results.BadRequest(validationResult.ErrorMessage));
            return;
        }

        // Direct JOIN-based raw SQL query to eliminate Any() operation
        var membersQuery = """
            SELECT DISTINCT m.MemberId, m.FirstName, m.LastName, m.Email, m.Phone, m.Mkanid
            FROM Members m
            INNER JOIN MemberLevelDepartments mld ON m.MemberId = mld.MemberId
            INNER JOIN LevelDepartments ld ON mld.LevelDepartmentId = ld.LevelDepartmentId
            WHERE ld.DepartmentId = {0}
            ORDER BY m.FirstName, m.LastName
            """;
        
        var membersRaw = await db.Database.SqlQueryRaw<MemberSummaryRawDto>(membersQuery, req.DepartmentId)
            .ToListAsync(ct);

        var members = membersRaw.Select(m => new MemberSummaryResponse
        {
            MemberId = m.MemberId,
            FirstName = m.FirstName,
            LastName = m.LastName,
            Email = m.Email,
            Phone = m.Phone,
            Mkanid = m.Mkanid
        }).ToList();

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

// Static validation class for Member endpoints
public static class MemberValidationMethods
{
    public static Result<bool> ValidateMkanIdRequest(GetMemberByMkanIdRequest req)
    {
        if (req.MkanId <= 0)
            return Result.Validation<bool>("Invalid MKAN ID. ID must be greater than 0.");
        
        return Result.Success(true);
    }

    public static Result<bool> ValidateUnitIdRequest(GetMembersByUnitRequest req)
    {
        if (req.UnitId <= 0)
            return Result.Validation<bool>("Invalid unit ID. ID must be greater than 0.");
        
        return Result.Success(true);
    }

    public static Result<bool> ValidateDepartmentIdRequest(GetMembersByDepartmentRequest req)
    {
        if (req.DepartmentId <= 0)
            return Result.Validation<bool>("Invalid department ID. ID must be greater than 0.");
        
        return Result.Success(true);
    }
}