using AmsaAPI.Common;
using AmsaAPI.Data;
using AmsaAPI.DTOs;
using AmsaAPI.Extensions;
using Microsoft.EntityFrameworkCore;

namespace AmsaAPI.Services;

public class MemberService(AmsaDbContext db)
{
    public async Task<Result<MemberDetailResponse>> GetMemberByIdAsync(int id, CancellationToken ct = default)
    {
        // Input validation
        if (id <= 0)
            return Result.Validation<MemberDetailResponse>("Invalid member ID. ID must be greater than 0.");

        var member = await db.Members
            .Include(m => m.Unit.State.National)
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.MemberId == id, ct);

        if (member == null)
            return Result.NotFound<MemberDetailResponse>($"Member with ID {id} not found.");

        var rolesData = await db.MemberLevelDepartments
            .Where(mld => mld.MemberId == id)
            .Include(mld => mld.LevelDepartment.Department)
            .Include(mld => mld.LevelDepartment.Level)
            .AsNoTracking()
            .ToListAsync(ct);

        var response = member.ToDetailResponseWithRoles(rolesData);
        return Result.Success(response);
    }

    public async Task<Result<MemberDetailResponse>> GetMemberByMkanIdAsync(int mkanId, CancellationToken ct = default)
    {
        // Input validation
        if (mkanId <= 0)
            return Result.Validation<MemberDetailResponse>("Invalid MKAN ID. ID must be greater than 0.");

        var member = await db.Members
            .Include(m => m.Unit.State.National)
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Mkanid == mkanId, ct);

        if (member == null)
            return Result.NotFound<MemberDetailResponse>($"Member with MKAN ID {mkanId} not found.");

        var rolesData = await db.MemberLevelDepartments
            .Where(mld => mld.MemberId == member.MemberId)
            .Include(mld => mld.LevelDepartment.Department)
            .Include(mld => mld.LevelDepartment.Level)
            .AsNoTracking()
            .ToListAsync(ct);

        var response = member.ToDetailResponseWithRoles(rolesData);
        return Result.Success(response);
    }

    public async Task<Result<List<MemberSummaryResponse>>> GetMembersByUnitAsync(int unitId, CancellationToken ct = default)
    {
        // Input validation
        if (unitId <= 0)
            return Result.Validation<List<MemberSummaryResponse>>("Invalid unit ID. ID must be greater than 0.");

        var members = await db.Members
            .Where(m => m.UnitId == unitId)
            .Select(m => new MemberSummaryResponse
            {
                MemberId = m.MemberId,
                FirstName = m.FirstName,
                LastName = m.LastName,
                Email = m.Email,
                Phone = m.Phone,
                Mkanid = m.Mkanid
            })
            .ToListAsync(ct);

        return Result.Success(members);
    }

    public async Task<Result<List<MemberSummaryResponse>>> GetMembersByDepartmentAsync(int departmentId, CancellationToken ct = default)
    {
        // Input validation
        if (departmentId <= 0)
            return Result.Validation<List<MemberSummaryResponse>>("Invalid department ID. ID must be greater than 0.");

        var members = await db.Members
            .Where(m => m.MemberLevelDepartments.Any(mld =>
                mld.LevelDepartment.DepartmentId == departmentId))
            .Select(m => new MemberSummaryResponse
            {
                MemberId = m.MemberId,
                FirstName = m.FirstName,
                LastName = m.LastName,
                Email = m.Email,
                Phone = m.Phone,
                Mkanid = m.Mkanid
            })
            .ToListAsync(ct);

        return Result.Success(members);
    }

    public async Task<Result<List<MemberSummaryResponse>>> SearchMembersByNameAsync(string name, CancellationToken ct = default)
    {
        // Input validation
        if (string.IsNullOrWhiteSpace(name) || name.Length < 2)
            return Result.Validation<List<MemberSummaryResponse>>("Search name must be at least 2 characters long.");

        var members = await db.Members
            .Where(m => m.FirstName.Contains(name) || m.LastName.Contains(name))
            .Select(m => new MemberSummaryResponse
            {
                MemberId = m.MemberId,
                FirstName = m.FirstName,
                LastName = m.LastName,
                Email = m.Email,
                Phone = m.Phone,
                Mkanid = m.Mkanid
            })
            .ToListAsync(ct);

        return Result.Success(members);
    }

    public async Task<Result<List<MemberDetailResponse>>> GetAllMembersAsync(CancellationToken ct = default)
    {
        var membersWithAllData = await db.Members
            .Include(m => m.Unit.State.National)
            .Include(m => m.MemberLevelDepartments)
                .ThenInclude(mld => mld.LevelDepartment.Department)
            .Include(m => m.MemberLevelDepartments)
                .ThenInclude(mld => mld.LevelDepartment.Level)
            .AsNoTracking()
            .ToListAsync(ct);

        var response = membersWithAllData.Select(member =>
            member.ToDetailResponseWithRoles(member.MemberLevelDepartments.ToList())
        ).ToList();

        return Result.Success(response);
    }
}