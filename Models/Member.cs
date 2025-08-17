namespace AmsaAPI.Models
{
    public class Member
    {
        public int MemberId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public int MKANID { get; set; }
        public int UnitId { get; set; }
    }
    public class Department
    {
        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
    }
    public class LevelDepartment
    {
        public int LevelDepartmentId { get; set; }
        public int LevelId { get; set; }
        public int DepartmentId { get; set; }
    }
    public class MemberLevelDepartment
    {
        public int MemberLevelDepartmentId { get; set; }
        public int MemberId { get; set; }
        public int LevelDepartmentId { get; set; }
    }
}
