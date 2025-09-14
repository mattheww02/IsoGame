using Godot;
using Godot.NativeInterop;
using HexLayersTest.Objects;
using HexLayersTest.Shared;
using System;
using System.Collections.Concurrent;
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
    private readonly ConcurrentDictionary<Vector2I, Action<TileWatchEventInfo>> _tileWatches;

    private const int MaxTilesToOvertake = 10;

    public PathManager(LevelArray level)
    {
        _level = level;
        _aStarGrid = new();
        _reservations = new();
        _tileWatches = new();

        _aStarGrid.Initialize(_level.SizeX, _level.SizeY, (x, y) => _level.GetTile(x, y).Navigable, (x, y) => _level.GetTile(x, y).Height);
        _aStarGrid.BuildGraph(1, 1, true, true);
    }

    public Vector2I[] GetPath(Vector2I start, Vector2I end)
    {
        return _aStarGrid.GetPathGrid(start, end);
    }

    public Vector2I[] GetPath(Unit unit, MultiUnitMoveRequest moveRequest)
    {
        Vector2I start = unit.GridPosition;
        if (!moveRequest.MoveRequests.TryGetValue(unit, out Vector2I end))
            return [];

        if ((moveRequest.FirstUnitPath?.Length ?? 0) == 0)
        {
            moveRequest.FirstUnitPath = GetPath(start, end);
            return moveRequest.FirstUnitPath;
        }

        List<Vector2I> path = new() { start };

        Vector2I offset = start - moveRequest.FirstUnitPath[0];

        foreach (var leaderStep in moveRequest.FirstUnitPath)
        {
            Vector2I desired = leaderStep + offset;

            if (ValidateGridPosition(desired))
            {
                if (path[^1] != desired)
                    path.Add(desired);
            }
            else
            {
                var detour = GetPath(path[^1], leaderStep);

                if (detour.Length > 1)
                {
                    path.AddRange(detour.Skip(1));
                }
                else
                {
                    continue;
                }
            }

            if (path[^1] == end)
                break;
        }

        if (path[^1] != end)
        {
            var tail = GetPath(path[^1], end);
            if (tail.Length > 1)
                path.AddRange(tail.Skip(1));
        }

        return path.ToArray();
    }

    public bool RegisterStartPosition(Unit unit)
    {
        var position = unit.ReservedGridPosition;
        if (!ValidateGridPosition(position) || !_reservations.Reserve(position, unit)) return false;

        TriggerTileWatchEvent(new(unit, position, Enums.TileWatchEventType.Added));
        return true;
    }

    public bool RegisterStartOfCombat(Unit unit)
    {
        var position = unit.ReservedGridPosition;
        if (!ValidateGridPosition(position) || !_reservations.IsReserved(position)) return false;

        TriggerTileWatchEvent(new(unit ,position, Enums.TileWatchEventType.StartCombat));
        return true;
    }

    public bool RegisterRemoval(Unit unit)
    {
        var position = unit.ReservedGridPosition;
        if (!ValidateGridPosition(position)) return false;

        if (_reservations.TryGetReservation(position, out var unitOnTile))

        TriggerTileWatchEvent(new(unit, position, Enums.TileWatchEventType.Removed));
        return true;
    }

    public bool RegisterMove(Unit unit, Queue<Vector2I> gridPath)
    {
        var position = unit.ReservedGridPosition;
        if (!gridPath.TryPeek(out var newPosition) || !ValidateGridPosition(position) || !ValidateGridPosition(newPosition)) return false;

        if (!_reservations.Exchange(position, newPosition))
        {
            //if (_reservations.TryGetReservation(newPosition, out Unit blocker) && blocker.IsMoving) return false;

            var gridPathList = gridPath.ToList();
            for (int i = 1; i <= MaxTilesToOvertake; i++)
            {
                if (i >= gridPath.Count) return false;

                if (_reservations.Exchange(position, gridPathList[i]))
                {
                    unit.TilesToOvertake = i + 1;
                    return true;
                }
            }

            return false;
        }

        TriggerTileWatchEvent(new(unit, position, Enums.TileWatchEventType.Left));
        TriggerTileWatchEvent(new(unit, newPosition, Enums.TileWatchEventType.Entered));
        return true;
    }

    public void RegisterMoveOvertake(Unit unit, Vector2I newPosition)
    {
        var position = unit.GridPosition;
        unit.TilesToOvertake--;
        TriggerTileWatchEvent(new(unit, position, Enums.TileWatchEventType.Left));
        TriggerTileWatchEvent(new(unit, newPosition, Enums.TileWatchEventType.Entered));
    }

    public bool CheckTileOccupied(Vector2I position, Action<TileWatchEventInfo> callback = null)
    {
        if (!ValidateGridPosition(position)) return false;

        if (!_reservations.TryGetReservation(position, out var unit)) return false;

        callback?.Invoke(new(unit, position, Enums.TileWatchEventType.GameTick));
        return true;
    }

    public bool CheckTileAvailable(Vector2I position)
    {
        if (!ValidateGridPosition(position)) return false;

        if (_reservations.TryGetReservation(position, out _)) return false;

        return true;
    }

    public bool WatchTile(Vector2I position, Action<TileWatchEventInfo> callback)
    {
        if (!ValidateGridPosition(position)) return false;

        _tileWatches.AddOrUpdate(position, callback, (_, existing) => existing + callback);
        return true;
    }

    public bool UnwatchTile(Vector2I position, Action<TileWatchEventInfo> callback)
    {
        if (!ValidateGridPosition(position)) return false;

        if (_tileWatches.TryGetValue(position, out var callbacks))
        {
            callbacks -= callback;
            if (callbacks == null)
                _tileWatches.TryRemove(position, out callbacks);
            else
                _tileWatches[position] = callbacks;

            return true;
        }
        return false;
    }

    private void TriggerTileWatchEvent(TileWatchEventInfo eventInfo)
    {
        if (_tileWatches.TryGetValue(eventInfo.Position, out var callbacks))
        {
            foreach (var callback in callbacks.GetInvocationList())
            {
                try
                {
                    ((Action<TileWatchEventInfo>)callback)(eventInfo);
                }
                catch (Exception ex)
                {
                    GD.PrintErr(ex);
                }
            }
        }
    }

    private bool ValidateGridPosition(Vector2I position)
    {
        return !PositionIsOutOfBounds(position) && _level.GetTile(position).Navigable;
    }

    private bool PositionIsOutOfBounds(Vector2I position)
    {
        return position.X < 0 || position.Y < 0 || position.X >= _level.SizeX || position.Y >= _level.SizeY;
    }
}
