using AmsaAPI.Common;
using AmsaAPI.Models;
using System.Text;

namespace AmsaAPI.Services
{
    public class CsvValidationHelper
    {
        private const long MaxFileSizeBytes = 10L * 1024 * 1024;
        private static readonly string[] ExpectedHeaders = {"NAME", "UNIT", "DEPARTMENT","EMAIL","PHONE","MKANID"}
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
            using var reader = new StreamReader(stream,encoding,leaveOpen:true);
            var headerLine = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(headerLine))
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

            var actualHeaders = 
        }

        private string[] ParseCsvLine(string line)
        {
            if (string.IsNullOrEmpty(line))
                return [];

            var fields = new List<string>();
            var currentField = new StringBuilder();
            bool inQuotes = false;

            for(int i = 0; i < line.Length;i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    
                }
            }
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
