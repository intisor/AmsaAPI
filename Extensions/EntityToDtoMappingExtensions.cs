using AmsaAPI.Data;
using AmsaAPI.DTOs;

namespace AmsaAPI.Extensions
{
    public static class EntityToDtoMappingExtensions
    {
        // Member mappings
        public static MemberDetailResponse ToDetailResponse(this Member member)
        {
            return new MemberDetailResponse
            {
                MemberId = member.MemberId,
                FirstName = member.FirstName,
                LastName = member.LastName,
                Email = member.Email,
                Phone = member.Phone,
                Mkanid = member.Mkanid,
                Unit = member.Unit.ToHierarchyDto(),
                Roles = member.MemberLevelDepartments.Select(mld => mld.ToRoleDto()).ToList()
            };
        }

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

        private static UnitHierarchyDto ToHierarchyDto(this Unit unit)
        {
            return new UnitHierarchyDto
            {
                UnitId = unit.UnitId,
                UnitName = unit.UnitName,
                State = unit.State.ToHierarchyDto()
            };
        }

        private static StateHierarchyDto ToHierarchyDto(this State state)
        {
            return new StateHierarchyDto
            {
                StateId = state.StateId,
                StateName = state.StateName,
                National = state.National.ToDto()
            };
        }

        private static NationalDto ToDto(this National national)
        {
            return new NationalDto
            {
                NationalId = national.NationalId,
                NationalName = national.NationalName
            };
        }

        private static DepartmentAtLevelDto ToRoleDto(this MemberLevelDepartment mld)
        {
            return new DepartmentAtLevelDto
            {
                DepartmentName = mld.LevelDepartment.Department.DepartmentName,
                LevelType = mld.LevelDepartment.Level.LevelType,
            };
        }

        // Level Department mappings
        public static LevelDepartmentResponse ToResponse(this LevelDepartment ld)
        {
            var scope = ld.Level.NationalId != null ? "National" :
                       ld.Level.StateId != null ? "State" :
                       ld.Level.UnitId != null ? "Unit" : "Unknown";

            return new LevelDepartmentResponse
            {
                LevelDepartmentId = ld.LevelDepartmentId,
                LevelId = ld.LevelId,
                DepartmentId = ld.DepartmentId,
                Department = ld.Department.DepartmentName,
                Level = ld.Level.LevelType,
                Scope = scope,
                ScopeId = ld.Level.NationalId ?? ld.Level.StateId ?? ld.Level.UnitId,
                MemberCount = ld.MemberLevelDepartments.Count
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

        // Hierarchy mappings
        public static OrganizationHierarchyResponse ToHierarchyResponse(this National national)
        {
            return new OrganizationHierarchyResponse
            {
                National = national.ToDto(),
                States = national.States.Select(s => new StateWithUnitsDto
                {
                    State = new StateDto { StateId = s.StateId, StateName = s.StateName },
                    Units = s.Units.Select(u => new UnitWithCountDto
                    {
                        Unit = new UnitDto { UnitId = u.UnitId, UnitName = u.UnitName },
                        MemberCount = u.Members.Count
                    }).ToList()
                }).ToList()
            };
        }
    }
}