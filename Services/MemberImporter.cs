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

                // Parse name
                var (firstName, lastName, nameError) = ParseName(record.Name);
                if (nameError != null)
                {
                    errors.Add(CreateError(ImportErrorType.InvalidNameFormat, nameError, record, rowNumber));
                    continue;
                }

                // Find or create member
                var memberResult = await FindOrCreateMember(record, firstName, lastName, allMembers, allUnits);
                if (!memberResult.Success)
                {
                    errors.Add(CreateError(ImportErrorType.MemberNotFound, memberResult.ErrorMessage!, record, rowNumber));
                    continue;
                }
                var member = memberResult.Member!;

                // Skip if no department specified
                if (string.IsNullOrWhiteSpace(record.Department))
                {
                    continue;
                }

                // Validate and resolve assignment: Department -> Level -> LevelDepartment -> Duplicate
                var assignmentResult = ValidateAndResolveAssignment(
                    record, member, allDepartments, allLevels, allLevelDepartments,
                    existingAssignments, assignmentsToAdd);

                if (!assignmentResult.Success)
                {
                    errors.Add(CreateError(assignmentResult.ErrorType, assignmentResult.ErrorMessage!, record, rowNumber));
                    continue;
                }

                // Add validated assignment
                assignmentsToAdd.Add(new MemberLevelDepartment
                {
                    MemberId = member.MemberId,
                    LevelDepartmentId = assignmentResult.LevelDepartmentId
                });
            }

            // Batch insert assignments
            if (!validateOnly && assignmentsToAdd.Count > 0)
            {
                _dbContext.MemberLevelDepartments.AddRange(assignmentsToAdd);
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

        private (string FirstName, string LastName, string? Error) ParseName(string name)
        {
            var nameParts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            return nameParts.Length switch
            {
                0 => (string.Empty, string.Empty, "Invalid name format"),
                1 => (nameParts[0], string.Empty, null),
                _ => (string.Join(" ", nameParts.Take(nameParts.Length - 1)), nameParts.Last(), null)
            };
        }

        private async Task<(bool Success, Member? Member, string? ErrorMessage)> FindOrCreateMember(
            MemberImportRecord record, string firstName, string lastName,
            List<Member> allMembers, List<Unit> allUnits)
        {
            // Try find by MKAN ID first, then by name
            var member = allMembers.FirstOrDefault(m => m.Mkanid == record.Mkanid)
                ?? allMembers.FirstOrDefault(m =>
                    string.Equals(m.FirstName.Trim(), firstName.Trim(), StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(m.LastName.Trim(), lastName.Trim(), StringComparison.OrdinalIgnoreCase));

            if (member != null)
                return (true, member, null);

            // Create new member
            var unit = allUnits.FirstOrDefault(u =>
                string.Equals(u.UnitName.Trim(), record.Unit.Trim(), StringComparison.OrdinalIgnoreCase));

            if (unit == null)
                return (false, null, $"Unit '{record.Unit}' not found - cannot create member '{record.Name}'");

            member = new Member
            {
                FirstName = firstName,
                LastName = lastName,
                Email = record.Email,
                Phone = record.Phone,
                Mkanid = record.Mkanid,
                UnitId = unit.UnitId
            };

            _dbContext.Members.Add(member);
            await _dbContext.SaveChangesAsync();

            member = await _dbContext.Members
                .Include(m => m.Unit).ThenInclude(u => u.State)
                .FirstAsync(m => m.MemberId == member.MemberId);

            allMembers.Add(member);
            return (true, member, null);
        }

        private (bool Success, ImportErrorType ErrorType, string? ErrorMessage, int LevelDepartmentId) ValidateAndResolveAssignment(
            MemberImportRecord record, Member member,
            List<Department> allDepartments, List<Level> allLevels, List<LevelDepartment> allLevelDepartments,
            List<MemberLevelDepartment> existingAssignments, List<MemberLevelDepartment> assignmentsToAdd)
        {
            // 1. Department must exist
            var department = allDepartments.FirstOrDefault(d =>
                string.Equals(d.DepartmentName.Trim(), record.Department.Trim(), StringComparison.OrdinalIgnoreCase));

            if (department == null)
                return (false, ImportErrorType.DepartmentNotFound, $"Department '{record.Department}' not found", 0);

            // 2. Resolve level for member's context
            var levelType = string.IsNullOrWhiteSpace(record.Level) ? "Unit" : record.Level.Trim();
            var level = ResolveLevelForMember(levelType, member, allLevels);

            if (level == null)
                return (false, ImportErrorType.LevelNotFound, 
                    $"Level '{levelType}' not found for member's organizational context", 0);

            // 3. Check VP department restrictions
            if (VpDepartments.Contains(department.DepartmentName) && level.LevelId != 1)
                return (false, ImportErrorType.RestrictedDepartmentAssignment,
                    $"Department '{department.DepartmentName}' is restricted to National level only", 0);

            // 4. LevelDepartment must exist (department available at this level)
            var levelDepartment = allLevelDepartments.FirstOrDefault(ld =>
                ld.LevelId == level.LevelId && ld.DepartmentId == department.DepartmentId);

            if (levelDepartment == null)
                return (false, ImportErrorType.InvalidLevelAssignment,
                    $"Department '{department.DepartmentName}' is not available at '{levelType}' level", 0);

            // 5. Check duplicates
            var isDuplicate = existingAssignments.Any(mld =>
                    mld.MemberId == member.MemberId && mld.LevelDepartmentId == levelDepartment.LevelDepartmentId) ||
                assignmentsToAdd.Any(mld =>
                    mld.MemberId == member.MemberId && mld.LevelDepartmentId == levelDepartment.LevelDepartmentId);

            if (isDuplicate)
                return (false, ImportErrorType.DuplicateAssignment,
                    "Duplicate assignment: Member already assigned to this department at this level", 0);

            return (true, default, null, levelDepartment.LevelDepartmentId);
        }

        private Level? ResolveLevelForMember(string levelType, Member member, List<Level> allLevels)
        {
            return levelType.ToLowerInvariant() switch
            {
                "national" => allLevels.FirstOrDefault(l => l.LevelId == 1),
                "state" => allLevels.FirstOrDefault(l => l.StateId == member.Unit.StateId && l.UnitId == null),
                "unit" => allLevels.FirstOrDefault(l => l.UnitId == member.UnitId),
                _ => null
            };
        }
    }
}
