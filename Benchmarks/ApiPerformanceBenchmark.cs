using BenchmarkDotNet.Attributes;
using Microsoft.EntityFrameworkCore;
using AmsaAPI.Data;
using AmsaAPI.DTOs;
using AmsaAPI.Extensions;

namespace AmsaAPI.Benchmarks;

[MemoryDiagnoser]
[SimpleJob]
public class ApiPerformanceBenchmark
{
    private List<Member> _members = null!;
    private List<MemberLevelDepartment> _roles = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Create test hierarchy data
        var national = new National { NationalId = 1, NationalName = "Nigeria" };
        var state = new State { StateId = 1, StateName = "Lagos", NationalId = 1, National = national };
        var unit = new Unit { UnitId = 1, UnitName = "Lagos State University", StateId = 1, State = state };
        var department = new Department { DepartmentId = 1, DepartmentName = "Medical" };
        var level = new Level { LevelId = 1, LevelType = "Executive" };
        var levelDepartment = new LevelDepartment 
        { 
            LevelDepartmentId = 1, 
            LevelId = 1, 
            DepartmentId = 1,
            Level = level,
            Department = department
        };

        // Create test members with realistic data patterns
        _members = new List<Member>();
        _roles = new List<MemberLevelDepartment>();

        for (int i = 1; i <= 1000; i++)
        {
            var member = new Member
            {
                MemberId = i,
                FirstName = $"FirstName{i}",
                LastName = $"LastName{i}",
                Email = $"member{i}@test.com",
                Phone = $"08012345{i:D3}",
                Mkanid = 1000 + i,
                UnitId = 1,
                Unit = unit
            };
            _members.Add(member);

            // Add some roles for variety (every 10th member gets a role)
            if (i % 10 == 0)
            {
                var memberRole = new MemberLevelDepartment
                {
                    MemberId = i,
                    LevelDepartmentId = 1,
                    LevelDepartment = levelDepartment
                };
                _roles.Add(memberRole);
            }
        }
    }

    [Benchmark]
    public List<MemberDetailResponse> Transform_MemberToDetailResponse()
    {
        var response = _members.Select(member => 
        {
            var memberRoles = _roles.Where(role => role.MemberId == member.MemberId).ToList();
            return member.ToDetailResponseWithRoles(memberRoles);
        }).ToList();

        return response;
    }

    [Benchmark]
    public List<MemberSummaryResponse> Transform_MemberToSummaryResponse()
    {
        var response = _members.Select(m => new MemberSummaryResponse
        {
            MemberId = m.MemberId,
            FirstName = m.FirstName,
            LastName = m.LastName,
            Email = m.Email,
            Phone = m.Phone,
            Mkanid = m.Mkanid
        }).ToList();

        return response;
    }

    [Benchmark]
    public List<MemberSummaryResponse> SearchByName_ContainsApproach()
    {
        var searchTerm = "FirstName1";
        var results = _members
            .Where(m => m.FirstName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) || 
                       m.LastName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
            .Select(m => new MemberSummaryResponse
            {
                MemberId = m.MemberId,
                FirstName = m.FirstName,
                LastName = m.LastName,
                Email = m.Email,
                Phone = m.Phone,
                Mkanid = m.Mkanid
            })
            .ToList();

        return results;
    }

    [Benchmark]
    public List<MemberSummaryResponse> SearchByName_IndexOfApproach()
    {
        var searchTerm = "FirstName1";
        var results = _members
            .Where(m => m.FirstName.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0 || 
                       m.LastName.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0)
            .Select(m => new MemberSummaryResponse
            {
                MemberId = m.MemberId,
                FirstName = m.FirstName,
                LastName = m.LastName,
                Email = m.Email,
                Phone = m.Phone,
                Mkanid = m.Mkanid
            })
            .ToList();

        return results;
    }

    [Benchmark]
    public List<MemberSummaryResponse> FilterByUnit_LinqWhere()
    {
        var results = _members
            .Where(m => m.UnitId == 1)
            .Select(m => new MemberSummaryResponse
            {
                MemberId = m.MemberId,
                FirstName = m.FirstName,
                LastName = m.LastName,
                Email = m.Email,
                Phone = m.Phone,
                Mkanid = m.Mkanid
            })
            .ToList();

        return results;
    }

    [Benchmark]
    public List<MemberDetailResponse> RoleAggregation_Linq()
    {
        var result = new List<MemberDetailResponse>();
        
        // Simulate the role aggregation logic used in both API implementations
        foreach (var member in _members.Take(100)) // Limit to 100 for performance testing
        {
            var memberRoles = _roles.Where(role => role.MemberId == member.MemberId).ToList();
            var response = member.ToDetailResponseWithRoles(memberRoles);
            result.Add(response);
        }

        return result;
    }

    [Benchmark]
    public Dictionary<int, List<MemberLevelDepartment>> RoleAggregation_GroupBy()
    {
        // Alternative approach using GroupBy for role aggregation
        var rolesGrouped = _roles
            .GroupBy(r => r.MemberId)
            .ToDictionary(g => g.Key, g => g.ToList());

        return rolesGrouped;
    }
}