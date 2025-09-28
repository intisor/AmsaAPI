using AmsaAPI.Common;
using AmsaAPI.Data;
using AmsaAPI.DTOs;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace AmsaAPI.FastEndpoints;

// Private record DTO for raw SQL projection - minimal and functional
file record DepartmentMemberCountDto(int DepartmentId, string DepartmentName, int MemberCount);

// Get All Departments Endpoint
public sealed class GetAllDepartmentsEndpoint(AmsaDbContext db) : Endpoint<EmptyRequest, List<DepartmentSummaryDto>>
{
    public override void Configure()
    {
        Get("/api/departments");
        AllowAnonymous();
        Summary(s => s.Summary = "Get all departments with member counts");
    }

    public override async Task HandleAsync(EmptyRequest req, CancellationToken ct)
    {
        // Single optimized raw SQL query with JOIN and GROUP BY
        var departmentsQuery = """
            SELECT 
                d.DepartmentId, 
                d.DepartmentName, 
                COUNT(DISTINCT mld.MemberLevelDepartmentId) as MemberCount
            FROM Departments d
            LEFT JOIN LevelDepartments ld ON d.DepartmentId = ld.DepartmentId  
            LEFT JOIN MemberLevelDepartments mld ON ld.LevelDepartmentId = mld.LevelDepartmentId
            GROUP BY d.DepartmentId, d.DepartmentName
            ORDER BY d.DepartmentName
            """;
        
        var departmentsRaw = await db.Database.SqlQueryRaw<DepartmentMemberCountDto>(departmentsQuery)
            .ToListAsync(ct);

        var departments = departmentsRaw.Select(d => new DepartmentSummaryDto
        {
            DepartmentId = d.DepartmentId,
            DepartmentName = d.DepartmentName,
            MemberCount = d.MemberCount
        }).ToList();

        await Send.OkAsync(departments, ct);
    }
}

// Get Department By ID Endpoint
public sealed class GetDepartmentByIdEndpoint(AmsaDbContext db) : Endpoint<GetDepartmentByIdRequest, DepartmentDetailResponse>
{
    public override void Configure()
    {
        Get("/api/departments/{id}");
        AllowAnonymous();
        Summary(s => s.Summary = "Get department details with levels");
    }

    public override async Task HandleAsync(GetDepartmentByIdRequest req, CancellationToken ct)
    {
        // Use Result pattern for input validation
        var validationResult = ValidateRequest(req);
        if (!validationResult.IsSuccess)
        {
            await Send.ResultAsync(Results.BadRequest(validationResult.ErrorMessage));
            return;
        }

        // Get department basic info
        var department = await db.Departments
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.DepartmentId == req.Id, ct);

        if (department == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        // Get level information for this department
        var levels = await db.LevelDepartments
            .Where(ld => ld.DepartmentId == req.Id)
            .Include(ld => ld.Level)
            .Include(ld => ld.MemberLevelDepartments)
            .AsNoTracking()
            .Select(d => new DepartmentLevelDto
            {
                LevelDepartmentId = d.LevelDepartmentId,
                LevelType = d.Level.LevelType,
                MemberCount = d.MemberLevelDepartments.Count()
            })
            .ToListAsync(ct);
        var response = new DepartmentDetailResponse
        {
            DepartmentId = department.DepartmentId,
            DepartmentName = department.DepartmentName,
            Levels = levels
        };

        await Send.OkAsync(response, ct);
    }

    private static Result<bool> ValidateRequest(GetDepartmentByIdRequest req)
    {
        if (req.Id <= 0)
            return Result.Validation<bool>("Invalid department ID. ID must be greater than 0.");
        
        return Result.Success(true);
    }
}