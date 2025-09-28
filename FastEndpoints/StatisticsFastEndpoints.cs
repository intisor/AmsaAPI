using AmsaAPI.Common;
using AmsaAPI.Data;
using AmsaAPI.DTOs;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace AmsaAPI.FastEndpoints;

// Private record DTOs for raw SQL projections - minimal and functional
file record DashboardCountsDto(
    int TotalMembers,
    int TotalUnits,
    int TotalDepartments,
    int TotalStates,
    int TotalNationals,
    int TotalLevels,
    int ExcoMembers
);

file record OverviewCountsDto(
    int TotalNationals,
    int TotalStates,
    int TotalUnits,
    int TotalMembers,
    int TotalDepartments,
    int TotalLevels,
    int TotalExcoPositions
);

file record HierarchyFlatDto(
    int NationalId,
    string NationalName,
    int StateId,
    string StateName,
    int UnitId,
    string UnitName,
    int MemberCount
);

// Get Dashboard Stats Endpoint
public sealed class GetDashboardStatsEndpoint(AmsaDbContext db) : Endpoint<EmptyRequest, DashboardStatsResponse>
{
    public override void Configure()
    {
        Get("/api/stats/dashboard");
        AllowAnonymous();
        Summary(s => s.Summary = "Get comprehensive dashboard statistics");
    }

    public override async Task HandleAsync(EmptyRequest req, CancellationToken ct)
    {
        // Single aggregated counts query
        var counts = await db.Database.SqlQueryRaw<DashboardCountsDto>("""
            SELECT 
                (SELECT COUNT(*) FROM Members) as TotalMembers,
                (SELECT COUNT(*) FROM Units) as TotalUnits,
                (SELECT COUNT(*) FROM Departments) as TotalDepartments,
                (SELECT COUNT(*) FROM States) as TotalStates,
                (SELECT COUNT(*) FROM Nationals) as TotalNationals,
                (SELECT COUNT(*) FROM Levels) as TotalLevels,
                (SELECT COUNT(*) FROM MemberLevelDepartments) as ExcoMembers
            """).FirstOrDefaultAsync(ct) ?? new DashboardCountsDto(0, 0, 0, 0, 0, 0, 0);

        // Raw SQL for recent members
        var recentMembers = await db.Database.SqlQueryRaw<RecentMemberDto>("""
            SELECT TOP 5 
                FirstName,
                LastName,
                Mkanid
            FROM Members
            ORDER BY MemberId DESC
            """).ToListAsync(ct);

        // EXCO breakdown query
        var excoBreakdown = await db.Database.SqlQueryRaw<ExcoBreakdownDto>("""
            SELECT 
                COUNT(CASE WHEN l.NationalId IS NOT NULL THEN 1 END) as NationalExco,
                COUNT(CASE WHEN l.StateId IS NOT NULL THEN 1 END) as StateExco,
                COUNT(CASE WHEN l.UnitId IS NOT NULL THEN 1 END) as UnitExco
            FROM MemberLevelDepartments mld
            INNER JOIN LevelDepartments ld ON mld.LevelDepartmentId = ld.LevelDepartmentId
            INNER JOIN Levels l ON ld.LevelId = l.LevelId
        """).FirstOrDefaultAsync(ct) ?? new ExcoBreakdownDto();

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
            NationalExcoCount = excoBreakdown.NationalExco,
            StateExcoCount = excoBreakdown.StateExco,
            UnitExcoCount = excoBreakdown.UnitExco
        };

        await Send.OkAsync(response, ct);
    }
}

// Get Organization Summary Endpoint
public sealed class GetOrganizationSummaryEndpoint(AmsaDbContext db) : Endpoint<EmptyRequest, OrganizationSummaryResponse>
{
    public override void Configure()
    {
        Get("/api/stats/organization-summary");
        AllowAnonymous();
        Summary(s => s.Summary = "Get organization overview with top units and departments");
    }

    public override async Task HandleAsync(EmptyRequest req, CancellationToken ct)
    {
        // Single aggregated counts query for overview
        var overviewCounts = await db.Database.SqlQueryRaw<OverviewCountsDto>("""
            SELECT 
                (SELECT COUNT(*) FROM Nationals) as TotalNationals,
                (SELECT COUNT(*) FROM States) as TotalStates,
                (SELECT COUNT(*) FROM Units) as TotalUnits,
                (SELECT COUNT(*) FROM Members) as TotalMembers,
                (SELECT COUNT(*) FROM Departments) as TotalDepartments,
                (SELECT COUNT(*) FROM Levels) as TotalLevels,
                (SELECT COUNT(*) FROM MemberLevelDepartments) as TotalExcoPositions
            """).FirstOrDefaultAsync(ct) ?? new OverviewCountsDto(0, 0, 0, 0, 0, 0, 0);

        // Raw SQL for top units
        var topUnits = await db.Database.SqlQueryRaw<TopUnitDto>("""
            SELECT TOP 10 u.UnitName, s.StateName as State, COUNT(m.MemberId) as MemberCount
            FROM Units u
            INNER JOIN States s ON u.StateId = s.StateId
            LEFT JOIN Members m ON u.UnitId = m.UnitId
            GROUP BY u.UnitName, s.StateName
            ORDER BY COUNT(m.MemberId) DESC
        """).ToListAsync(ct);

        // Raw SQL for top departments
        var topDepartments = await db.Database.SqlQueryRaw<TopDepartmentDto>("""
            SELECT TOP 10 d.DepartmentName, COUNT(mld.MemberLevelDepartmentId) as MemberCount
            FROM Departments d
            LEFT JOIN LevelDepartments ld ON d.DepartmentId = ld.DepartmentId
            LEFT JOIN MemberLevelDepartments mld ON ld.LevelDepartmentId = mld.LevelDepartmentId
            GROUP BY d.DepartmentName
            ORDER BY COUNT(mld.MemberLevelDepartmentId) DESC
        """).ToListAsync(ct);

        // EXCO breakdown query
        var excoBreakdown = await db.Database.SqlQueryRaw<ExcoBreakdownDto>("""
            SELECT 
                COUNT(CASE WHEN l.NationalId IS NOT NULL THEN 1 END) as NationalExco,
                COUNT(CASE WHEN l.StateId IS NOT NULL THEN 1 END) as StateExco,
                COUNT(CASE WHEN l.UnitId IS NOT NULL THEN 1 END) as UnitExco
            FROM MemberLevelDepartments mld
            INNER JOIN LevelDepartments ld ON mld.LevelDepartmentId = ld.LevelDepartmentId
            INNER JOIN Levels l ON ld.LevelId = l.LevelId
        """).FirstOrDefaultAsync(ct) ?? new ExcoBreakdownDto();

        var response = new OrganizationSummaryResponse
        {
            Overview = new OverviewDto
            {
                TotalNationals = overviewCounts.TotalNationals,
                TotalStates = overviewCounts.TotalStates,
                TotalUnits = overviewCounts.TotalUnits,
                TotalMembers = overviewCounts.TotalMembers,
                TotalDepartments = overviewCounts.TotalDepartments,
                TotalLevels = overviewCounts.TotalLevels,
                TotalExcoPositions = overviewCounts.TotalExcoPositions
            },
            ExcoBreakdown = excoBreakdown,
            TopUnits = topUnits,
            TopDepartments = topDepartments
        };

        await Send.OkAsync(response, ct);
    }
}

// Get Organization Hierarchy Endpoint
public sealed class GetOrganizationHierarchyEndpoint(AmsaDbContext db) : Endpoint<EmptyRequest, IReadOnlyList<HierarchyNationalNodeDto>>
{   
    public override void Configure()
    {
        Get("/api/hierarchy");
        AllowAnonymous();
        Summary(s => s.Summary = "Get organizational hierarchy in tree structure");
    }

    public override async Task HandleAsync(EmptyRequest req, CancellationToken ct)
    {
        // Single flattened hierarchy query
        var flatHierarchy = await db.Database.SqlQueryRaw<HierarchyFlatDto>("""
            SELECT 
                n.NationalId,
                n.NationalName,
                ISNULL(s.StateId, 0) as StateId,
                ISNULL(s.StateName, '') as StateName,
                ISNULL(u.UnitId, 0) as UnitId,
                ISNULL(u.UnitName, '') as UnitName,
                COUNT(m.MemberId) as MemberCount
            FROM Nationals n
            LEFT JOIN States s ON n.NationalId = s.NationalId
            LEFT JOIN Units u ON s.StateId = u.StateId
            LEFT JOIN Members m ON u.UnitId = m.UnitId
            GROUP BY n.NationalId, n.NationalName, s.StateId, s.StateName, u.UnitId, u.UnitName
            ORDER BY n.NationalName, s.StateName, u.UnitName
            """).ToListAsync(ct);

        // Rebuild tree structure in memory using LINQ grouping
        var treeData = flatHierarchy
            .GroupBy(h => new { h.NationalId, h.NationalName })
            .Select(nationalGroup => new HierarchyNationalNodeDto(
                nationalGroup.Key.NationalId,
                nationalGroup.Key.NationalName,
                nationalGroup.Sum(h => h.MemberCount),
                nationalGroup
                    .Where(h => h.StateId != 0) // Filter out null states
                    .GroupBy(h => new { h.StateId, h.StateName })
                    .Select(stateGroup => new HierarchyStateNodeDto(
                        stateGroup.Key.StateId,
                        stateGroup.Key.StateName,
                        stateGroup.Sum(h => h.MemberCount),
                        stateGroup
                            .Where(h => h.UnitId != 0)  // Filter out null units
                            .Select(h => new HierarchyUnitNodeDto(
                                h.UnitId,
                                h.UnitName,
                                h.MemberCount
                            ))
                            .ToList()
                    ))
                    .ToList()
            ))
            .ToList();

        await Send.OkAsync(treeData, ct);
    }
}