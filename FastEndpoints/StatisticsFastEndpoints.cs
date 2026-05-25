using AmsaAPI.Common;
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
            .AsNoTracking()
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
            TotalMembers = await db.Members.AsNoTracking().CountAsync(ct),
            TotalUnits = await db.Units.AsNoTracking().CountAsync(ct),
            TotalDepartments = await db.Departments.AsNoTracking().CountAsync(ct),
            TotalStates = await db.States.AsNoTracking().CountAsync(ct),
            TotalNationals = await db.Nationals.AsNoTracking().CountAsync(ct),
            TotalLevels = await db.Levels.AsNoTracking().CountAsync(ct),
            ExcoMembers = await db.MemberLevelDepartments.AsNoTracking().CountAsync(ct),
            RecentMembers = recentMembers,
            NationalExcoCount = await db.Levels.AsNoTracking().CountAsync(l => l.NationalId != null, ct),
            StateExcoCount = await db.Levels.AsNoTracking().CountAsync(l => l.StateId != null, ct),
            UnitExcoCount = await db.Levels.AsNoTracking().CountAsync(l => l.UnitId != null, ct)
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
        var overview = new OverviewDto
        {
            TotalNationals = await db.Nationals.AsNoTracking().CountAsync(ct),
            TotalStates = await db.States.AsNoTracking().CountAsync(ct),
            TotalUnits = await db.Units.AsNoTracking().CountAsync(ct),
            TotalMembers = await db.Members.AsNoTracking().CountAsync(ct),
            TotalDepartments = await db.Departments.AsNoTracking().CountAsync(ct),
            TotalLevels = await db.Levels.AsNoTracking().CountAsync(ct),
            TotalExcoPositions = await db.MemberLevelDepartments.AsNoTracking().CountAsync(ct)
        };

        var topUnits = await db.Units
            .AsNoTracking()
            .Select(u => new TopUnitDto
            {
                UnitName = u.UnitName,
                State = u.State.StateName,
                MemberCount = u.Members.Count()
            })
            .OrderByDescending(u => u.MemberCount)
            .ThenBy(u => u.UnitName)
            .Take(10)
            .ToListAsync(ct);

        var topDepartments = await db.Departments
            .AsNoTracking()
            .Select(d => new TopDepartmentDto
            {
                DepartmentName = d.DepartmentName,
                MemberCount = d.LevelDepartments
                    .SelectMany(ld => ld.MemberLevelDepartments)
                    .Count()
            })
            .OrderByDescending(d => d.MemberCount)
            .ThenBy(d => d.DepartmentName)
            .Take(10)
            .ToListAsync(ct);

        var excoBreakdown = new ExcoBreakdownDto
        {
            NationalExco = await db.Levels.AsNoTracking().CountAsync(l => l.NationalId != null, ct),
            StateExco = await db.Levels.AsNoTracking().CountAsync(l => l.StateId != null, ct),
            UnitExco = await db.Levels.AsNoTracking().CountAsync(l => l.UnitId != null, ct)
        };

        var response = new OrganizationSummaryResponse
        {
            Overview = overview,
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
        var flatHierarchy = await db.Nationals
            .AsNoTracking()
            .SelectMany(n => n.States.DefaultIfEmpty(), (n, s) => new { National = n, State = s })
            .SelectMany(x => x.State == null ? db.Units.Where(u => false).DefaultIfEmpty() : x.State.Units.DefaultIfEmpty(), (x, u) => new
            {
                x.National.NationalId,
                x.National.NationalName,
                StateId = x.State != null ? x.State.StateId : 0,
                StateName = x.State != null ? x.State.StateName : string.Empty,
                UnitId = u != null ? u.UnitId : 0,
                UnitName = u != null ? u.UnitName : string.Empty,
                MemberCount = u != null ? u.Members.Count() : 0
            })
            .ToListAsync(ct);

        var treeData = flatHierarchy
            .GroupBy(h => new { h.NationalId, h.NationalName })
            .Select(nationalGroup => new HierarchyNationalNodeDto(
                nationalGroup.Key.NationalId,
                nationalGroup.Key.NationalName,
                nationalGroup.Sum(h => h.MemberCount),
                nationalGroup
                    .Where(h => h.StateId != 0)
                    .GroupBy(h => new { h.StateId, h.StateName })
                    .Select(stateGroup => new HierarchyStateNodeDto(
                        stateGroup.Key.StateId,
                        stateGroup.Key.StateName,
                        stateGroup.Sum(h => h.MemberCount),
                        stateGroup
                            .Where(h => h.UnitId != 0)
                            .Select(h => new HierarchyUnitNodeDto(
                                h.UnitId,
                                h.UnitName,
                                h.MemberCount))
                            .ToList()))
                    .ToList()))
            .ToList();

        await Send.OkAsync(treeData, ct);
    }
}