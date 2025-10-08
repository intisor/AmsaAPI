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
            var membersCreated = new List<Member>(); // Track member-only creations
            var stopwatch = Stopwatch.StartNew();

            using var reader = new StreamReader(csvFilePath);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            var records = csv.GetRecords<MemberImportRecord>().ToList();

            var allMembers = await _dbContext.Members
                .Include(m => m.Unit)
                    .ThenInclude(u => u.State)
                .ToListAsync();
            var allUnits = await _dbContext.Units.ToListAsync();
            var allDepartments = await _dbContext.Departments.ToListAsync();
            var allLevels = await _dbContext.Levels.ToListAsync();
            var allLevelDepartments = await _dbContext.LevelDepartments.ToListAsync();
            var existingAssignments = await _dbContext.MemberLevelDepartments.ToListAsync();

            int rowNumber = 1;
            foreach (var record in records)
            {
                rowNumber++;

                // Parse name inline
                var nameParts = record.Name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (nameParts.Length == 0)
                {
                    errors.Add(new ImportError
                    {
                        ErrorType = ImportErrorType.InvalidNameFormat,
                        DetailedMessage = "Invalid name format",
                        RecordData = record,
                        RowNumber = rowNumber,
                        Severity = ImportSeverity.Error
                    });
                    continue;
                }

                var firstName = nameParts.Length == 1 ? nameParts[0] : string.Join(" ", nameParts.Take(nameParts.Length - 1));
                var lastName = nameParts.Length == 1 ? string.Empty : nameParts.Last();

                // Find or create member inline
                var member = allMembers.FirstOrDefault(m => m.Mkanid == record.Mkanid)
                    ?? allMembers.FirstOrDefault(m =>
                        string.Equals(m.FirstName.Trim(), firstName.Trim(), StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(m.LastName.Trim(), lastName.Trim(), StringComparison.OrdinalIgnoreCase));

                bool memberWasCreated = false;
                if (member == null)
                {
                    var unit = allUnits.FirstOrDefault(u =>
                        string.Equals(u.UnitName.Trim(), record.Unit.Trim(), StringComparison.OrdinalIgnoreCase));

                    if (unit == null)
                    {
                        errors.Add(new ImportError
                        {
                            ErrorType = ImportErrorType.MemberNotFound,
                            DetailedMessage = $"Unit '{record.Unit}' not found - cannot create member '{record.Name}'",
                            RecordData = record,
                            RowNumber = rowNumber,
                            Severity = ImportSeverity.Error
                        });
                        continue;
                    }

                    member = new Member
                    {
                        FirstName = firstName,
                        LastName = lastName,
                        Email = record.Email,
                        Phone = record.Phone,
                        Mkanid = record.Mkanid,
                        UnitId = unit.UnitId
                    };

                    if (!validateOnly)
                    {
                        _dbContext.Members.Add(member);
                        await _dbContext.SaveChangesAsync();

                        member = await _dbContext.Members
                            .Include(m => m.Unit).ThenInclude(u => u.State)
                            .FirstAsync(m => m.MemberId == member.MemberId);
                    }

                    allMembers.Add(member);
                    memberWasCreated = true;
                }

                // Track member-only success if no department specified
                if (string.IsNullOrWhiteSpace(record.Department))
                {
                    if (memberWasCreated)
                    {
                        membersCreated.Add(member);
                    }
                    continue; // This is now appropriate - member handled, no assignment needed
                }

                // Validate and resolve assignment inline
                var department = allDepartments.FirstOrDefault(d =>
                    string.Equals(d.DepartmentName.Trim(), record.Department.Trim(), StringComparison.OrdinalIgnoreCase));

                if (department == null)
                {
                    errors.Add(new ImportError
                    {
                        ErrorType = ImportErrorType.DepartmentNotFound,
                        DetailedMessage = $"Department '{record.Department}' not found",
                        RecordData = record,
                        RowNumber = rowNumber,
                        Severity = ImportSeverity.Error
                    });
                    continue;
                }

                var levelType = string.IsNullOrWhiteSpace(record.Level) ? "unit" : record.Level.Trim().ToLowerInvariant();
                var level = levelType switch
                {
                    "national" => allLevels.FirstOrDefault(l => l.LevelId == 1),
                    "state" => allLevels.FirstOrDefault(l => l.StateId == member.Unit.StateId && l.UnitId == null),
                    "unit" => allLevels.FirstOrDefault(l => l.UnitId == member.UnitId),
                    _ => null
                };

                if (level == null)
                {
                    errors.Add(new ImportError
                    {
                        ErrorType = ImportErrorType.LevelNotFound,
                        DetailedMessage = $"Level '{levelType}' not found for member's organizational context",
                        RecordData = record,
                        RowNumber = rowNumber,
                        Severity = ImportSeverity.Error
                    });
                    continue;
                }

                if (VpDepartments.Contains(department.DepartmentName) && level.LevelId != 1)
                {
                    errors.Add(new ImportError
                    {
                        ErrorType = ImportErrorType.RestrictedDepartmentAssignment,
                        DetailedMessage = $"Department '{department.DepartmentName}' is restricted to National level only",
                        RecordData = record,
                        RowNumber = rowNumber,
                        Severity = ImportSeverity.Error
                    });
                    continue;
                }

                var levelDepartment = allLevelDepartments.FirstOrDefault(ld =>
                    ld.LevelId == level.LevelId && ld.DepartmentId == department.DepartmentId);

                if (levelDepartment == null)
                {
                    errors.Add(new ImportError
                    {
                        ErrorType = ImportErrorType.InvalidLevelAssignment,
                        DetailedMessage = $"Department '{department.DepartmentName}' is not available at '{levelType}' level",
                        RecordData = record,
                        RowNumber = rowNumber,
                        Severity = ImportSeverity.Error
                    });
                    continue;
                }

                var isDuplicate = existingAssignments.Any(mld =>
                        mld.MemberId == member.MemberId && mld.LevelDepartmentId == levelDepartment.LevelDepartmentId) ||
                    assignmentsToAdd.Any(mld =>
                        mld.MemberId == member.MemberId && mld.LevelDepartmentId == levelDepartment.LevelDepartmentId);

                if (isDuplicate)
                {
                    errors.Add(new ImportError
                    {
                        ErrorType = ImportErrorType.DuplicateAssignment,
                        DetailedMessage = "Duplicate assignment: Member already assigned to this department at this level",
                        RecordData = record,
                        RowNumber = rowNumber,
                        Severity = ImportSeverity.Error
                    });
                    continue;
                }

                assignmentsToAdd.Add(new MemberLevelDepartment
                {
                    MemberId = member.MemberId,
                    LevelDepartmentId = levelDepartment.LevelDepartmentId
                });
            }

            if (!validateOnly && assignmentsToAdd.Count > 0)
            {
                _dbContext.MemberLevelDepartments.AddRange(assignmentsToAdd);
                await _dbContext.SaveChangesAsync();
            }

            stopwatch.Stop();

            return new ImportResult
            {
                TotalRecords = records.Count,
                SuccessfulImports = assignmentsToAdd.Count,
                FailedImports = errors.Count,
                MemberOnlyValidated = membersCreated.Count, // Use existing property
                Errors = errors,
                ProcessingTimeMs = stopwatch.ElapsedMilliseconds,
                ImportedAssignments = validateOnly ? new List<MemberLevelDepartment>() : assignmentsToAdd
            };
        }
    }
}
