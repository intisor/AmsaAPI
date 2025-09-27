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
            // Get recent members
            var recentMembers = await db.Members
                .OrderByDescending(m => m.MemberId)
                .Take(5)
                .Select(m => new RecentMemberDto
                {
                    FirstName = m.FirstName,
                    LastName = m.LastName,
                    Mkanid = m.Mkanid
                })
                .ToListAsync();

            var response = new DashboardStatsResponse
            {
                TotalMembers = await db.Members.CountAsync(),
                TotalUnits = await db.Units.CountAsync(),
                TotalDepartments = await db.Departments.CountAsync(),
                TotalStates = await db.States.CountAsync(),
                TotalNationals = await db.Nationals.CountAsync(),
                TotalLevels = await db.Levels.CountAsync(),
                ExcoMembers = await db.MemberLevelDepartments.CountAsync(),
                RecentMembers = recentMembers,
                NationalExcoCount = await db.Levels
                    .Where(l => l.NationalId != null)
                    .SelectMany(l => l.LevelDepartments)
                    .SelectMany(ld => ld.MemberLevelDepartments)
                    .CountAsync(),
                StateExcoCount = await db.Levels
                    .Where(l => l.StateId != null)
                    .SelectMany(l => l.LevelDepartments)
                    .SelectMany(ld => ld.MemberLevelDepartments)
                    .CountAsync(),
                UnitExcoCount = await db.Levels
                    .Where(l => l.UnitId != null)
                    .SelectMany(l => l.LevelDepartments)
                    .SelectMany(ld => ld.MemberLevelDepartments)
                    .CountAsync()
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
            var unitStats = await db.Database
                .SqlQueryRaw<UnitStatsDto>("SELECT u.UnitId, u.UnitName, COUNT(m.MemberId) as MemberCount FROM Units u LEFT JOIN Members m ON u.UnitId = m.UnitId GROUP BY u.UnitId, u.UnitName")
                .ToListAsync();

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
            // Get top units
            var topUnits = await db.Database.SqlQueryRaw<TopUnitDto>("""
                SELECT TOP 10 u.UnitName, s.StateName as State, COUNT(m.MemberId) as MemberCount
                FROM Units u
                INNER JOIN States s ON u.StateId = s.StateId
                LEFT JOIN Members m ON u.UnitId = m.UnitId
                GROUP BY u.UnitName, s.StateName
                ORDER BY COUNT(m.MemberId) DESC
                """).ToListAsync();

            // Get top departments
            var topDepartments = await db.Database.SqlQueryRaw<TopDepartmentDto>("""
                SELECT TOP 10 d.DepartmentName, COUNT(mld.MemberLevelDepartmentId) as MemberCount
                FROM Departments d
                LEFT JOIN LevelDepartments ld ON d.DepartmentId = ld.DepartmentId
                LEFT JOIN MemberLevelDepartments mld ON ld.LevelDepartmentId = mld.LevelDepartmentId
                GROUP BY d.DepartmentName
                ORDER BY COUNT(mld.MemberLevelDepartmentId) DESC
                """).ToListAsync();

            var response = new OrganizationSummaryResponse
            {
                Overview = new OverviewDto
                {
                    TotalNationals = await db.Nationals.CountAsync(),
                    TotalStates = await db.States.CountAsync(),
                    TotalUnits = await db.Units.CountAsync(),
                    TotalMembers = await db.Members.CountAsync(),
                    TotalDepartments = await db.Departments.CountAsync(),
                    TotalLevels = await db.Levels.CountAsync(),
                    TotalExcoPositions = await db.MemberLevelDepartments.CountAsync()
                },
                ExcoBreakdown = new ExcoBreakdownDto
                {
                    NationalExco = await db.Levels
                        .Where(l => l.NationalId != null)
                        .SelectMany(l => l.LevelDepartments)
                        .SelectMany(ld => ld.MemberLevelDepartments)
                        .CountAsync(),
                    StateExco = await db.Levels
                        .Where(l => l.StateId != null)
                        .SelectMany(l => l.LevelDepartments)
                        .SelectMany(ld => ld.MemberLevelDepartments)
                        .CountAsync(),
                    UnitExco = await db.Levels
                        .Where(l => l.UnitId != null)
                        .SelectMany(l => l.LevelDepartments)
                        .SelectMany(ld => ld.MemberLevelDepartments)
                        .CountAsync()
                },
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