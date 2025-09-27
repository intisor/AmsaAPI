using BenchmarkDotNet.Attributes;
using System.Text.Json;
using AmsaAPI.Data;
using AmsaAPI.DTOs;

namespace AmsaAPI.Benchmarks;

[MemoryDiagnoser]
[SimpleJob]
public class SerializationBenchmark
{
    private Member _complexMember = null!;
    private JsonSerializerOptions _ignoreCyclesOptions = null!;
    private JsonSerializerOptions _preserveOptions = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Create complex member with circular references
        var unit = new Unit { UnitId = 1, UnitName = "Test Unit" };
        var state = new State { StateId = 1, StateName = "Test State" };
        var national = new National { NationalId = 1, NationalName = "Test National" };
        
        state.National = national;
        unit.State = state;
        
        _complexMember = new Member 
        { 
            MemberId = 1, 
            FirstName = "Test", 
            LastName = "Member",
            UnitId = 1,
            Unit = unit 
        };

        _ignoreCyclesOptions = new JsonSerializerOptions 
        { 
            ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles,
            WriteIndented = true 
        };
        
        _preserveOptions = new JsonSerializerOptions 
        { 
            ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve,
            WriteIndented = true 
        };
    }

    [Benchmark(Baseline = true)]
    public string Serialize_IgnoreCycles()
    {
        return JsonSerializer.Serialize(_complexMember, _ignoreCyclesOptions);
    }

    [Benchmark]
    public string Serialize_PreserveReferences()
    {
        return JsonSerializer.Serialize(_complexMember, _preserveOptions);
    }

    [Benchmark]
    public string Serialize_Compact()
    {
        var options = new JsonSerializerOptions 
        { 
            ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles,
            WriteIndented = false 
        };
        return JsonSerializer.Serialize(_complexMember, options);
    }

    [Benchmark]
    public string Serialize_Default()
    {
        // Test default serialization without reference handling
        try
        {
            return JsonSerializer.Serialize(new 
            {
                _complexMember.MemberId,
                _complexMember.FirstName,
                _complexMember.LastName,
                UnitName = _complexMember.Unit?.UnitName ?? string.Empty
            });
        }
        catch
        {
            return "{}"; // Handle any serialization errors
        }
    }
}