using AmsaAPI.Tests;

namespace AmsaAPI.Tests;

public class TestRunner
{
    public static async Task Main(string[] args)
    {
        await CsvImportTests.RunAllTests();
    }
}