using AmsaAPI.Data;
using AmsaAPI.DTOs;
using Microsoft.EntityFrameworkCore;

namespace AmsaAPI.Endpoints;

public static class OrganizationEndpoints
{
    public static void MapOrganizationEndpoints(this WebApplication app)
    {
        // Unit endpoints
        var unitGroup = app.MapGroup("/api/minimal/units").WithTags("Units (Minimal API)");
        unitGroup.MapGet("/", GetAllUnits);
        unitGroup.MapGet("/{id:int}", GetUnitById);
        unitGroup.MapGet("/state/{stateId:int}", GetUnitsByState);
        unitGroup.MapPost("/", CreateUnit);
        unitGroup.MapPut("/{id:int}", UpdateUnit);
        unitGroup.MapDelete("/{id:int}", DeleteUnit);

        // State endpoints
        var stateGroup = app.MapGroup("/api/minimal/states").WithTags("States (Minimal API)");
        stateGroup.MapGet("/", GetAllStates);
        stateGroup.MapGet("/{id:int}", GetStateById);
        stateGroup.MapPost("/", CreateState);

        // National endpoints
        var nationalGroup = app.MapGroup("/api/minimal/nationals").WithTags("Nationals (Minimal API)");
        nationalGroup.MapGet("/", GetAllNationals);
        nationalGroup.MapGet("/{id:int}", GetNationalById);

        // Hierarchy endpoint
        app.MapGet("/api/minimal/hierarchy", GetHierarchy).WithTags("Organization (Minimal API)");
    }

    #region Unit Endpoints

    private static async Task<IResult> GetAllUnits(AmsaDbContext db)
    {
        try
        {
            var units = await db.Database.SqlQueryRaw<UnitSummaryDto>("""
                SELECT u.UnitId, u.UnitName, u.StateId, s.StateName, n.NationalName,
                       COUNT(m.MemberId) as MemberCount
                FROM Units u
                INNER JOIN States s ON u.StateId = s.StateId
                INNER JOIN Nationals n ON s.NationalId = n.NationalId
                LEFT JOIN Members m ON u.UnitId = m.UnitId
                GROUP BY u.UnitId, u.UnitName, u.StateId, s.StateName, n.NationalName
                ORDER BY s.StateName, u.UnitName
                """).ToListAsync();

            return Results.Ok(units);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error retrieving units: {ex.Message}");
        }
    }

    private static async Task<IResult> GetUnitById(int id, AmsaDbContext db)
    {
        try
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
                """, id).ToListAsync();

            var unitDetail = unit.FirstOrDefault();
            if (unitDetail == null)
                return Results.NotFound($"Unit with ID {id} not found");

            // Get members for this unit
            var members = await db.Database.SqlQueryRaw<UnitMemberDto>("""
                SELECT MemberId, FirstName, LastName, Email, Phone, Mkanid
                FROM Members
                WHERE UnitId = {0}
                ORDER BY FirstName, LastName
                """, id).ToListAsync();

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
                """, id).ToListAsync();

            var response = new UnitDetailResponse
            {
                Unit = unitDetail,
                Members = members,
                ExcoRoles = excoRoles
            };

            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error retrieving unit: {ex.Message}");
        }
    }

    private static async Task<IResult> GetUnitsByState(int stateId, AmsaDbContext db)
    {
        try
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
                """, stateId).ToListAsync();

            return Results.Ok(units);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error retrieving units by state: {ex.Message}");
        }
    }

    private static async Task<IResult> CreateUnit(Unit unit, AmsaDbContext db)
    {
        try
        {
            // Validate StateId exists
            var stateExists = await db.States
                .AsNoTracking()
                .AnyAsync(s => s.StateId == unit.StateId);

            if (!stateExists)
                return Results.BadRequest($"State with ID {unit.StateId} does not exist");

            db.Units.Add(unit);
            await db.SaveChangesAsync();
            
            return Results.Created($"/api/minimal/units/{unit.UnitId}", 
                new { unit.UnitId, unit.UnitName, unit.StateId });
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to create unit: {ex.Message}");
        }
    }

    private static async Task<IResult> UpdateUnit(int id, Unit updatedUnit, AmsaDbContext db)
    {
        try
        {
            var unit = await db.Units.FindAsync(id);
            if (unit == null)
                return Results.NotFound($"Unit with ID {id} not found");

            // Validate StateId exists if changed
            if (unit.StateId != updatedUnit.StateId)
            {
                var stateExists = await db.States
                    .AsNoTracking()
                    .AnyAsync(s => s.StateId == updatedUnit.StateId);

                if (!stateExists)
                    return Results.BadRequest($"State with ID {updatedUnit.StateId} does not exist");
            }

            unit.UnitName = updatedUnit.UnitName;
            unit.StateId = updatedUnit.StateId;
            await db.SaveChangesAsync();
            
            return Results.Ok(new { unit.UnitId, unit.UnitName, unit.StateId });
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to update unit: {ex.Message}");
        }
    }

    private static async Task<IResult> DeleteUnit(int id, AmsaDbContext db)
    {
        try
        {
            var unit = await db.Units.FindAsync(id);
            if (unit == null)
                return Results.NotFound($"Unit with ID {id} not found");

            // Check if unit has members
            var hasMembers = await db.Members
                .AsNoTracking()
                .AnyAsync(m => m.UnitId == id);

            if (hasMembers)
                return Results.BadRequest("Cannot delete unit with existing members");

            db.Units.Remove(unit);
            await db.SaveChangesAsync();
            
            return Results.Ok(new { Message = $"Unit '{unit.UnitName}' deleted successfully" });
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to delete unit: {ex.Message}");
        }
    }

    #endregion

    #region State Endpoints

    private static async Task<IResult> GetAllStates(AmsaDbContext db)
    {
        try
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
                """).ToListAsync();

            return Results.Ok(states);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error retrieving states: {ex.Message}");
        }
    }

    private static async Task<IResult> GetStateById(int id, AmsaDbContext db)
    {
        try
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
                """, id).ToListAsync();

            var stateDetail = state.FirstOrDefault();
            if (stateDetail == null)
                return Results.NotFound($"State with ID {id} not found");

            return Results.Ok(stateDetail);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error retrieving state: {ex.Message}");
        }
    }

    private static async Task<IResult> CreateState(State state, AmsaDbContext db)
    {
        try
        {
            // Validate NationalId exists
            var nationalExists = await db.Nationals
                .AsNoTracking()
                .AnyAsync(n => n.NationalId == state.NationalId);

            if (!nationalExists)
                return Results.BadRequest($"National with ID {state.NationalId} does not exist");

            // Check if state name already exists
            var existingState = await db.States
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.StateName == state.StateName);

            if (existingState != null)
                return Results.BadRequest($"State '{state.StateName}' already exists");

            db.States.Add(state);
            await db.SaveChangesAsync();
            
            return Results.Created($"/api/minimal/states/{state.StateId}", 
                new { state.StateId, state.StateName, state.NationalId });
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to create state: {ex.Message}");
        }
    }

    #endregion

    #region National Endpoints

    private static async Task<IResult> GetAllNationals(AmsaDbContext db)
    {
        try
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
                """).ToListAsync();

            return Results.Ok(nationals);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error retrieving nationals: {ex.Message}");
        }
    }

    private static async Task<IResult> GetNationalById(int id, AmsaDbContext db)
    {
        try
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
                """, id).ToListAsync();

            var nationalDetail = national.FirstOrDefault();
            if (nationalDetail == null)
                return Results.NotFound($"National with ID {id} not found");

            return Results.Ok(nationalDetail);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error retrieving national: {ex.Message}");
        }
    }

    #endregion

    #region Hierarchy

    private static async Task<IResult> GetHierarchy(AmsaDbContext db)
    {
        try
        {
            var hierarchy = await db.Database.SqlQueryRaw<HierarchyDto>("""
                SELECT n.NationalId, n.NationalName,
                       s.StateId, s.StateName,
                       u.UnitId, u.UnitName,
                       COUNT(m.MemberId) as MemberCount
                FROM Nationals n
                LEFT JOIN States s ON n.NationalId = s.NationalId
                LEFT JOIN Units u ON s.StateId = u.StateId
                LEFT JOIN Members m ON u.UnitId = m.UnitId
                GROUP BY n.NationalId, n.NationalName, s.StateId, s.StateName, u.UnitId, u.UnitName
                ORDER BY n.NationalName, s.StateName, u.UnitName
                """).ToListAsync();

            return Results.Ok(hierarchy);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error retrieving hierarchy: {ex.Message}");
        }
    }

    #endregion
}