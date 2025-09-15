using AmsaAPI.Data;
using AmsaAPI.DTOs;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace AmsaAPI.FastEndpoints;

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
        var recentMembers = await db.Members
            .OrderByDescending(m => m.MemberId)
            .Take(5)
            .Select(m => new RecentMemberDto
            {
                FirstName = m.FirstName,
                LastName = m.LastName,
                Mkanid = m.Mkanid
            })
            .ToListAsync(ct);

        var response = new DashboardStatsResponse
        {
            TotalMembers = await db.Members.CountAsync(ct),
            TotalUnits = await db.Units.CountAsync(ct),
            TotalDepartments = await db.Departments.CountAsync(ct),
            TotalStates = await db.States.CountAsync(ct),
            TotalNationals = await db.Nationals.CountAsync(ct),
            TotalLevels = await db.Levels.CountAsync(ct),
            ExcoMembers = await db.MemberLevelDepartments.CountAsync(ct),
            RecentMembers = recentMembers,
            NationalExcoCount = await db.Levels.Where(l => l.NationalId != null).SelectMany(l => l.LevelDepartments).SelectMany(ld => ld.MemberLevelDepartments).CountAsync(ct),
            StateExcoCount = await db.Levels.Where(l => l.StateId != null).SelectMany(l => l.LevelDepartments).SelectMany(ld => ld.MemberLevelDepartments).CountAsync(ct),
            UnitExcoCount = await db.Levels.Where(l => l.UnitId != null).SelectMany(l => l.LevelDepartments).SelectMany(ld => ld.MemberLevelDepartments).CountAsync(ct)
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
        // Get top units
        var topUnits = await db.Database.SqlQueryRaw<TopUnitDto>("""
            SELECT TOP 10 u.UnitName, s.StateName as State, COUNT(m.MemberId) as MemberCount
            FROM Units u
            INNER JOIN States s ON u.StateId = s.StateId
            LEFT JOIN Members m ON u.UnitId = m.UnitId
            GROUP BY u.UnitName, s.StateName
            ORDER BY COUNT(m.MemberId) DESC
            """).ToListAsync(ct);

        // Get top departments
        var topDepartments = await db.Database.SqlQueryRaw<TopDepartmentDto>("""
            SELECT TOP 10 d.DepartmentName, COUNT(mld.MemberLevelDepartmentId) as MemberCount
            FROM Departments d
            LEFT JOIN LevelDepartments ld ON d.DepartmentId = ld.DepartmentId
            LEFT JOIN MemberLevelDepartments mld ON ld.LevelDepartmentId = mld.LevelDepartmentId
            GROUP BY d.DepartmentName
            ORDER BY COUNT(mld.MemberLevelDepartmentId) DESC
            """).ToListAsync(ct);

        var response = new OrganizationSummaryResponse
        {
            Overview = new OverviewDto
            {
                TotalNationals = await db.Nationals.CountAsync(ct),
                TotalStates = await db.States.CountAsync(ct),
                TotalUnits = await db.Units.CountAsync(ct),
                TotalMembers = await db.Members.CountAsync(ct),
                TotalDepartments = await db.Departments.CountAsync(ct),
                TotalLevels = await db.Levels.CountAsync(ct),
                TotalExcoPositions = await db.MemberLevelDepartments.CountAsync(ct)
            },
            ExcoBreakdown = new ExcoBreakdownDto
            {
                NationalExco = await db.Levels
                    .Where(l => l.NationalId != null)
                    .SelectMany(l => l.LevelDepartments)
                    .SelectMany(ld => ld.MemberLevelDepartments)
                    .CountAsync(ct),
                StateExco = await db.Levels
                    .Where(l => l.StateId != null)
                    .SelectMany(l => l.LevelDepartments)
                    .SelectMany(ld => ld.MemberLevelDepartments)
                    .CountAsync(ct),
                UnitExco = await db.Levels
                    .Where(l => l.UnitId != null)
                    .SelectMany(l => l.LevelDepartments)
                    .SelectMany(ld => ld.MemberLevelDepartments)
                    .CountAsync(ct)
            },
            TopUnits = topUnits,
            TopDepartments = topDepartments
        };

        await Send.OkAsync(response, ct);
    }
}

// Get Organization Hierarchy Endpoint
public sealed class GetOrganizationHierarchyEndpoint(AmsaDbContext db) : Endpoint<EmptyRequest, List<HierarchyDto>>
{
    public override void Configure()
    {
        Get("/api/hierarchy");
        AllowAnonymous();
        Summary(s => s.Summary = "Get complete organizational hierarchy");
    }

    public override async Task HandleAsync(EmptyRequest req, CancellationToken ct)
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
            """).ToListAsync(ct);

        await Send.OkAsync(hierarchy, ct);
    }
}