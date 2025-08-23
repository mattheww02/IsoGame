using Godot;
using Godot.NativeInterop;
using HexLayersTest.Objects;
using HexLayersTest.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HexLayersTest;

public class PathManager
{
    private readonly LevelArray _level;
    private readonly ImprovedAStar2DGrid _aStarGrid;
    private readonly TileReservationSet _reservations;

    public PathManager(LevelArray level)
    {
        _level = level;
        _aStarGrid = new();
        _reservations = new();

        _aStarGrid.Initialize(_level.SizeX, _level.SizeY, (x, y) => _level.GetTile(x, y).Navigable, (x, y) => _level.GetTile(x, y).Height);
        _aStarGrid.BuildGraph(1, 1, true, true);
    }

    public Vector2I[] GetPath(Vector2I start, Vector2I end)
    {
        return _aStarGrid.GetPathGrid(start, end);
    }

    public bool RegisterStartPosition(Unit unit)
    {
        var position = unit.GridPosition;
        return ValidateGridPosition(position) && _reservations.Reserve(position);
    }

    public bool RegisterRemoval(Unit unit)
    {
        var position = unit.GridPosition;
        return ValidateGridPosition(position) && _reservations.Release(position);
    }

    public bool RegisterMove(Unit unit, Vector2I newPosition)
    {
        var position = unit.GridPosition;
        return ValidateGridPosition(position) && _reservations.Exchange(position, newPosition);
    }

    private bool PositionIsOutOfBounds(Vector2I position)
    {
        return position.X < 0 || position.Y < 0 || position.X >= _level.SizeX || position.Y >= _level.SizeY;
    }

    private bool ValidateGridPosition(Vector2I position)
    {
        return !PositionIsOutOfBounds(position) && _level.GetTile(position).Navigable;
    }
}
