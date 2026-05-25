using AmsaAPI.Data;
using AmsaAPI.DTOs;
using Microsoft.EntityFrameworkCore;

namespace AmsaAPI.Endpoints;

public static class StatisticsEndpoints
{
    public static void MapStatisticsEndpoints(this WebApplication app)
    {
        var statsGroup = app.MapGroup("/api/minimal/stats").WithTags("Statistics (Minimal API)");

        // Get dashboard statistics
        statsGroup.MapGet("/dashboard", GetDashboardStats);

        // Get unit statistics
        statsGroup.MapGet("/units", GetUnitStats);

        // Get department statistics
        statsGroup.MapGet("/departments", GetDepartmentStats);

        // Get organization summary
        statsGroup.MapGet("/organization-summary", GetOrganizationSummary);
    }

    private static async Task<IResult> GetDashboardStats(AmsaDbContext db)
    {
        try
        {
            var counts = await db.Database.SqlQueryRaw<DashboardStatsCountsDto>("""
                SELECT
                    (SELECT COUNT(*) FROM Members) as TotalMembers,
                    (SELECT COUNT(*) FROM Units) as TotalUnits,
                    (SELECT COUNT(*) FROM Departments) as TotalDepartments,
                    (SELECT COUNT(*) FROM States) as TotalStates,
                    (SELECT COUNT(*) FROM Nationals) as TotalNationals,
                    (SELECT COUNT(*) FROM Levels) as TotalLevels,
                    (SELECT COUNT(*) FROM MemberLevelDepartments) as ExcoMembers,
                    (SELECT COUNT(*) FROM Levels WHERE NationalId IS NOT NULL) as NationalExcoCount,
                    (SELECT COUNT(*) FROM Levels WHERE StateId IS NOT NULL) as StateExcoCount,
                    (SELECT COUNT(*) FROM Levels WHERE UnitId IS NOT NULL) as UnitExcoCount
                """).FirstOrDefaultAsync() ?? new DashboardStatsCountsDto();

            var recentMembers = await db.Database.SqlQueryRaw<RecentMemberDto>("""
                SELECT TOP 5 FirstName, LastName, Mkanid
                FROM Members
                ORDER BY MemberId DESC
                """).ToListAsync();

            var response = new DashboardStatsResponse
            {
                TotalMembers = counts.TotalMembers,
                TotalUnits = counts.TotalUnits,
                TotalDepartments = counts.TotalDepartments,
                TotalStates = counts.TotalStates,
                TotalNationals = counts.TotalNationals,
                TotalLevels = counts.TotalLevels,
                ExcoMembers = counts.ExcoMembers,
                RecentMembers = recentMembers,
                NationalExcoCount = counts.NationalExcoCount,
                StateExcoCount = counts.StateExcoCount,
                UnitExcoCount = counts.UnitExcoCount
            };

            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error retrieving dashboard statistics: {ex.Message}");
        }
    }

    private static async Task<IResult> GetUnitStats(AmsaDbContext db)
    {
        try
        {
            var unitStats = await db.Database.SqlQueryRaw<UnitStatsDto>("""
                SELECT u.UnitId, u.UnitName, s.StateName,
                       COUNT(DISTINCT m.MemberId) as MemberCount,
                       COUNT(DISTINCT mld.MemberLevelDepartmentId) as ExcoCount
                FROM Units u
                INNER JOIN States s ON u.StateId = s.StateId
                LEFT JOIN Members m ON u.UnitId = m.UnitId
                LEFT JOIN Levels l ON u.UnitId = l.UnitId
                LEFT JOIN LevelDepartments ld ON l.LevelId = ld.LevelId
                LEFT JOIN MemberLevelDepartments mld ON ld.LevelDepartmentId = mld.LevelDepartmentId
                GROUP BY u.UnitId, u.UnitName, s.StateName
                """).ToListAsync();

            return Results.Ok(unitStats);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error retrieving unit statistics: {ex.Message}");
        }
    }

    private static async Task<IResult> GetDepartmentStats(AmsaDbContext db)
    {
        try
        {
            var deptStats = await db.Database.SqlQueryRaw<DepartmentStatsDto>("""
                SELECT d.DepartmentId, d.DepartmentName,
                       COUNT(mld.MemberLevelDepartmentId) as TotalMemberCount,
                       COUNT(CASE WHEN l.NationalId IS NOT NULL THEN mld.MemberLevelDepartmentId END) as NationalCount,
                       COUNT(CASE WHEN l.StateId IS NOT NULL THEN mld.MemberLevelDepartmentId END) as StateCount,
                       COUNT(CASE WHEN l.UnitId IS NOT NULL THEN mld.MemberLevelDepartmentId END) as UnitCount
                FROM Departments d
                LEFT JOIN LevelDepartments ld ON d.DepartmentId = ld.DepartmentId
                LEFT JOIN Levels l ON ld.LevelId = l.LevelId
                LEFT JOIN MemberLevelDepartments mld ON ld.LevelDepartmentId = mld.LevelDepartmentId
                GROUP BY d.DepartmentId, d.DepartmentName
                ORDER BY COUNT(mld.MemberLevelDepartmentId) DESC
                """).ToListAsync();

            return Results.Ok(deptStats);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error retrieving department statistics: {ex.Message}");
        }
    }

    private static async Task<IResult> GetOrganizationSummary(AmsaDbContext db)
    {
        try
        {
            var overview = await db.Database.SqlQueryRaw<OverviewDto>("""
                SELECT
                    (SELECT COUNT(*) FROM Nationals) as TotalNationals,
                    (SELECT COUNT(*) FROM States) as TotalStates,
                    (SELECT COUNT(*) FROM Units) as TotalUnits,
                    (SELECT COUNT(*) FROM Members) as TotalMembers,
                    (SELECT COUNT(*) FROM Departments) as TotalDepartments,
                    (SELECT COUNT(*) FROM Levels) as TotalLevels,
                    (SELECT COUNT(*) FROM MemberLevelDepartments) as TotalExcoPositions
                """).FirstOrDefaultAsync() ?? new OverviewDto();

            var topUnits = await db.Database.SqlQueryRaw<TopUnitDto>("""
                SELECT TOP 10 u.UnitName, s.StateName as State, COUNT(m.MemberId) as MemberCount
                FROM Units u
                INNER JOIN States s ON u.StateId = s.StateId
                LEFT JOIN Members m ON u.UnitId = m.UnitId
                GROUP BY u.UnitName, s.StateName
                ORDER BY COUNT(m.MemberId) DESC
                """).ToListAsync();

            var topDepartments = await db.Database.SqlQueryRaw<TopDepartmentDto>("""
                SELECT TOP 10 d.DepartmentName, COUNT(mld.MemberLevelDepartmentId) as MemberCount
                FROM Departments d
                LEFT JOIN LevelDepartments ld ON d.DepartmentId = ld.DepartmentId
                LEFT JOIN MemberLevelDepartments mld ON ld.LevelDepartmentId = mld.LevelDepartmentId
                GROUP BY d.DepartmentName
                ORDER BY COUNT(mld.MemberLevelDepartmentId) DESC
                """).ToListAsync();

            var excoBreakdown = await db.Database.SqlQueryRaw<ExcoBreakdownDto>("""
                SELECT
                    COUNT(CASE WHEN l.NationalId IS NOT NULL THEN 1 END) as NationalExco,
                    COUNT(CASE WHEN l.StateId IS NOT NULL THEN 1 END) as StateExco,
                    COUNT(CASE WHEN l.UnitId IS NOT NULL THEN 1 END) as UnitExco
                FROM MemberLevelDepartments mld
                INNER JOIN LevelDepartments ld ON mld.LevelDepartmentId = ld.LevelDepartmentId
                INNER JOIN Levels l ON ld.LevelId = l.LevelId
                """).FirstOrDefaultAsync() ?? new ExcoBreakdownDto();

            var response = new OrganizationSummaryResponse
            {
                Overview = overview,
                ExcoBreakdown = excoBreakdown,
                TopUnits = topUnits,
                TopDepartments = topDepartments
            };

            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error retrieving organization summary: {ex.Message}");
        }
    }
}