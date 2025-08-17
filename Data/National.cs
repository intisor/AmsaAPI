using System;
using System.Collections.Generic;

namespace AmsaAPI.Data;

public partial class National
{
    public int NationalId { get; set; }

    public string NationalName { get; set; } = null!;

    public virtual ICollection<Level> Levels { get; set; } = new List<Level>();

    public virtual ICollection<State> States { get; set; } = new List<State>();
}
