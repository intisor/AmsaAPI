using AmsaAPI.Data;
using AmsaAPI.DTOs;

namespace AmsaAPI.Extensions;

public static class EntityToDtoMappingExtensions
{
    // Member mapping with separate roles (for FromSqlRaw scenarios)
    public static MemberDetailResponse ToDetailResponseWithRoles(this Member member, List<MemberLevelDepartment> roles)
    {
        return new MemberDetailResponse
        {
            MemberId = member.MemberId,
            FirstName = member.FirstName,
            LastName = member.LastName,
            Email = member.Email,
            Phone = member.Phone,
            Mkanid = member.Mkanid,
            Unit = new UnitHierarchyDto
            {
                UnitId = member.UnitId,
                UnitName = member.Unit?.UnitName ?? string.Empty,
                State = new StateHierarchyDto
                {
                    StateId = member.Unit?.StateId ?? 0,
                    StateName = member.Unit?.State?.StateName ?? string.Empty,
                    National = new NationalDto
                    {
                        NationalId = member.Unit?.State?.NationalId ?? 0,
                        NationalName = member.Unit?.State?.National?.NationalName ?? string.Empty
                    }
                }
            },
            Roles = roles.Select(role => new DepartmentAtLevelDto
            {
                DepartmentName = role.LevelDepartment?.Department?.DepartmentName ?? string.Empty,
                LevelType = role.LevelDepartment?.Level?.LevelType ?? string.Empty,
            }).ToList()
        };
    }

    // Create entity from request DTO
    public static Member ToEntity(this CreateMemberRequest request)
    {
        return new Member
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            Phone = request.Phone,
            Mkanid = request.Mkanid,
            UnitId = request.UnitId
        };
    }

    public static void UpdateEntity(this UpdateMemberRequest request, Member member)
    {
        member.FirstName = request.FirstName;
        member.LastName = request.LastName;
        member.Email = request.Email;
        member.Phone = request.Phone;
        member.UnitId = request.UnitId;
    }
}