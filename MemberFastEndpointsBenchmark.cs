using BenchmarkDotNet.Attributes;
using AmsaAPI.Data;
using AmsaAPI.DTOs;
using AmsaAPI.Extensions;
using Microsoft.VSDiagnostics;

namespace AmsaAPI.Benchmarks;
[SimpleJob]
[CPUUsageDiagnoser]
public class MemberFastEndpointsBenchmark
{
    private List<Member> _members = null !;
    private List<MemberLevelDepartment> _roles = null !;
    [GlobalSetup]
    public void Setup()
    {
        // Create realistic test hierarchy data
        var national = new National
        {
            NationalId = 1,
            NationalName = "Nigeria"
        };
        var state = new State
        {
            StateId = 1,
            StateName = "Lagos",
            NationalId = 1,
            National = national
        };
        var unit = new Unit
        {
            UnitId = 1,
            UnitName = "Lagos State University",
            StateId = 1,
            State = state
        };
        // Create multiple departments and levels for realistic scenarios
        var departments = new List<Department>
        {
            new()
            {
                DepartmentId = 1,
                DepartmentName = "Medical"
            },
            new()
            {
                DepartmentId = 2,
                DepartmentName = "Engineering"
            },
            new()
            {
                DepartmentId = 3,
                DepartmentName = "Legal"
            },
            new()
            {
                DepartmentId = 4,
                DepartmentName = "Finance"
            }
        };
        var levels = new List<Level>
        {
            new()
            {
                LevelId = 1,
                LevelType = "Executive"
            },
            new()
            {
                LevelId = 2,
                LevelType = "Coordinator"
            },
            new()
            {
                LevelId = 3,
                LevelType = "Member"
            }
        };
        var levelDepartments = new List<LevelDepartment>();
        var ldId = 1;
        foreach (var dept in departments)
        {
            foreach (var level in levels)
            {
                levelDepartments.Add(new LevelDepartment { LevelDepartmentId = ldId++, LevelId = level.LevelId, DepartmentId = dept.DepartmentId, Level = level, Department = dept });
            }
        }

        // Create test members with realistic data patterns
        _members = new List<Member>();
        _roles = new List<MemberLevelDepartment>();
        var random = new Random(42); // Fixed seed for consistent results
        for (int i = 1; i <= 5000; i++)
        {
            var member = new Member
            {
                MemberId = i,
                FirstName = $"FirstName{i}",
                LastName = $"LastName{i}",
                Email = $"member{i}@test.com",
                Phone = $"08012345{i:D4}",
                Mkanid = 1000 + i,
                UnitId = 1,
                Unit = unit,
                MemberLevelDepartments = new List<MemberLevelDepartment>()
            };
            // Add roles with realistic distribution
            var roleCount = random.Next(0, 4); // 0-3 roles per member
            var selectedLevelDepartments = levelDepartments.OrderBy(x => random.Next()).Take(roleCount);
            foreach (var ld in selectedLevelDepartments)
            {
                var memberRole = new MemberLevelDepartment
                {
                    MemberLevelDepartmentId = _roles.Count + 1,
                    MemberId = i,
                    LevelDepartmentId = ld.LevelDepartmentId,
                    LevelDepartment = ld,
                    Member = member
                };
                _roles.Add(memberRole);
                member.MemberLevelDepartments.Add(memberRole);
            }

            _members.Add(member);
        }
    }

    [Benchmark(Baseline = true)]
    public List<MemberDetailResponse> Current_IterativeRoleMapping()
    {
        // This simulates the current approach in GetAllMembersEndpoint
        var response = _members.Select(member =>
        {
            return member.ToDetailResponseWithRoles(member.MemberLevelDepartments.ToList());
        }).ToList();
        return response;
    }

    [Benchmark]
    public List<MemberDetailResponse> GroupBy_PreAggregatedRoles()
    {
        // Optimized approach using GroupBy for role aggregation
        var rolesGrouped = _roles.GroupBy(r => r.MemberId).ToDictionary(g => g.Key, g => g.ToList());
        var response = _members.Select(member =>
        {
            var memberRoles = rolesGrouped.GetValueOrDefault(member.MemberId, new List<MemberLevelDepartment>());
            return member.ToDetailResponseWithRoles(memberRoles);
        }).ToList();
        return response;
    }

    [Benchmark]
    public List<MemberDetailResponse> GroupBy_SinglePassOptimized()
    {
        // Single pass optimization
        var rolesGrouped = _roles.GroupBy(r => r.MemberId).ToDictionary(g => g.Key, g => g.ToList());
        var response = new List<MemberDetailResponse>(_members.Count);
        foreach (var member in _members)
        {
            var memberRoles = rolesGrouped.GetValueOrDefault(member.MemberId, new List<MemberLevelDepartment>());
            response.Add(member.ToDetailResponseWithRoles(memberRoles));
        }

        return response;
    }

    [Benchmark]
    public List<MemberDetailResponse> Lookup_FastRoleRetrieval()
    {
        // Using ILookup for role retrieval
        var rolesLookup = _roles.ToLookup(r => r.MemberId);
        var response = _members.Select(member =>
        {
            var memberRoles = rolesLookup[member.MemberId].ToList();
            return member.ToDetailResponseWithRoles(memberRoles);
        }).ToList();
        return response;
    }

    [Benchmark]
    public Dictionary<int, List<MemberLevelDepartment>> RoleAggregation_GroupByDictionary()
    {
        // Isolated test of GroupBy aggregation
        return _roles.GroupBy(r => r.MemberId).ToDictionary(g => g.Key, g => g.ToList());
    }

    [Benchmark]
    public ILookup<int, MemberLevelDepartment> RoleAggregation_ToLookup()
    {
        // Alternative aggregation using ToLookup
        return _roles.ToLookup(r => r.MemberId);
    }

    [Benchmark]
    public List<MemberDetailResponse> Parallel_PlinqProcessing()
    {
        // Test parallel processing for large datasets
        var rolesGrouped = _roles.GroupBy(r => r.MemberId).ToDictionary(g => g.Key, g => g.ToList());
        var response = _members.AsParallel().Select(member =>
        {
            var memberRoles = rolesGrouped.GetValueOrDefault(member.MemberId, new List<MemberLevelDepartment>());
            return member.ToDetailResponseWithRoles(memberRoles);
        }).ToList();
        return response;
    }
}