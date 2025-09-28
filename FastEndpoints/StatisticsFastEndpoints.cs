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
            .OrderByDescending(m => m.MemberId)
            .Take(5)
            .Select(m => new RecentMemberDto
            {
                FirstName = m.FirstName,
                LastName = m.LastName,
                Mkanid = m.Mkanid
            })
            .ToListAsync(ct);

        var excoBreakdown = await db.MemberLevelDepartments
            .GroupBy(mld => new { }) 
            .Select(g => new ExcoBreakdownDto
            {
                NationalExco = g.Count(mld => mld.LevelDepartment.Level.NationalId != null),
                StateExco = g.Count(mld => mld.LevelDepartment.Level.StateId != null),
                UnitExco = g.Count(mld => mld.LevelDepartment.Level.UnitId != null)
            })
            .FirstOrDefaultAsync(ct) ?? new ExcoBreakdownDto();

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
        var topUnits = await db.Units
            .Select(u => new TopUnitDto
            {
                UnitName = u.UnitName,
                State = u.State.StateName,
                MemberCount = u.Members.Count
            })
            .OrderByDescending(u => u.MemberCount)
            .Take(10)
            .ToListAsync(ct);

        var topDepartments = await db.Departments
            .Select(d => new TopDepartmentDto
            {
                DepartmentName = d.DepartmentName,
                MemberCount = d.LevelDepartments
                    .SelectMany(ld => ld.MemberLevelDepartments)
                    .Count()
            })
            .OrderByDescending(d => d.MemberCount)
            .Take(10)
            .ToListAsync(ct);

        var excoBreakdown = await db.MemberLevelDepartments
            .GroupBy(mld => new { })
            .Select(g => new ExcoBreakdownDto
            {
                NationalExco = g.Count(mld => mld.LevelDepartment.Level.NationalId != null),
                StateExco = g.Count(mld => mld.LevelDepartment.Level.StateId != null),
                UnitExco = g.Count(mld => mld.LevelDepartment.Level.UnitId != null)
            })
            .FirstOrDefaultAsync(ct) ?? new ExcoBreakdownDto();

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
            ExcoBreakdown = excoBreakdown,
            TopUnits = topUnits,
            TopDepartments = topDepartments
        };

        await Send.OkAsync(response, ct);
    }
}



// Get Organization Hierarchy Endpoint - Final Optimized Version
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
        var hierarchyData = await db.Nationals
            .AsNoTracking()
            .Include(n => n.States.OrderBy(s => s.StateName))
                .ThenInclude(s => s.Units.OrderBy(u => u.UnitName))
                    .ThenInclude(u => u.Members)
            .OrderBy(n => n.NationalName)
            .ToListAsync(ct);

        var treeData = hierarchyData.Select(national => 
            new HierarchyNationalNodeDto(
                national.NationalId,
                national.NationalName,
                national.States.SelectMany(s => s.Units)
                                .Sum(u => u.Members.Count()),
                national.States.Select(state => 
                    new HierarchyStateNodeDto(
                        state.StateId,
                        state.StateName,
                        state.Units.Sum(u => u.Members.Count()),    
                        state.Units.Select(unit => 
                            new HierarchyUnitNodeDto(
                                unit.UnitId,
                                unit.UnitName,
                                unit.Members.Count
                            )
                        ).ToList()
                    )
                ).ToList()
            )
        ).ToList();

        await Send.OkAsync(treeData, ct);
    }
}