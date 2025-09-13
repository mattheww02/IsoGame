using Godot;
using HexLayersTest.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HexLayersTest;

public class TileReservationSet
{
    private readonly BlockingDictionary<Vector2I, Unit> _reservations;
    public TileReservationSet()
    {
        _reservations = [];
    }
    public bool Reserve(Vector2I tile, Unit unit) => _reservations.Add(tile, unit);
    public bool IsReserved(Vector2I tile) => _reservations.Contains(tile);
    public bool TryGetReservation(Vector2I tile, out Unit unit) => _reservations.TryGetValue(tile, out unit);
    public bool Release(Vector2I tile) => _reservations.Remove(tile);
    public bool Exchange(Vector2I tileToRemove, Vector2I tileToAdd) => _reservations.RemoveAndAdd(tileToRemove, tileToAdd);
}
