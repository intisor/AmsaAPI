using BenchmarkDotNet.Running;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Exporters;
using AmsaAPI.Benchmarks;

namespace AmsaAPI;

public class BenchmarkRunner
{
    public static void RunBenchmarks()
    {
        Console.WriteLine("🚀 Welcome to the AMSA API Performance Benchmark!");
        Console.WriteLine("═══════════════════════════════════════════════");
        Console.WriteLine();
        Console.WriteLine("This benchmark will analyze the performance of your API implementation:");
        Console.WriteLine("• Data transformation operations (Entity → DTO mapping)");
        Console.WriteLine("• Search algorithms and filtering operations");
        Console.WriteLine("• Statistics queries (EF LINQ optimization)");
        Console.WriteLine("• Serialization performance");
        Console.WriteLine("• API comparison (FastEndpoints vs Minimal API)");
        Console.WriteLine();
        Console.WriteLine("Press any key to start the benchmark...");
        Console.ReadKey();
        Console.WriteLine();

        var config = DefaultConfig.Instance
            .AddLogger(ConsoleLogger.Default)
            .AddColumnProvider(DefaultColumnProviders.Instance)
            .AddExporter(MarkdownExporter.GitHub);

        BenchmarkDotNet.Running.BenchmarkRunner.Run<ApiPerformanceBenchmark>(config);
        BenchmarkDotNet.Running.BenchmarkRunner.Run<MemberFastEndpointsBenchmark>(config);
        BenchmarkDotNet.Running.BenchmarkRunner.Run<DepartmentFastEndpointsBenchmark>(config);
        BenchmarkDotNet.Running.BenchmarkRunner.Run<OrganizationFastEndpointsBenchmark>(config);
        BenchmarkDotNet.Running.BenchmarkRunner.Run<StatisticsQueryBenchmark>(config);
        BenchmarkDotNet.Running.BenchmarkRunner.Run<SerializationBenchmark>(config);
        BenchmarkDotNet.Running.BenchmarkRunner.Run<ApiComparisonBenchmark>(config);
        
        Console.WriteLine();
        Console.WriteLine("✅ Benchmark completed! Key insights:");
        Console.WriteLine("═══════════════════════════════════");
        Console.WriteLine();
        Console.WriteLine("📊 Performance Optimization Opportunities:");
        Console.WriteLine("• Consider using GroupBy for role aggregation in high-volume scenarios");
        Console.WriteLine("• Evaluate if complex DetailResponse mapping is always necessary");
        Console.WriteLine("• Benchmark database-level vs in-memory operations");
        Console.WriteLine("• Consider expanding optimized query patterns");
        Console.WriteLine();
        Console.WriteLine("Reports generated successfully!");
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }
}