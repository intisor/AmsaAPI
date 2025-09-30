using AmsaAPI.Common;
using AmsaAPI.Models;
using CsvHelper;
using System.Globalization;
using System.Text;

namespace AmsaAPI.Services
{
    public class CsvValidationHelper
    {
        private const long MaxFileSizeBytes = 10L * 1024 * 1024;
        private static readonly string[] ExpectedHeaders = {"NAME", "UNIT", "DEPARTMENT","EMAIL","PHONE","MKANID"};
        
        public async Task<Result<CsvValidationResult>> ValidateFileStructure(Stream stream)
        {
            var csvResult = new CsvValidationResult();
            if (stream.Length is 0)
            {
                var emptyError = new ImportError
                {
                    ErrorType = ImportErrorType.EmptyFile,
                    DetailedMessage = "File is empty",
                    Severity = ImportSeverity.Error
                };
                csvResult.Errors.Add(emptyError);
                csvResult.IsValid = false;
                return Result.Failure<CsvValidationResult>(ErrorType.BadRequest, emptyError.DetailedMessage);
            }

            if (stream.Length > MaxFileSizeBytes)
            {
                var sizeError = new ImportError
                {
                    ErrorType = ImportErrorType.FileSizeExceeded,
                    DetailedMessage = $"File too large ({stream.Length} bytes > 10MB)",
                    Severity = ImportSeverity.Error
                };
                csvResult.Errors.Add(sizeError);
                csvResult.IsValid = false;
                return Result.Failure<CsvValidationResult>(ErrorType.BadRequest, sizeError.DetailedMessage);
            }

            stream.Position = 0;
            var encoding = DetectEncoding(stream);
            csvResult.DetectedEncoding = encoding.EncodingName;
            if (encoding != Encoding.UTF8)
            {
                csvResult.Errors.Add(new ImportError
                {
                    ErrorType = ImportErrorType.UnicodeEncodingError,
                    DetailedMessage = $"Non-UTF8 encoding detected: {encoding.EncodingName}. May cause character issues.",
                    Severity = ImportSeverity.Warning
                });
            }

            stream.Position = 0;
            try
            {
                using var reader = new StreamReader(stream, encoding, leaveOpen: true);
                using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

                await csv.ReadAsync();
                csv.ReadHeader();

                if (csv.HeaderRecord == null || csv.HeaderRecord.Length is 0)
                {
                    var headerError = new ImportError
                    {
                        ErrorType = ImportErrorType.HeaderValidationFailed,
                        DetailedMessage = "Header row is missing or empty",
                        RowNumber = 1,
                        Severity = ImportSeverity.Error
                    };
                    csvResult.Errors.Add(headerError);
                    csvResult.IsValid = false;
                    return Result.Failure<CsvValidationResult>(ErrorType.Validation, headerError.DetailedMessage);
                }

                var actualHeaders = csv.HeaderRecord.Select(h => h.Trim()).Where(h => !string.IsNullOrEmpty(h)).ToArray();
                var missingHeaders = ExpectedHeaders.Except(actualHeaders, StringComparer.OrdinalIgnoreCase).ToList();
                if (missingHeaders.Count != 0)
                {
                    var missingError = new ImportError
                    {
                        ErrorType = ImportErrorType.HeaderValidationFailed,
                        DetailedMessage = "Missing required headers: " + string.Join(", ", missingHeaders),
                        Severity = ImportSeverity.Error
                    };
                    csvResult.Errors.Add(missingError);
                    csvResult.IsValid = false;
                    return Result.Failure<CsvValidationResult>(ErrorType.Validation, missingError.DetailedMessage);
                }

                csvResult.IsValid = true;
                return Result.Success(csvResult);
            }
            catch (Exception ex)
            {
                var parseError = new ImportError
                {
                    ErrorType = ImportErrorType.HeaderValidationFailed,
                    DetailedMessage = $"Failed to parse CSV header: {ex.Message}",
                    Severity = ImportSeverity.Error
                };
                csvResult.Errors.Add(parseError);
                csvResult.IsValid = false;
                return Result.Failure<CsvValidationResult>(ErrorType.Validation, parseError.DetailedMessage);
            }
        }

        public async Task<Result<List<MemberImportRecord>>> ParseWithValidation(Stream stream)
        {
            var validationResult
        }
        private Encoding DetectEncoding(Stream stream)
        {
            if (stream.Length < 3)
                return Encoding.UTF8;
            byte[] buffer = new byte[3];
            stream.Read(buffer, 0, 3);
            stream.Position = 0;

            if (buffer[0] == 0xEF && buffer[1] == 0xBB && buffer[2] == 0xBF)
                return Encoding.UTF8;
            return Encoding.UTF8;
        }
    }
}
    