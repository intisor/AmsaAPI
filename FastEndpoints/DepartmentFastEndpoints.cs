using AmsaAPI.Data;
using AmsaAPI.DTOs;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace AmsaAPI.FastEndpoints;

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
        var departments = await db.Database.SqlQueryRaw<DepartmentSummaryDto>("""
            SELECT d.DepartmentId, d.DepartmentName,
                   COUNT(mld.MemberLevelDepartmentId) as MemberCount
            FROM Departments d
            LEFT JOIN LevelDepartments ld ON d.DepartmentId = ld.DepartmentId
            LEFT JOIN MemberLevelDepartments mld ON ld.LevelDepartmentId = mld.LevelDepartmentId
            GROUP BY d.DepartmentId, d.DepartmentName
            ORDER BY d.DepartmentName
            """).ToListAsync(ct);

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
        var levels = await db.Database.SqlQueryRaw<DepartmentLevelDto>("""
            SELECT ld.LevelDepartmentId, l.LevelType,
                   CASE 
                       WHEN l.NationalId IS NOT NULL THEN 'National'
                       WHEN l.StateId IS NOT NULL THEN 'State'
                       WHEN l.UnitId IS NOT NULL THEN 'Unit'
                       ELSE 'Unknown'
                   END as Scope,
                   COUNT(mld.MemberLevelDepartmentId) as MemberCount
            FROM LevelDepartments ld
            INNER JOIN Levels l ON ld.LevelId = l.LevelId
            LEFT JOIN MemberLevelDepartments mld ON ld.LevelDepartmentId = mld.LevelDepartmentId
            WHERE ld.DepartmentId = {0}
            GROUP BY ld.LevelDepartmentId, l.LevelType, l.NationalId, l.StateId, l.UnitId
            """, req.Id).ToListAsync(ct);

        var response = new DepartmentDetailResponse
        {
            DepartmentId = department.DepartmentId,
            DepartmentName = department.DepartmentName,
            Levels = levels
        };

        await Send.OkAsync(response, ct);
    }
}