using System.ComponentModel.DataAnnotations;

namespace AmsaAPI.DTOs;

// Core Response DTOs
public class MemberDetailResponse
{
    public int MemberId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public int Mkanid { get; set; }
    public UnitHierarchyDto Unit { get; set; } = new();
    public List<DepartmentAtLevelDto> Roles { get; set; } = [];
}

public class MemberSummaryResponse
{
    public int MemberId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public int Mkanid { get; set; }
}

// Hierarchy DTOs
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

public class DepartmentAtLevelDto
{
    public string DepartmentName { get; set; } = string.Empty;
    public string LevelType { get; set; } = string.Empty;
}

// Organization DTOs
public class UnitSummaryDto
{
    public int UnitId { get; set; }
    public string UnitName { get; set; } = string.Empty;
    public int StateId { get; set; }
    public string StateName { get; set; } = string.Empty;
    public string NationalName { get; set; } = string.Empty;
    public int MemberCount { get; set; }
}

public class UnitDetailDto
{
    public int UnitId { get; set; }
    public string UnitName { get; set; } = string.Empty;
    public int StateId { get; set; }
    public string StateName { get; set; } = string.Empty;
    public int NationalId { get; set; }
    public string NationalName { get; set; } = string.Empty;
    public int MemberCount { get; set; }
}

public class UnitDetailResponse
{
    public UnitDetailDto Unit { get; set; } = new();
    public List<UnitMemberDto> Members { get; set; } = new();
    public List<UnitExcoDto> ExcoRoles { get; set; } = new();
}

public class UnitMemberDto
{
    public int MemberId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public int Mkanid { get; set; }
}

public class UnitExcoDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public int Mkanid { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public string LevelType { get; set; } = string.Empty;
}

public class UnitStateDto
{
    public int UnitId { get; set; }
    public string UnitName { get; set; } = string.Empty;
    public int MemberCount { get; set; }
    public int ExcoCount { get; set; }
}

public class StateSummaryDto
{
    public int StateId { get; set; }
    public string StateName { get; set; } = string.Empty;
    public string NationalName { get; set; } = string.Empty;
    public int UnitCount { get; set; }
    public int MemberCount { get; set; }
    public int ExcoCount { get; set; }
}

public class StateDetailDto
{
    public int StateId { get; set; }
    public string StateName { get; set; } = string.Empty;
    public int NationalId { get; set; }
    public string NationalName { get; set; } = string.Empty;
    public int UnitCount { get; set; }
    public int MemberCount { get; set; }
}

public class NationalSummaryDto
{
    public int NationalId { get; set; }
    public string NationalName { get; set; } = string.Empty;
    public int StateCount { get; set; }
    public int UnitCount { get; set; }
    public int MemberCount { get; set; }
    public int ExcoCount { get; set; }
}

public class NationalDetailDto
{
    public int NationalId { get; set; }
    public string NationalName { get; set; } = string.Empty;
    public int StateCount { get; set; }
    public int UnitCount { get; set; }
    public int MemberCount { get; set; }
}

public class DepartmentSummaryDto
{
    public int DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public int MemberCount { get; set; }
}

public class DepartmentDetailResponse
{
    public int DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public List<DepartmentLevelDto> Levels { get; set; } = new();
}

public class DepartmentLevelDto
{
    public int LevelDepartmentId { get; set; }
    public string LevelType { get; set; } = string.Empty;
    public int MemberCount { get; set; }
}

// Statistics DTOs
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

public class HierarchyDto
{
    public int NationalId { get; set; }
    public string NationalName { get; set; } = string.Empty;
    public int? StateId { get; set; }
    public string? StateName { get; set; }
    public int? UnitId { get; set; }
    public string? UnitName { get; set; }
    public int MemberCount { get; set; }
}

// Request DTOs
public class GetMemberByIdRequest
{
    public int Id { get; set; }
}

public class GetMemberByMkanIdRequest
{
    public int MkanId { get; set; }
}

public class GetMembersByUnitRequest
{
    public int UnitId { get; set; }
}

public class GetMembersByDepartmentRequest
{
    public int DepartmentId { get; set; }
}

public class SearchMembersByNameRequest
{
    public string Name { get; set; } = string.Empty;
}

public class GetUnitByIdRequest
{
    public int Id { get; set; }
}

public class GetUnitsByStateRequest
{
    public int StateId { get; set; }
}

public class GetStateByIdRequest
{
    public int Id { get; set; }
}

public class GetNationalByIdRequest
{
    public int Id { get; set; }
}

public class GetDepartmentByIdRequest
{
    public int Id { get; set; }
}

// Import Response DTOs
public class ImportResponse
{
    public string Message { get; set; } = string.Empty;
    public List<string> UnmatchedRecords { get; set; } = new();
    public int UnmatchedCount { get; set; }
    public bool Success => UnmatchedCount == 0;
}

// Statistics DTOs for specific endpoints
public class UnitStatsDto
{
    public int UnitId { get; set; }
    public string UnitName { get; set; } = string.Empty;
    public string StateName { get; set; } = string.Empty;
    public int MemberCount { get; set; }
    public int ExcoCount { get; set; }
}

public class DepartmentStatsDto
{
    public int DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public int TotalMemberCount { get; set; }
    public int NationalCount { get; set; }
    public int StateCount { get; set; }
    public int UnitCount { get; set; }
}

// Create/Update DTOs  
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