using AmsaAPI;
using AmsaAPI.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace AmsaNigeriaApi.Endpoints
{
    public static class MemberEndpoints
    {
        public static void MapMemberEndpoints(this WebApplication app)
        {
            // Import EXCO members from CSV
            app.MapGet("/api/import/exco", async (AmsaDbContext dbContext) =>
            {
                try
                {
                    var importer = new ExcoImporter(dbContext);
                    var unmatchedRecords = await importer.ImportExcoRecords();
                    
                    if (unmatchedRecords.Any())
                    {
                        return Results.Ok(new { 
                            Message = "Import completed with unmatched records.", 
                            UnmatchedRecords = unmatchedRecords,
                            UnmatchedCount = unmatchedRecords.Count
                        });
                    }
                    
                    return Results.Ok(new { Message = "Import completed successfully." });
                }
                catch (Exception ex)
                {
                    return Results.Problem($"Import failed: {ex.Message}");
                }
            });

            // Upload and import CSV file
            app.MapPost("/api/import/exco/upload", async (IFormFile csvFile, AmsaDbContext dbContext) =>
            {
                try
                {
                    if (csvFile == null || csvFile.Length == 0)
                        return Results.BadRequest("No file uploaded");

                    if (!csvFile.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                        return Results.BadRequest("Only CSV files are allowed");
                    
                    var importer = new ExcoImporter(dbContext);
                    var tempPath = Path.GetTempFileName();
                    
                    using (var stream = new FileStream(tempPath, FileMode.Create))
                    {
                        await csvFile.CopyToAsync(stream);
                    }

                    var unmatchedRecords = await importer.ImportExcoRecords(tempPath);
                    File.Delete(tempPath);
                    
                    if (unmatchedRecords.Any())
                    {
                        return Results.Ok(new { 
                            Message = "Import completed with unmatched records.", 
                            UnmatchedRecords = unmatchedRecords,
                            UnmatchedCount = unmatchedRecords.Count
                        });
                    }
                    
                    return Results.Ok(new { Message = "Import completed successfully." });
                }
                catch (Exception ex)
                {
                    return Results.Problem($"Import failed: {ex.Message}");
                }
            });

            // Test CSV file exists
            app.MapGet("/api/import/test", () =>
            {
                var csvPath = Path.Combine(Directory.GetCurrentDirectory(), "excos_list_updated.csv");
                return Results.Ok(new 
                { 
                    CsvFileExists = File.Exists(csvPath),
                    CsvPath = csvPath,
                    CurrentDirectory = Directory.GetCurrentDirectory()
                });
            });
        }
    }
}