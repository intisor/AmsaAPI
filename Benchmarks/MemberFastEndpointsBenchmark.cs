using BenchmarkDotNet.Attributes;
using AmsaAPI.Data;
using AmsaAPI.DTOs;
using AmsaAPI.Extensions;

namespace AmsaAPI.Benchmarks;

[MemoryDiagnoser]
[SimpleJob]
public class MemberFastEndpointsBenchmark
{
    private List<Member> _members = null!;
    private List<MemberLevelDepartment> _roles = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Create test hierarchy data efficiently
        var national = new National { NationalId = 1, NationalName = "Nigeria" };
        var state = new State { StateId = 1, StateName = "Lagos", NationalId = 1, National = national };
        var unit = new Unit { UnitId = 1, UnitName = "Lagos State University", StateId = 1, State = state };

        // Create departments and levels
        var departments = Enumerable.Range(1, 4).Select(i => 
            new Department { DepartmentId = i, DepartmentName = $"Department{i}" }).ToList();
        
        var levels = Enumerable.Range(1, 3).Select(i => 
            new Level { LevelId = i, LevelType = $"Level{i}" }).ToList();

        var levelDepartments = new List<LevelDepartment>();
        var ldId = 1;
        foreach (var dept in departments)
        {
            foreach (var level in levels)
            {
                levelDepartments.Add(new LevelDepartment 
                { 
                    LevelDepartmentId = ldId++, 
                    LevelId = level.LevelId, 
                    DepartmentId = dept.DepartmentId, 
                    Level = level, 
                    Department = dept 
                });
            }
        }

        // Create test data
        _members = new List<Member>();
        _roles = new List<MemberLevelDepartment>();
        var random = new Random(42); // Fixed seed for consistent results
        
        for (int i = 1; i <= 1000; i++) // Reduced for faster benchmarks
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
            
            // Add 0-2 roles per member for realistic distribution
            var roleCount = random.Next(0, 3);
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
    public List<MemberDetailResponse> Current_IterativeMapping()
    {
        return _members.Select(member =>
            member.ToDetailResponseWithRoles(member.MemberLevelDepartments.ToList())
        ).ToList();
    }

    [Benchmark]
    public List<MemberDetailResponse> Optimized_GroupByPreAggregation()
    {
        var rolesGrouped = _roles.GroupBy(r => r.MemberId).ToDictionary(g => g.Key, g => g.ToList());
        return _members.Select(member =>
        {
            var memberRoles = rolesGrouped.GetValueOrDefault(member.MemberId, new List<MemberLevelDepartment>());
            return member.ToDetailResponseWithRoles(memberRoles);
        }).ToList();
    }

    [Benchmark]
    public List<MemberDetailResponse> Optimized_ToLookup()
    {
        var rolesLookup = _roles.ToLookup(r => r.MemberId);
        return _members.Select(member =>
        {
            var memberRoles = rolesLookup[member.MemberId].ToList();
            return member.ToDetailResponseWithRoles(memberRoles);
        }).ToList();
    }

    [Benchmark]
    public List<MemberDetailResponse> Optimized_GroupJoin()
    {
        return _members.GroupJoin(_roles,
            m => m.MemberId,
            r => r.MemberId,
            (m, roles) => new MemberDetailResponse 
            { 
                MemberId = m.MemberId,
                FirstName = m.FirstName,
                LastName = m.LastName,
                Email = m.Email,
                Phone = m.Phone,
                Mkanid = m.Mkanid,
                Unit = new UnitHierarchyDto
                {
                    UnitId = m.Unit?.UnitId ?? 0,
                    UnitName = m.Unit?.UnitName ?? string.Empty,
                    State = new StateHierarchyDto
                    {
                        StateId = m.Unit?.State?.StateId ?? 0,
                        StateName = m.Unit?.State?.StateName ?? string.Empty,
                        National = new NationalDto
                        {
                            NationalId = m.Unit?.State?.National?.NationalId ?? 0,
                            NationalName = m.Unit?.State?.National?.NationalName ?? string.Empty
                        }
                    }
                },
                Roles = roles.Select(r => new DepartmentAtLevelDto 
                { 
                    DepartmentName = r.LevelDepartment?.Department?.DepartmentName ?? "",
                    LevelType = r.LevelDepartment?.Level?.LevelType ?? ""
                }).ToList()
            }).ToList();
    }

    [Benchmark]
    public List<MemberSummaryResponse> Transform_ToSummary()
    {
        return _members.Select(m => new MemberSummaryResponse 
        { 
            MemberId = m.MemberId, 
            FirstName = m.FirstName,
            LastName = m.LastName,
            Email = m.Email,
            Phone = m.Phone,
            Mkanid = m.Mkanid
        }).ToList();
    }
}