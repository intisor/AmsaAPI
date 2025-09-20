using BenchmarkDotNet.Running;
using AmsaAPI.Benchmarks;

namespace AmsaAPI;

public class BenchmarkRunner
{
    public static void RunBenchmarks()
    {
        Console.WriteLine("?? Welcome to the AMSA API Performance Benchmark!");
        Console.WriteLine("???????????????????????????????????????????????");
        Console.WriteLine();
        Console.WriteLine("This benchmark will analyze the performance of your dual API implementation:");
        Console.WriteLine("• Data transformation operations (Entity ? DTO mapping)");
        Console.WriteLine("• Search algorithms (Contains vs IndexOf)");
        Console.WriteLine("• Role aggregation strategies (LINQ vs GroupBy)");
        Console.WriteLine("• Filtering operations");
        Console.WriteLine();
        Console.WriteLine("Press any key to start the benchmark...");
        Console.ReadKey();
        Console.WriteLine();

        var summary = BenchmarkDotNet.Running.BenchmarkRunner.Run<ApiPerformanceBenchmark>();
        
        Console.WriteLine();
        Console.WriteLine("?? Benchmark completed! Here are the key insights:");
        Console.WriteLine("???????????????????????????????????????????????");
        Console.WriteLine();
        Console.WriteLine("?? What to look for in the results:");
        Console.WriteLine("• Transform_MemberToDetailResponse vs Transform_MemberToSummaryResponse");
        Console.WriteLine("  ? Compare complex object mapping vs simple projection");
        Console.WriteLine();
        Console.WriteLine("• SearchByName_ContainsApproach vs SearchByName_IndexOfApproach");
        Console.WriteLine("  ? Your FastEndpoints uses Contains, Minimal API uses EF.Functions.Like");
        Console.WriteLine();
        Console.WriteLine("• RoleAggregation_Linq vs RoleAggregation_GroupBy");
        Console.WriteLine("  ? Current approach vs optimized GroupBy aggregation");
        Console.WriteLine();
        Console.WriteLine("?? Performance Optimization Opportunities:");
        Console.WriteLine("• Consider using GroupBy for role aggregation in high-volume scenarios");
        Console.WriteLine("• Evaluate if complex DetailResponse mapping is always necessary");
        Console.WriteLine("• Benchmark database-level vs in-memory operations");
        Console.WriteLine();
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }
}