using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HexLayersTest;

public class Team
{
    public Color GuiColour { get; set; }

    private HashSet<Unit> _units;
}
