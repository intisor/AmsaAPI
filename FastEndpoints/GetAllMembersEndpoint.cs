using AmsaAPI.Data;
using AmsaAPI.DTOs;
using AmsaAPI.Extensions;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace AmsaAPI.FastEndpoints;

public sealed class GetAllMembersEndpoint(AmsaDbContext db) : Endpoint<object, List<MemberDetailResponse>>
{
    private readonly AmsaDbContext _db = db;

    public override void Configure()
    {
        Get("/api/members");
        AllowAnonymous();
        Description(b => b.WithTags("Members"));
    }

    public override async Task HandleAsync(object req, CancellationToken ct)
    {
        // Get member data with organizational hierarchy using FromSqlRaw
        var membersWithHierarchy = await _db.Members
            .FromSqlRaw("""
                SELECT m.MemberId, m.FirstName, m.LastName, m.Email, m.Phone, m.Mkanid, m.UnitId
                FROM Members m
                """)
            .Include(m => m.Unit)
                .ThenInclude(u => u.State)
                    .ThenInclude(s => s.National)
            .AsNoTracking()
            .ToListAsync(ct);

        // Get roles data using FromSqlRaw on MemberLevelDepartments
        var rolesData = await _db.MemberLevelDepartments
            .FromSqlRaw("""
                SELECT mld.MemberLevelDepartmentId, mld.MemberId, mld.LevelDepartmentId
                FROM MemberLevelDepartments mld
                """)
            .Include(mld => mld.LevelDepartment)
                .ThenInclude(ld => ld.Department)
            .Include(mld => mld.LevelDepartment)
                .ThenInclude(ld => ld.Level)
            .AsNoTracking()
            .ToListAsync(ct);

        // Transform to response DTOs using extension methods
        var response = membersWithHierarchy.Select(member => 
        {
            var memberRoles = rolesData.Where(role => role.MemberId == member.MemberId).ToList();
            return member.ToDetailResponseWithRoles(memberRoles);
        }).ToList();

        Response = response;
    }
}
