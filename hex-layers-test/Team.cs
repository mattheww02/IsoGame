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
    public bool IsPlayerControlled { get; set; }
    public int Stock { get; private set; }

    private readonly HashSet<Unit> _units;

    public IEnumerable<Unit> Units => _units;

    public Team()
    {
        _units = [];
    }
    
    public bool AddUnit(Unit unit)
    {
        if (_units.Add(unit))
        {
            unit.Team = this;
            unit.Modulate = GuiColour;
            Stock += unit.Stock;
            return true;
        }
        return false;
    }

    public bool RemoveUnit(Unit unit)
    {
        if (_units.Remove(unit))
        {
            unit.Team = null;
            unit.Modulate = Colors.White;
            Stock -= unit.Stock;
            return true;
        }
        return false;
    }
}
