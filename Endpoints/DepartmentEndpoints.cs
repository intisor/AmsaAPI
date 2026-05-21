using AmsaAPI.Data;
using AmsaAPI.DTOs;
using Microsoft.EntityFrameworkCore;

namespace AmsaAPI.Endpoints;

public static class DepartmentEndpoints
{
    public static void MapDepartmentEndpoints(this WebApplication app)
    {
        var deptGroup = app.MapGroup("/api/minimal/departments").WithTags("Departments (Minimal API)");

        // Get all departments
        deptGroup.MapGet("/", GetAllDepartments);
        
        // Get department by ID
        deptGroup.MapGet("/{id:int}", GetDepartmentById);
        
        // Create department
        deptGroup.MapPost("/", CreateDepartment);
        
        // Update department
        deptGroup.MapPut("/{id:int}", UpdateDepartment);
        
        // Delete department
        deptGroup.MapDelete("/{id:int}", DeleteDepartment);
    }

    private static async Task<IResult> GetAllDepartments(AmsaDbContext db)
    {
        try
        {
            var departments = await db.Database.SqlQueryRaw<DepartmentSummaryDto>("""
                SELECT d.DepartmentId, d.DepartmentName,
                       COUNT(mld.MemberLevelDepartmentId) as MemberCount
                FROM Departments d
                LEFT JOIN LevelDepartments ld ON d.DepartmentId = ld.DepartmentId
                LEFT JOIN MemberLevelDepartments mld ON ld.LevelDepartmentId = mld.LevelDepartmentId
                GROUP BY d.DepartmentId, d.DepartmentName
                ORDER BY d.DepartmentName
                """).ToListAsync();

            return Results.Ok(departments);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error retrieving departments: {ex.Message}");
        }
    }

    private static async Task<IResult> GetDepartmentById(int id, AmsaDbContext db)
    {
        try
        {
            // Get department basic info using raw SQL
            var department = await db.Database.SqlQueryRaw<DepartmentSummaryDto>("""
                SELECT d.DepartmentId, d.DepartmentName, 0 as MemberCount
                FROM Departments d
                WHERE d.DepartmentId = {0}
                """, id).FirstOrDefaultAsync();

            if (department == null)
                return Results.NotFound($"Department with ID {id} not found");

            // Get level information for this department
            var levels = await db.Database.SqlQueryRaw<DepartmentLevelDto>("""
                SELECT ld.LevelDepartmentId, l.LevelType,
                       COUNT(mld.MemberLevelDepartmentId) as MemberCount
                FROM LevelDepartments ld
                INNER JOIN Levels l ON ld.LevelId = l.LevelId
                LEFT JOIN MemberLevelDepartments mld ON ld.LevelDepartmentId = mld.LevelDepartmentId
                WHERE ld.DepartmentId = {0}
                GROUP BY ld.LevelDepartmentId, l.LevelType
                """, id).ToListAsync();

            var response = new DepartmentDetailResponse
            {
                DepartmentId = department.DepartmentId,
                DepartmentName = department.DepartmentName,
                Levels = levels
            };

            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error retrieving department: {ex.Message}");
        }
    }

    private static async Task<IResult> CreateDepartment(CreateDepartmentRequest request, AmsaDbContext db)
    {
        try
        {
            // Check if department name already exists using raw SQL
            var existingDept = await db.Database.SqlQueryRaw<DepartmentSummaryDto>("""
                SELECT d.DepartmentId, d.DepartmentName, 0 as MemberCount
                FROM Departments d
                WHERE d.DepartmentName = {0}
                """, request.DepartmentName).FirstOrDefaultAsync();

            if (existingDept != null)
                return Results.BadRequest($"Department '{request.DepartmentName}' already exists");

            var department = new Department { DepartmentName = request.DepartmentName };
            db.Departments.Add(department);
            await db.SaveChangesAsync();

            return Results.Created($"/api/minimal/departments/{department.DepartmentId}", 
                new
                {
                    department.DepartmentId,
                    department.DepartmentName
                });
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to create department: {ex.Message}");
        }
    }

    private static async Task<IResult> UpdateDepartment(int id, UpdateDepartmentRequest request, AmsaDbContext db)
    {
        try
        {
            // Get department using raw SQL
            var department = await db.Database.SqlQueryRaw<DepartmentSummaryDto>("""
                SELECT d.DepartmentId, d.DepartmentName, 0 as MemberCount
                FROM Departments d
                WHERE d.DepartmentId = {0}
                """, id).FirstOrDefaultAsync();

            if (department == null)
                return Results.NotFound($"Department with ID {id} not found");

            // Check if new name already exists (excluding current department)
            var existingDept = await db.Database.SqlQueryRaw<DepartmentSummaryDto>("""
                SELECT d.DepartmentId, d.DepartmentName, 0 as MemberCount
                FROM Departments d
                WHERE d.DepartmentName = {0} AND d.DepartmentId != {1}
                """, request.DepartmentName, id).FirstOrDefaultAsync();

            if (existingDept != null)
                return Results.BadRequest($"Department '{request.DepartmentName}' already exists");

            // Update using raw SQL
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE Departments
                SET DepartmentName = {request.DepartmentName}
                WHERE DepartmentId = {id}
                """);

            return Results.Ok(new
            {
                DepartmentId = id,
                DepartmentName = request.DepartmentName
            });
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to update department: {ex.Message}");
        }
    }

    private static async Task<IResult> DeleteDepartment(int id, AmsaDbContext db)
    {
        try
        {
            // Get department using raw SQL
            var department = await db.Database.SqlQueryRaw<DepartmentSummaryDto>("""
                SELECT d.DepartmentId, d.DepartmentName, 0 as MemberCount
                FROM Departments d
                WHERE d.DepartmentId = {0}
                """, id).FirstOrDefaultAsync();

            if (department == null)
                return Results.NotFound($"Department with ID {id} not found");

            // Check if department has associated level departments using raw SQL
            var hasLevelDepartments = await db.Database.SqlQueryRaw<int>("""
                SELECT COUNT(*) FROM LevelDepartments
                WHERE DepartmentId = {0}
                """, id).FirstOrDefaultAsync();

            if (hasLevelDepartments > 0)
                return Results.BadRequest("Cannot delete department with existing level assignments");

            // Delete using raw SQL
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                DELETE FROM Departments
                WHERE DepartmentId = {id}
                """);

            return Results.Ok(new
            {
                Message = $"Department '{department.DepartmentName}' deleted successfully"
            });
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to delete department: {ex.Message}");
        }
    }
}