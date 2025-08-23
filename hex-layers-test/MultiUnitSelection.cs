using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HexLayersTest;

public class MultiUnitSelection : IEnumerable<Unit>
{
    private readonly HashSet<Unit> _units;
    private int _minX, _minY, _maxX, _maxY;

    public MultiUnitSelection()
    {
        _units = [];
    }

    public void Clear()
    {
        _units.Clear();
        _minX = 0;
        _minY = 0;
        _maxX = 0;
        _maxY = 0;
    }

    public int Count => _units.Count;

    public bool Add(Unit unit)
    {
        
        if (_units.Count == 0 )
        {
            (_minX, _minY) = unit.GridPosition;
            (_maxX, _maxY) = (_minX, _minY);
        }
        else
        {
            _minX = Math.Min(unit.GridPosition.X, _minX);
            _minY = Math.Min(unit.GridPosition.Y, _minY);
            _maxX = Math.Max(unit.GridPosition.X, _maxX);
            _maxY = Math.Max(unit.GridPosition.Y, _maxY);
        }
            
        return _units.Add(unit);
    }

    public bool Contains(Unit unit) => _units.Contains(unit);

    public Unit[,] GetFormation()
    {
        if (_units.Count == 0) return null;

        var formation = new Unit[_maxX -  _minX + 1, _maxY - _minY + 1];

        foreach (var unit in _units)
            formation[unit.GridPosition.X - _minX, unit.GridPosition.Y - _minY] = unit;

        return formation;
    }

    public IEnumerator<Unit> GetEnumerator() => _units.GetEnumerator();

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}
