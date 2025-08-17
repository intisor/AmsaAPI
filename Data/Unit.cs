using System;
using System.Collections.Generic;

namespace AmsaAPI.Data;

public partial class Unit
{
    public int UnitId { get; set; }

    public string UnitName { get; set; } = null!;

    public int StateId { get; set; }

    public virtual ICollection<Level> Levels { get; set; } = new List<Level>();

    public virtual ICollection<Member> Members { get; set; } = new List<Member>();

    public virtual State State { get; set; } = null!;
}
