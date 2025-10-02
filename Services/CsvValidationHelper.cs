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
        private static readonly string[] ExpectedHeaders = {"NAME", "UNIT", "DEPARTMENT", "LEVEL", "EMAIL","PHONE","MKANID"};
        
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
            var validationResult = await ValidateFileStructure(stream);
            if (!validationResult.IsSuccess)
            {
                return Result.Failure<List<MemberImportRecord>>(validationResult.ErrorType, validationResult.ErrorMessage);
            }

            var records = new List<MemberImportRecord>();
            var errors = new List<ImportError>();

            try
            {
                stream.Position = 0;
                var encoding = DetectEncoding(stream);
                stream.Position = 0;

                using var reader = new StreamReader(stream, encoding, leaveOpen: true);
                using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

                await csv.ReadAsync();
                csv.ReadHeader();

                int rowNumber = 2;

                while (await csv.ReadAsync())
                {
                    var parseResult = ParseSingleRecord(csv, rowNumber);

                    if (parseResult.IsSuccess)
                    {
                        records.Add(parseResult.Value!);
                    }
                    else
                    {
                        errors.Add(CreateImportError(parseResult.ErrorMessage, rowNumber));
                    }

                    rowNumber++;
                }

                var criticalErrors = errors.Where(e => e.Severity == ImportSeverity.Error).ToList();
                if (criticalErrors.Any())
                {
                    var errorMessage = string.Join("; ", criticalErrors.Select(e => $"Row {e.RowNumber}: {e.DetailedMessage}"));
                    return Result.Failure<List<MemberImportRecord>>(ErrorType.Validation, errorMessage);
                }

                return Result.Success(records);
            }
            catch (Exception ex)
            {
                return Result.Failure<List<MemberImportRecord>>(ErrorType.BadRequest, $"Failed to parse CSV: {ex.Message}");
            }
        }

        private Result<MemberImportRecord> ParseSingleRecord(CsvReader csv, int rowNumber)
        {
            try
            {
                var record = new MemberImportRecord
                {
                    Name = GetCsvField(csv, "NAME")?.Trim() ?? string.Empty,
                    Unit = GetCsvField(csv, "UNIT")?.Trim() ?? string.Empty,
                    Department = GetCsvField(csv, "DEPARTMENT")?.Trim() ?? string.Empty,
                    Level = GetCsvField(csv, "LEVEL")?.Trim(),
                    Email = GetCsvField(csv, "EMAIL")?.Trim(),
                    Phone = GetCsvField(csv, "PHONE")?.Trim()
                };

                var mkanidStr = GetCsvField(csv, "MKANID")?.Trim();
                if (!int.TryParse(mkanidStr, out int mkanid))
                {
                    // Log as warning if you want, or treat as error if required
                    return Result.Failure<MemberImportRecord>(ErrorType.Validation, $"Invalid MKANID: '{mkanidStr}'");
                }
                record.Mkanid = mkanid;

                var validationResult = ValidateRecord(record);
                if (!validationResult.IsSuccess)
                {
                    return validationResult;
                }

                return Result.Success(record);
            }
            catch (Exception ex)
            {
                return Result.Failure<MemberImportRecord>(ErrorType.Validation, $"Error parsing record: {ex.Message}");
            }
        }

        private Result<MemberImportRecord> ValidateRecord(MemberImportRecord record)
        {
            if (string.IsNullOrWhiteSpace(record.Name))
                return Result.Failure<MemberImportRecord>(ErrorType.Validation, "Name is required");

            if (string.IsNullOrWhiteSpace(record.Unit))
                return Result.Failure<MemberImportRecord>(ErrorType.Validation, "Unit is required");

            if (record.Name.Length > 500)
                return Result.Failure<MemberImportRecord>(ErrorType.Validation, "Name too long");

            if (record.Unit.Length > 200)
                return Result.Failure<MemberImportRecord>(ErrorType.Validation, "Unit name too long");

            // Department and Level can be empty (member-only path)
            // Email/Phone are optional, but you can add format/length checks if needed

            return Result.Success(record);
        }

        private string? GetCsvField(CsvReader csv, string fieldName)
        {
            return csv.TryGetField(fieldName, out string? value) ? value : null;
        }

        private ImportError CreateImportError(string message, int rowNumber)
        {
            return new ImportError
            {
                ErrorType = ImportErrorType.MalformedData,
                DetailedMessage = message,
                RowNumber = rowNumber,
                Severity = ImportSeverity.Error
            };
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