using System;
using System.Collections.Generic;

namespace AmsaAPI.Data;

public partial class Member
{
    public int MemberId { get; set; }

    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public int Mkanid { get; set; }

    public int UnitId { get; set; }

    public virtual ICollection<MemberLevelDepartment> MemberLevelDepartments { get; set; } = new List<MemberLevelDepartment>();

    public virtual Unit Unit { get; set; } = null!;
}

