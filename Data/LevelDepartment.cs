using System;
using System.Collections.Generic;

namespace AmsaAPI.Data;

public partial class LevelDepartment
{
    public int LevelDepartmentId { get; set; }

    public int LevelId { get; set; }

    public int DepartmentId { get; set; }

    public virtual Department Department { get; set; } = null!;

    public virtual Level Level { get; set; } = null!;

    public virtual ICollection<MemberLevelDepartment> MemberLevelDepartments { get; set; } = new List<MemberLevelDepartment>();
}
