using System.ComponentModel.DataAnnotations;

namespace AmsaAPI.DTOs
{
    // Response DTOs for complex hierarchical data
    public class MemberDetailResponse
    {
        public int MemberId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public int Mkanid { get; set; }
        public UnitHierarchyDto Unit { get; set; } = new();
        public List<MemberRoleDto> Roles { get; set; } = new();
    }

    public class UnitHierarchyDto
    {
        public int UnitId { get; set; }
        public string UnitName { get; set; } = string.Empty;
        public StateHierarchyDto State { get; set; } = new();
    }

    public class StateHierarchyDto
    {
        public int StateId { get; set; }
        public string StateName { get; set; } = string.Empty;
        public NationalDto National { get; set; } = new();
    }

    public class NationalDto
    {
        public int NationalId { get; set; }
        public string NationalName { get; set; } = string.Empty;
    }

    public class MemberRoleDto
    {
        public string DepartmentName { get; set; } = string.Empty;
        public string LevelType { get; set; } = string.Empty;
        public string Scope { get; set; } = string.Empty;
        public string? ScopeName { get; set; }
    }

    // Dashboard and Statistics DTOs
    public class DashboardStatsResponse
    {
        public int TotalMembers { get; set; }
        public int TotalUnits { get; set; }
        public int TotalDepartments { get; set; }
        public int TotalStates { get; set; }
        public int TotalNationals { get; set; }
        public int TotalLevels { get; set; }
        public int ExcoMembers { get; set; }
        public int NationalExcoCount { get; set; }
        public int StateExcoCount { get; set; }
        public int UnitExcoCount { get; set; }
        public List<RecentMemberDto> RecentMembers { get; set; } = new();
    }

    public class RecentMemberDto
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public int Mkanid { get; set; }
    }

    // Organization Summary DTOs
    public class OrganizationSummaryResponse
    {
        public OverviewDto Overview { get; set; } = new();
        public ExcoBreakdownDto ExcoBreakdown { get; set; } = new();
        public List<TopUnitDto> TopUnits { get; set; } = new();
        public List<TopDepartmentDto> TopDepartments { get; set; } = new();
    }

    public class OverviewDto
    {
        public int TotalNationals { get; set; }
        public int TotalStates { get; set; }
        public int TotalUnits { get; set; }
        public int TotalMembers { get; set; }
        public int TotalDepartments { get; set; }
        public int TotalLevels { get; set; }
        public int TotalExcoPositions { get; set; }
    }

    public class ExcoBreakdownDto
    {
        public int NationalExco { get; set; }
        public int StateExco { get; set; }
        public int UnitExco { get; set; }
    }

    public class TopUnitDto
    {
        public string UnitName { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public int MemberCount { get; set; }
    }

    public class TopDepartmentDto
    {
        public string DepartmentName { get; set; } = string.Empty;
        public int MemberCount { get; set; }
    }

    // Level Department DTOs
    public class LevelDepartmentResponse
    {
        public int LevelDepartmentId { get; set; }
        public int LevelId { get; set; }
        public int DepartmentId { get; set; }
        public string Department { get; set; } = string.Empty;
        public string Level { get; set; } = string.Empty;
        public string Scope { get; set; } = string.Empty;
        public int? ScopeId { get; set; }
        public int MemberCount { get; set; }
    }

    // Request DTOs for creation/updates
    public class CreateMemberRequest
    {
        [Required]
        [MaxLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string LastName { get; set; } = string.Empty;

        [EmailAddress]
        [MaxLength(100)]
        public string? Email { get; set; }

        [Phone]
        [MaxLength(15)]
        public string? Phone { get; set; }

        [Required]
        public int Mkanid { get; set; }

        [Required]
        public int UnitId { get; set; }
    }

    public class UpdateMemberRequest
    {
        [Required]
        [MaxLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string LastName { get; set; } = string.Empty;

        [EmailAddress]
        [MaxLength(100)]
        public string? Email { get; set; }

        [Phone]
        [MaxLength(15)]
        public string? Phone { get; set; }

        [Required]
        public int UnitId { get; set; }
    }

    // Import Response DTOs
    public class ImportResponse
    {
        public string Message { get; set; } = string.Empty;
        public List<string> UnmatchedRecords { get; set; } = new();
        public int UnmatchedCount { get; set; }
        public bool Success => UnmatchedCount == 0;
    }

    // Hierarchy Response DTOs
    public class OrganizationHierarchyResponse
    {
        public NationalDto National { get; set; } = new();
        public List<StateWithUnitsDto> States { get; set; } = new();
    }

    public class StateWithUnitsDto
    {
        public StateDto State { get; set; } = new();
        public List<UnitWithCountDto> Units { get; set; } = new();
    }

    public class StateDto
    {
        public int StateId { get; set; }
        public string StateName { get; set; } = string.Empty;
    }

    public class UnitWithCountDto
    {
        public UnitDto Unit { get; set; } = new();
        public int MemberCount { get; set; }
    }

    public class UnitDto
    {
        public int UnitId { get; set; }
        public string UnitName { get; set; } = string.Empty;
    }
}