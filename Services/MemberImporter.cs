using AmsaAPI.Data;
using AmsaAPI.Models;
using CsvHelper;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Globalization;

namespace AmsaAPI.Services
{
    public class MemberImporter
    {
        private readonly AmsaDbContext _dbContext;
        private static readonly HashSet<string> VpDepartments = new(StringComparer.OrdinalIgnoreCase)
        {
            "VP Admin",
            "VP South-West",
            "VP North",
            "VP South-South/South-East"
        };

        public MemberImporter(AmsaDbContext context)
        {
            _dbContext = context;
        }

        public async Task<ImportResult> ImportMemberRecords(Stream stream, bool validateOnly = false)
        {
            var tempPath = Path.GetTempFileName();
            try
            {
                using (var fileStream = new FileStream(tempPath, FileMode.Create))
                {
                    await stream.CopyToAsync(fileStream);
                }
                return await ImportMemberRecords(tempPath, validateOnly);
            }
            finally
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }
        }

        public async Task<ImportResult> ImportMemberRecords(string csvFilePath, bool validateOnly = false)
        {
            var errors = new List<ImportError>();
            var assignmentsToAdd = new List<MemberLevelDepartment>();
            var stopwatch = Stopwatch.StartNew();

            // Read CSV
            using var reader = new StreamReader(csvFilePath);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            var records = csv.GetRecords<MemberImportRecord>().ToList();

            // Load all data into memory
            var allMembers = await _dbContext.Members
                .Include(m => m.Unit)
                .ThenInclude(u => u.State)
                .ToListAsync();
            var allDepartments = await _dbContext.Departments.ToListAsync();
            var allLevels = await _dbContext.Levels.ToListAsync();
            var allLevelDepartments = await _dbContext.LevelDepartments.ToListAsync();
            var existingAssignments = await _dbContext.MemberLevelDepartments.ToListAsync();

            // Process each record
            int rowNumber = 1;
            foreach (var record in records)
            {
                rowNumber++;

                // Parse name
                var nameParts = record.Name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                string firstName, lastName;

                if (nameParts.Length >= 2)
                {
                    firstName = string.Join(" ", nameParts.Take(nameParts.Length - 1));
                    lastName = nameParts.Last();
                }
                else if (nameParts.Length == 1)
                {
                    firstName = nameParts[0];
                    lastName = "";
                }
                else
                {
                    errors.Add(CreateError(ImportErrorType.InvalidNameFormat, 
                        "Invalid name format", record, rowNumber));
                    continue;
                }

                // Lookup member
                var member = allMembers.FirstOrDefault(m =>
                    string.Equals(m.FirstName.Trim(), firstName.Trim(), StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(m.LastName.Trim(), lastName.Trim(), StringComparison.OrdinalIgnoreCase));

                if (member == null)
                {
                    errors.Add(CreateError(ImportErrorType.MemberNotFound, 
                        $"Member '{record.Name}' not found", record, rowNumber));
                    continue;
                }

                // Handle member-only import (no department assignment)
                if (string.IsNullOrWhiteSpace(record.Department))
                {
                    continue;
                }

                // Lookup department
                var department = allDepartments.FirstOrDefault(d =>
                    string.Equals(d.DepartmentName.Trim(), record.Department.Trim(), StringComparison.OrdinalIgnoreCase));

                if (department == null)
                {
                    errors.Add(CreateError(ImportErrorType.DepartmentNotFound, 
                        $"Department '{record.Department}' not found", record, rowNumber));
                    continue;
                }

                // Resolve level
                var levelType = string.IsNullOrWhiteSpace(record.Level) ? "Unit" : record.Level.Trim();
                Level? level = null;

                if (string.Equals(levelType, "National", StringComparison.OrdinalIgnoreCase))
                {
                    level = allLevels.FirstOrDefault(l => l.LevelId == 1);
                }
                else if (string.Equals(levelType, "State", StringComparison.OrdinalIgnoreCase))
                {
                    level = allLevels.FirstOrDefault(l => 
                        l.StateId == member.Unit.StateId && l.UnitId == null);
                }
                else if (string.Equals(levelType, "Unit", StringComparison.OrdinalIgnoreCase))
                {
                    level = allLevels.FirstOrDefault(l => l.UnitId == member.UnitId);
                }

                if (level == null)
                {
                    errors.Add(CreateError(ImportErrorType.LevelNotFound, 
                        $"Level '{levelType}' not found for member's organizational context", record, rowNumber));
                    continue;
                }

                // Validate VP department restriction
                if (VpDepartments.Contains(department.DepartmentName) && level.LevelId != 1)
                {
                    errors.Add(CreateError(ImportErrorType.RestrictedDepartmentAssignment, 
                        $"Department '{department.DepartmentName}' is restricted to National level only", record, rowNumber));
                    continue;
                }

                // Find LevelDepartment
                var levelDepartment = allLevelDepartments.FirstOrDefault(ld =>
                    ld.LevelId == level.LevelId && ld.DepartmentId == department.DepartmentId);

                if (levelDepartment == null)
                {
                    errors.Add(CreateError(ImportErrorType.InvalidLevelAssignment, 
                        $"Department '{department.DepartmentName}' is not available at '{levelType}' level", record, rowNumber));
                    continue;
                }

                // Check duplicate
                var isDuplicate = existingAssignments.Any(mld =>
                    mld.MemberId == member.MemberId &&
                    mld.LevelDepartmentId == levelDepartment.LevelDepartmentId);

                if (!isDuplicate)
                {
                    isDuplicate = assignmentsToAdd.Any(mld =>
                        mld.MemberId == member.MemberId &&
                        mld.LevelDepartmentId == levelDepartment.LevelDepartmentId);
                }

                if (isDuplicate)
                {
                    errors.Add(CreateError(ImportErrorType.DuplicateAssignment, 
                        $"Duplicate assignment: Member already assigned to this department at this level", record, rowNumber));
                    continue;
                }

                // Add to batch
                assignmentsToAdd.Add(new MemberLevelDepartment
                {
                    MemberId = member.MemberId,
                    LevelDepartmentId = levelDepartment.LevelDepartmentId
                });
            }

            // Batch insert (only if not validateOnly)
            if (!validateOnly && assignmentsToAdd.Count > 0)
            {
                await _dbContext.MemberLevelDepartments.AddRangeAsync(assignmentsToAdd);
                await _dbContext.SaveChangesAsync();
            }

            stopwatch.Stop();

            // Build and return ImportResult
            return new ImportResult
            {
                TotalRecords = records.Count,
                SuccessfulImports = assignmentsToAdd.Count,
                FailedImports = errors.Count,
                Errors = errors,
                ProcessingTimeMs = stopwatch.ElapsedMilliseconds,
                ImportedAssignments = validateOnly ? new List<MemberLevelDepartment>() : assignmentsToAdd
            };
        }

        private ImportError CreateError(ImportErrorType type, string message, MemberImportRecord record, int rowNumber)
        {
            return new ImportError
            {
                ErrorType = type,
                DetailedMessage = message,
                RecordData = record,
                RowNumber = rowNumber,
                Severity = ImportSeverity.Error
            };
        }
    }
}
