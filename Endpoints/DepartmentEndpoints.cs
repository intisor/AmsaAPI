using AmsaAPI.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace AmsaAPI.Endpoints
{
    public static class DepartmentEndpoints
    {
        public static void MapDepartmentEndpoints(this WebApplication app)
        {
            var deptGroup = app.MapGroup("/api/departments").WithTags("Departments");

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
                // Get department basic info
                var department = await db.Departments
                    .AsNoTracking()
                    .FirstOrDefaultAsync(d => d.DepartmentId == id);

                if (department == null)
                    return Results.NotFound($"Department with ID {id} not found");

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

        private static async Task<IResult> CreateDepartment(Department department, AmsaDbContext db)
        {
            try
            {
                // Check if department name already exists
                var existingDept = await db.Departments
                    .AsNoTracking()
                    .FirstOrDefaultAsync(d => d.DepartmentName == department.DepartmentName);

                if (existingDept != null)
                    return Results.BadRequest($"Department '{department.DepartmentName}' already exists");

                db.Departments.Add(department);
                await db.SaveChangesAsync();
                
                return Results.Created($"/api/departments/{department.DepartmentId}", 
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

        private static async Task<IResult> UpdateDepartment(int id, Department updatedDept, AmsaDbContext db)
        {
            try
            {
                var department = await db.Departments.FindAsync(id);
                if (department == null)
                    return Results.NotFound($"Department with ID {id} not found");

                // Check if new name already exists (excluding current department)
                var existingDept = await db.Departments
                    .AsNoTracking()
                    .FirstOrDefaultAsync(d => d.DepartmentName == updatedDept.DepartmentName && d.DepartmentId != id);

                if (existingDept != null)
                    return Results.BadRequest($"Department '{updatedDept.DepartmentName}' already exists");

                department.DepartmentName = updatedDept.DepartmentName;
                await db.SaveChangesAsync();
                
                return Results.Ok(new
                {
                    department.DepartmentId,
                    department.DepartmentName
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
                var department = await db.Departments.FindAsync(id);
                if (department == null)
                    return Results.NotFound($"Department with ID {id} not found");

                // Check if department has associated level departments
                var hasLevelDepartments = await db.LevelDepartments
                    .AsNoTracking()
                    .AnyAsync(ld => ld.DepartmentId == id);

                if (hasLevelDepartments)
                    return Results.BadRequest("Cannot delete department with existing level assignments");

                db.Departments.Remove(department);
                await db.SaveChangesAsync();
                
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

    // Helper DTOs for department queries
    public class DepartmentSummaryDto
    {
        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public int MemberCount { get; set; }
    }

    public class DepartmentLevelDto
    {
        public int LevelDepartmentId { get; set; }
        public string LevelType { get; set; } = string.Empty;
        public string Scope { get; set; } = string.Empty;
        public int MemberCount { get; set; }
    }

    public class DepartmentDetailResponse
    {
        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public List<DepartmentLevelDto> Levels { get; set; } = new();
    }
}