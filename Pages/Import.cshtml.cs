using AmsaAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AmsaAPI.Pages;

public class ImportModel : PageModel
{
    private readonly CsvValidationHelper _validator;
    private readonly MemberImporter _importer;

    public ImportModel(CsvValidationHelper validator, MemberImporter importer)
    {
        _validator = validator;
        _importer = importer;
    }

    [BindProperty]
    public IFormFile? CsvFile { get; set; }

    public string? Message { get; set; }
    public bool? Success { get; set; }
    public int TotalRecords { get; set; }
    public int SuccessfulImports { get; set; }
    public int FailedImports { get; set; }
    public List<string>? Errors { get; set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (CsvFile == null)
        {
            Message = "Select a CSV file";
            Success = false;
            return Page();
        }

        try
        {
            using var stream = CsvFile.OpenReadStream();
            var result = await _importer.ImportMemberRecords(stream, validateOnly: false);

            TotalRecords = result.TotalRecords;
            SuccessfulImports = result.SuccessfulImports;
            FailedImports = result.FailedImports;
            Success = FailedImports == 0;
            Message = (Success == true)
                ? $"Imported {SuccessfulImports} members" 
                : $"{SuccessfulImports} imported, {FailedImports} failed";
            
            if (result.Errors?.Any() == true)
            {
                Errors = result.Errors.Take(10).Select(e => $"Row {e.RowNumber}: {e.DetailedMessage}").ToList();
            }

            return Page();
        }
        catch (Exception ex)
        {
            Message = $"Error: {ex.Message}";
            Success = false;
            return Page();
        }
    }
}
