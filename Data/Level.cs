using System;
using System.Collections.Generic;

namespace AmsaAPI.Data;

public partial class Level
{
    public int LevelId { get; set; }

    public string LevelType { get; set; } = null!;

    public int? NationalId { get; set; }

    public int? StateId { get; set; }

    public int? UnitId { get; set; }

    public virtual ICollection<LevelDepartment> LevelDepartments { get; set; } = new List<LevelDepartment>();

    public virtual National? National { get; set; }

    public virtual State? State { get; set; }

    public virtual Unit? Unit { get; set; }
}
