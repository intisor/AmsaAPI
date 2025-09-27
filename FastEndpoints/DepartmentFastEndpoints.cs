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
        try
        {
            var departments = await db.Departments
                .Select(d => new DepartmentSummaryDto
                {
                    DepartmentId = d.DepartmentId,
                    DepartmentName = d.DepartmentName,
                    MemberCount = db.MemberLevelDepartments.Count(mld => d.LevelDepartments.Select(ld => ld.LevelDepartmentId).Contains(mld.LevelDepartmentId))
                })
                .OrderBy(d => d.DepartmentName)
                .ToListAsync(ct);

            await Send.OkAsync(departments, ct);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            await Send.ResultAsync(Results.Problem("Failed to retrieve departments: " + ex.Message));
        }
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
        try
        {
            if (req.Id <= 0)
            {
                await Send.ResultAsync(Results.BadRequest("Invalid department ID. ID must be greater than 0."));
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
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            await Send.ResultAsync(Results.Problem($"Failed to retrieve department with ID {req.Id}: {ex.Message}"));
        }
    }
}