using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using AmsaAPI.Data;
using AmsaAPI;
using System.Globalization;
using CsvHelper;

namespace AmsaAPI.Tests;

public class CsvImportTests
{
    private AmsaDbContext CreateInMemoryDatabase()
    {
        var options = new DbContextOptionsBuilder<AmsaDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new AmsaDbContext(options);
        
        // Seed with test data
        SeedTestData(context);
        
        return context;
    }

    private void SeedTestData(AmsaDbContext context)
    {
        // Add test nationals
        var national = new National { NationalId = 1, NationalName = "Test National" };
        context.Nationals.Add(national);

        // Add test states
        var state = new State { StateId = 1, StateName = "Test State", NationalId = 1 };
        context.States.Add(state);

        // Add test units
        var unit = new Unit { UnitId = 1, UnitName = "Test Unit", StateId = 1 };
        context.Units.Add(unit);

        // Add test levels
        var level = new Level { LevelId = 1, LevelType = "National", NationalId = 1 };
        context.Levels.Add(level);

        // Add test departments
        var departments = new[]
        {
            new Department { DepartmentId = 1, DepartmentName = "President" },
            new Department { DepartmentId = 2, DepartmentName = "VP Admin" },
            new Department { DepartmentId = 3, DepartmentName = "VP South-West" },
            new Department { DepartmentId = 4, DepartmentName = "Finance" },
            new Department { DepartmentId = 5, DepartmentName = "Welfare" }
        };
        context.Departments.AddRange(departments);

        // Add test level departments
        var levelDepartments = new[]
        {
            new LevelDepartment { LevelDepartmentId = 1, LevelId = 1, DepartmentId = 1 },
            new LevelDepartment { LevelDepartmentId = 2, LevelId = 1, DepartmentId = 2 },
            new LevelDepartment { LevelDepartmentId = 3, LevelId = 1, DepartmentId = 3 },
            new LevelDepartment { LevelDepartmentId = 4, LevelId = 1, DepartmentId = 4 },
            new LevelDepartment { LevelDepartmentId = 5, LevelId = 1, DepartmentId = 5 }
        };
        context.LevelDepartments.AddRange(levelDepartments);

        // Add test members
        var members = new[]
        {
            new Member 
            { 
                MemberId = 1, 
                FirstName = "Abdulqudus", 
                LastName = "Sulaiman", 
                Email = "test1@example.com",
                Mkanid = 1001,
                UnitId = 1
            },
            new Member 
            { 
                MemberId = 2, 
                FirstName = "Abdul-Khabeer", 
                LastName = "Arowosere", 
                Email = "test2@example.com",
                Mkanid = 1002,
                UnitId = 1
            },
            new Member 
            { 
                MemberId = 3, 
                FirstName = "Sheriffdeen", 
                LastName = "Saula", 
                Email = "test3@example.com",
                Mkanid = 1003,
                UnitId = 1
            }
        };
        context.Members.AddRange(members);

        context.SaveChanges();
    }

    public async Task<List<string>> TestCsvImportFromString(string csvContent)
    {
        using var context = CreateInMemoryDatabase();
        var importer = new ExcoImporter(context);
        
        // Write CSV content to a temporary file
        var tempFile = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(tempFile, csvContent);
            return await importer.ImportExcoRecords(tempFile);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    public async Task TestValidCsvImport()
    {
        Console.WriteLine("\n=== Testing Valid CSV Import ===");
        
        var validCsv = @"NAME,UNIT,DEPARTMENT
Abdulqudus Sulaiman,UNILORIN,President
Abdul-Khabeer Arowosere,FUTMINNA,VP Admin
Sheriffdeen Saula,NOUN,VP South-West";

        var unmatchedRecords = await TestCsvImportFromString(validCsv);
        
        Console.WriteLine($"Valid CSV Import - Unmatched records: {unmatchedRecords.Count}");
        foreach (var record in unmatchedRecords)
        {
            Console.WriteLine($"  - {record}");
        }
    }

    public async Task TestEmptyFile()
    {
        Console.WriteLine("\n=== Testing Empty File ===");
        
        try
        {
            var unmatchedRecords = await TestCsvImportFromString("");
            Console.WriteLine($"Empty file - Unmatched records: {unmatchedRecords.Count}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Empty file error (expected): {ex.Message}");
        }
    }

    public async Task TestOnlyHeaders()
    {
        Console.WriteLine("\n=== Testing File With Only Headers ===");
        
        var headerOnlyCsv = "NAME,UNIT,DEPARTMENT";
        var unmatchedRecords = await TestCsvImportFromString(headerOnlyCsv);
        
        Console.WriteLine($"Headers only - Unmatched records: {unmatchedRecords.Count}");
        foreach (var record in unmatchedRecords)
        {
            Console.WriteLine($"  - {record}");
        }
    }

    public async Task TestEmptyValues()
    {
        Console.WriteLine("\n=== Testing Empty Values ===");
        
        var emptyValuesCsv = @"NAME,UNIT,DEPARTMENT
,UNILORIN,President
Abdul-Khabeer Arowosere,,VP Admin
Sheriffdeen Saula,NOUN,
,,";

        var unmatchedRecords = await TestCsvImportFromString(emptyValuesCsv);
        
        Console.WriteLine($"Empty values - Unmatched records: {unmatchedRecords.Count}");
        foreach (var record in unmatchedRecords)
        {
            Console.WriteLine($"  - {record}");
        }
    }

    public async Task TestSpecialCharacters()
    {
        Console.WriteLine("\n=== Testing Special Characters ===");
        
        var specialCharsCsv = @"NAME,UNIT,DEPARTMENT
Abdür-Rahman Öğretmen,UNI'LORIN,Président
José María García-López,FUTMIN/NA,VP-Admin
王小明,NOUN(Lagos),التربية";

        var unmatchedRecords = await TestCsvImportFromString(specialCharsCsv);
        
        Console.WriteLine($"Special characters - Unmatched records: {unmatchedRecords.Count}");
        foreach (var record in unmatchedRecords)
        {
            Console.WriteLine($"  - {record}");
        }
    }

    public async Task TestVariousNameFormats()
    {
        Console.WriteLine("\n=== Testing Various Name Formats ===");
        
        var nameFormatsCsv = @"NAME,UNIT,DEPARTMENT
SingleName,UNILORIN,President
Two Names,FUTMINNA,VP Admin
Three Middle Names Here,NOUN,VP South-West
A B C D E F G,UNIABUJA,Finance
Name-With-Hyphens,UNICAL,Welfare
Name.With.Dots,FUOYE,President
Name_With_Underscores,UNILORIN,VP Admin
Name With    Multiple   Spaces,FUNAAB,Finance";

        var unmatchedRecords = await TestCsvImportFromString(nameFormatsCsv);
        
        Console.WriteLine($"Various name formats - Unmatched records: {unmatchedRecords.Count}");
        foreach (var record in unmatchedRecords)
        {
            Console.WriteLine($"  - {record}");
        }
    }

    public async Task TestDuplicateRecords()
    {
        Console.WriteLine("\n=== Testing Duplicate Records ===");
        
        var duplicatesCsv = @"NAME,UNIT,DEPARTMENT
Abdulqudus Sulaiman,UNILORIN,President
Abdulqudus Sulaiman,UNILORIN,President
Abdul-Khabeer Arowosere,FUTMINNA,VP Admin
Abdul-Khabeer Arowosere,FUTMINNA,VP Admin";

        var unmatchedRecords = await TestCsvImportFromString(duplicatesCsv);
        
        Console.WriteLine($"Duplicate records - Unmatched records: {unmatchedRecords.Count}");
        foreach (var record in unmatchedRecords)
        {
            Console.WriteLine($"  - {record}");
        }
    }

    public async Task TestLongValues()
    {
        Console.WriteLine("\n=== Testing Long Values ===");
        
        var longValuesCsv = @"NAME,UNIT,DEPARTMENT
Very Long Name That Exceeds Normal Limits And Goes On For A Very Long Time With Many Words And Spaces,Very Long University Name That Exceeds Normal Database Field Limits,Very Long Department Name That Exceeds Normal Database Field Limits
Short Name,Short Unit,Short Dept";

        var unmatchedRecords = await TestCsvImportFromString(longValuesCsv);
        
        Console.WriteLine($"Long values - Unmatched records: {unmatchedRecords.Count}");
        foreach (var record in unmatchedRecords)
        {
            Console.WriteLine($"  - {record}");
        }
    }

    public async Task TestMalformedCsv()
    {
        Console.WriteLine("\n=== Testing Malformed CSV ===");
        
        try
        {
            var malformedCsv = @"WRONG_NAME,WRONG_UNIT,WRONG_DEPARTMENT
Abdulqudus Sulaiman,UNILORIN,President
Abdul-Khabeer Arowosere,FUTMINNA,VP Admin";

            var unmatchedRecords = await TestCsvImportFromString(malformedCsv);
            
            Console.WriteLine($"Malformed CSV - Unmatched records: {unmatchedRecords.Count}");
            foreach (var record in unmatchedRecords)
            {
                Console.WriteLine($"  - {record}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Malformed CSV error (expected): {ex.Message}");
        }
    }

    public async Task TestMissingColumns()
    {
        Console.WriteLine("\n=== Testing Missing Columns ===");
        
        try
        {
            var missingColumnsCsv = @"NAME,UNIT
Abdulqudus Sulaiman,UNILORIN
Abdul-Khabeer Arowosere,FUTMINNA";

            var unmatchedRecords = await TestCsvImportFromString(missingColumnsCsv);
            
            Console.WriteLine($"Missing columns - Unmatched records: {unmatchedRecords.Count}");
            foreach (var record in unmatchedRecords)
            {
                Console.WriteLine($"  - {record}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Missing columns error (expected): {ex.Message}");
        }
    }

    public static async Task RunAllTests()
    {
        var tests = new CsvImportTests();
        
        Console.WriteLine("=== CSV Import Edge Case Testing ===");
        
        try
        {
            await tests.TestValidCsvImport();
            await tests.TestEmptyFile();
            await tests.TestOnlyHeaders();
            await tests.TestEmptyValues();
            await tests.TestSpecialCharacters();
            await tests.TestVariousNameFormats();
            await tests.TestDuplicateRecords();
            await tests.TestLongValues();
            await tests.TestMalformedCsv();
            await tests.TestMissingColumns();
            
            Console.WriteLine("\n=== All CSV Import Tests Completed ===");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nTest execution failed: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }
}