using AmsaAPI.Data;
using System.ComponentModel.DataAnnotations;

namespace AmsaAPI.Models
{
    public enum ImportErrorType
    {
        MemberNotFound,          // Maps to ErrorType.NotFound
        DepartmentNotFound,      // Maps to ErrorType.NotFound
        LevelNotFound,           // Maps to ErrorType.NotFound
        InvalidLevelAssignment,  // Maps to ErrorType.Validation
        RestrictedDepartmentAssignment, // Maps to ErrorType.Forbidden
        DuplicateAssignment,     // Maps to ErrorType.Conflict
        InvalidNameFormat,       // Maps to ErrorType.Validation
        NoDepartmentSpecified,   // Maps to ErrorType.Validation (as info)
        // CSV-specific
        InvalidCsvStructure,     // Maps to ErrorType.BadRequest
        UnicodeEncodingError,    // Maps to ErrorType.BadRequest
        FileSizeExceeded,        // Maps to ErrorType.BadRequest
        HeaderValidationFailed,  // Maps to ErrorType.Validation
        EmptyFile,               // Maps to ErrorType.BadRequest
        MalformedData,           // Maps to ErrorType.Validation
        InternationalCharacterError, // Maps to ErrorType.Validation
        ExcessiveFieldLength     // Maps to ErrorType.Validation
    }

    public enum ImportSeverity
    {
        Error,
        Warning,
        Information
    }

    public class ImportError
    {
        public ImportErrorType ErrorType { get; set; }  // Specific type
        public MemberImportRecord? RecordData { get; set; }  // Link to your record
        public string DetailedMessage { get; set; } = string.Empty;
        public int RowNumber { get; set; } = 0;
        public string? FieldName { get; set; }  // e.g., "DEPARTMENT"
        public string? OriginalValue { get; set; }  // Bad CSV value
        public string? ExpectedFormat { get; set; }  // e.g., "Max 200 chars"
        public ImportSeverity Severity { get; set; }  // Uses ImportSeverity enum: Error, Warning, Information
    }

    public class CsvValidationResult
    {
        public bool IsValid { get; set; } = true;  // Overall valid (no critical errors)
        public List<ImportError> Errors { get; set; } = new();  // Detailed list
        public string DetectedEncoding { get; set; } = "UTF-8";
    }

    public class ImportResult
    {
        public int TotalRecords { get; set; } = 0;
        public int SuccessfulImports { get; set; } = 0;
        public int FailedImports { get; set; } = 0;
        public int MemberOnlyValidated { get; set; } = 0;  // For optionals
        public List<ImportError> Errors { get; set; } = new();
        public long ProcessingTimeMs { get; set; } = 0;
        public List<MemberLevelDepartment> ImportedAssignments { get; set; } = new(); 
    }
    public class MemberImportRecord
    {
        [Required(ErrorMessage = "Name is required for member identification.")]
        [MaxLength(500, ErrorMessage = "Name too long - max 500 characters for international names.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Unit is required for organizational context.")]
        [MaxLength(200, ErrorMessage = "Unit name too long.")]
        public string Unit { get; set; } = string.Empty;

        public string Department { get; set; } = string.Empty;

        [MaxLength(50, ErrorMessage = "Level type too long.")]
        public string? Level { get; set; }

        [EmailAddress(ErrorMessage = "Invalid email format.")]
        [MaxLength(254, ErrorMessage = "Email address too long.")]
        public string? Email { get; set; }

        [Phone(ErrorMessage = "Invalid phone number format.")]
        [MaxLength(20, ErrorMessage = "Phone number too long.")]
        public string? Phone { get; set; }

        [Required(ErrorMessage = "MKAN ID is required.")]
        public int Mkanid { get; set; }
    }
}
