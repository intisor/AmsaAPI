using System;
using System.Collections.Generic;

namespace AmsaAPI.Data;

public partial class State
{
    public int StateId { get; set; }

    public string StateName { get; set; } = null!;

    public int NationalId { get; set; }

    public virtual ICollection<Level> Levels { get; set; } = new List<Level>();

    public virtual National National { get; set; } = null!;

    public virtual ICollection<Unit> Units { get; set; } = new List<Unit>();
}
