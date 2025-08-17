using System;
using System.Collections.Generic;

namespace AmsaAPI.Data;

public partial class Department
{
    public int DepartmentId { get; set; }

    public string DepartmentName { get; set; } = null!;

    public virtual ICollection<LevelDepartment> LevelDepartments { get; set; } = new List<LevelDepartment>();
}
