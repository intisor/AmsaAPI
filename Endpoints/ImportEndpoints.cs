using AmsaAPI.Data;
using AmsaAPI.DTOs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace AmsaAPI.Endpoints
{
    public static class ImportEndpoints
    {
        public static void MapImportEndpoints(this WebApplication app)
        {
            var importGroup = app.MapGroup("/api/import").WithTags("Import");

            // Test CSV file exists
            importGroup.MapGet("/test", TestCsvFileExists);
            
            // Import EXCO members from default CSV
            importGroup.MapGet("/exco", ImportExcoFromDefaultCsv);
            
            // Upload and import CSV file
            importGroup.MapPost("/exco/upload", UploadAndImportCsv);
        }

        private static Task<IResult> TestCsvFileExists()
        {
            try
            {
                var csvPath = Path.Combine(Directory.GetCurrentDirectory(), "excos_list_updated.csv");
                
                return Task.FromResult(Results.Ok(new
                {
                    CsvFileExists = File.Exists(csvPath),
                    CsvPath = csvPath,
                    CurrentDirectory = Directory.GetCurrentDirectory()
                }));
            }
            catch (Exception ex)
            {
                return Task.FromResult(Results.Problem($"Error checking CSV file: {ex.Message}"));
            }
        }

        private static async Task<IResult> ImportExcoFromDefaultCsv(AmsaDbContext dbContext)
        {
            try
            {
                var csvPath = Path.Combine(Directory.GetCurrentDirectory(), "excos_list_updated.csv");
                
                if (!File.Exists(csvPath))
                {
                    return Results.BadRequest($"Default CSV file not found at: {csvPath}");
                }

                var importer = new ExcoImporter(dbContext);
                var unmatchedRecords = await importer.ImportExcoRecords();

                var response = new ImportResponse
                {
                    Message = unmatchedRecords.Any()
                        ? "Import completed with unmatched records."
                        : "Import completed successfully.",
                    UnmatchedRecords = unmatchedRecords,
                    UnmatchedCount = unmatchedRecords.Count
                };

                return Results.Ok(response);
            }
            catch (FileNotFoundException ex)
            {
                return Results.NotFound($"CSV file not found: {ex.Message}");
            }
            catch (Exception ex)
            {
                return Results.Problem($"Import failed: {ex.Message}");
            }
        }

        private static async Task<IResult> UploadAndImportCsv(IFormFile csvFile, AmsaDbContext dbContext)
        {
            try
            {
                // Validate file upload
                if (csvFile == null || csvFile.Length == 0)
                    return Results.BadRequest("No file uploaded or file is empty");

                if (!csvFile.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                    return Results.BadRequest("Only CSV files are allowed");

                // Validate file size (limit to 10MB)
                const long maxFileSize = 10 * 1024 * 1024; // 10MB
                if (csvFile.Length > maxFileSize)
                    return Results.BadRequest("File size exceeds 10MB limit");

                var importer = new ExcoImporter(dbContext);
                var tempPath = Path.GetTempFileName();

                try
                {
                    // Save uploaded file to temporary location
                    using (var stream = new FileStream(tempPath, FileMode.Create))
                    {
                        await csvFile.CopyToAsync(stream);
                    }

                    // Import from temporary file
                    var unmatchedRecords = await importer.ImportExcoRecords(tempPath);

                    var response = new ImportResponse
                    {
                        Message = unmatchedRecords.Any()
                            ? "Import completed with unmatched records."
                            : "Import completed successfully.",
                        UnmatchedRecords = unmatchedRecords,
                        UnmatchedCount = unmatchedRecords.Count
                    };

                    return Results.Ok(response);
                }
                finally
                {
                    // Clean up temporary file
                    if (File.Exists(tempPath))
                    {
                        File.Delete(tempPath);
                    }
                }
            }
            catch (Exception ex)
            {
                return Results.Problem($"Import failed: {ex.Message}");
            }
        }
    }
}