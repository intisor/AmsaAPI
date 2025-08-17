using System;
using System.Collections.Generic;

namespace AmsaAPI.Data;

public partial class MemberLevelDepartment
{
    public int MemberLevelDepartmentId { get; set; }

    public int MemberId { get; set; }

    public int LevelDepartmentId { get; set; }

    public virtual LevelDepartment LevelDepartment { get; set; } = null!;

    public virtual Member Member { get; set; } = null!;
}
