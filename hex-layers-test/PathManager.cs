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
    private readonly TileReservationSet _globalReservations;
    private readonly Dictionary<Team, TileReservationSet> _teamReservations;

    public PathManager(LevelArray level)
    {
        _level = level;
        _teamReservations = [];
    }

    public bool RegisterStartPosition(Unit unit)
    {
        var position = unit.GridPosition;
        if (PositionIsOutOfBounds(position) || _level.GetTile(position).Navigable || TileIsOccupied(position) ||
            !_teamReservations.TryGetValue(unit.Team, out var tileReservationSet)) return false;

        return tileReservationSet.Reserve(position);
    }

    public bool RegisterMove(Unit unit, Vector2I newPosition)
    {
        if (PositionIsOutOfBounds(newPosition) || _level.GetTile(newPosition).Navigable || TileIsOccupied(newPosition) ||
            !_teamReservations.TryGetValue(unit.Team, out var tileReservationSet)) return false;

        return tileReservationSet.Release(newPosition) && tileReservationSet.Reserve(newPosition); //TODO: consider race conditions
    }

    public bool TileIsOccupied(Vector2I position)
    {
        return _globalReservations.IsReserved(position);
    }

    private bool PositionIsOutOfBounds(Vector2I position)
    {
        return position.X < 0 || position.Y < 0 || position.X >= _level.SizeX || position.Y >= _level.SizeY;
    }
}
