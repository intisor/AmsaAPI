using AmsaAPI.Data;
using CsvHelper;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace AmsaAPI
{
    public class ExcoRecord
    {
        public required string NAME { get; set; }
        public required string UNIT { get; set; }
        public required string DEPARTMENT { get; set; }
    }
    
    public class ExcoImporter
    {
        private readonly AmsaDbContext _dbContext;

        public ExcoImporter(AmsaDbContext context)
        {
            _dbContext = context;
        }

        public async Task<List<string>> ImportExcoRecords()
        {
            return await ImportExcoRecords("excos_list_updated.csv");
        }

        public async Task<List<string>> ImportExcoRecords(string csvFilePath)
        {
            var unmatchedRecords = new List<string>();
            var memberLevelDepartmentsToAdd = new List<MemberLevelDepartment>();

            using var reader = new StreamReader(csvFilePath);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            var records = csv.GetRecords<ExcoRecord>().ToList();

            var allMembers = await _dbContext.Members.ToListAsync();
            var allDepartments = await _dbContext.Departments.ToListAsync();
            var allLevelDepartments = await _dbContext.LevelDepartments
                .Where(ld => ld.LevelId == 1)
                .ToListAsync();
            var existingMemberLevelDepartments = await _dbContext.MemberLevelDepartments.ToListAsync();

            foreach (var record in records)
            {
                var nameParts = record.NAME.Split(' ', StringSplitOptions.RemoveEmptyEntries);
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
                    unmatchedRecords.Add($"{record.NAME} - {record.UNIT} - {record.DEPARTMENT} (Invalid name format)");
                    continue;
                }

                var member = allMembers.FirstOrDefault(m =>
                    string.Equals(m.FirstName.Trim(), firstName.Trim(), StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(m.LastName.Trim(), lastName.Trim(), StringComparison.OrdinalIgnoreCase));

                var department = allDepartments.FirstOrDefault(d =>
                    string.Equals(d.DepartmentName.Trim(), record.DEPARTMENT.Trim(), StringComparison.OrdinalIgnoreCase));

                var levelDepartment = department != null
                    ? allLevelDepartments.FirstOrDefault(ld => ld.DepartmentId == department.DepartmentId)
                    : null;

                if (member != null && levelDepartment != null)
                {
                    var exists = existingMemberLevelDepartments.Any(mld =>
                        mld.MemberId == member.MemberId &&
                        mld.LevelDepartmentId == levelDepartment.LevelDepartmentId);

                    if (!exists && !memberLevelDepartmentsToAdd.Any(mld =>
                        mld.MemberId == member.MemberId &&
                        mld.LevelDepartmentId == levelDepartment.LevelDepartmentId))
                    {
                        memberLevelDepartmentsToAdd.Add(new MemberLevelDepartment
                        {
                            MemberId = member.MemberId,
                            LevelDepartmentId = levelDepartment.LevelDepartmentId
                        });
                    }
                }
                else
                {
                    var reason = "";
                    if (member == null) reason += "Member not found; ";
                    if (department == null) reason += "Department not found; ";
                    if (levelDepartment == null && department != null) reason += "LevelDepartment not found; ";
                    
                    unmatchedRecords.Add($"{record.NAME} - {record.UNIT} - {record.DEPARTMENT} ({reason.TrimEnd(';', ' ')})");
                }
            }

            if (memberLevelDepartmentsToAdd.Count != 0)
            {
                await _dbContext.MemberLevelDepartments.AddRangeAsync(memberLevelDepartmentsToAdd);
                await _dbContext.SaveChangesAsync();
            }

            return unmatchedRecords;
        }
    }
}