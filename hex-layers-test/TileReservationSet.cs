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
    private readonly BlockingHashSet<Vector2I> _reservations;
    public TileReservationSet()
    {
        _reservations = [];
    }
    public bool Reserve(Vector2I tile) => _reservations.Add(tile);
    public bool IsReserved(Vector2I tile) => _reservations.Contains(tile);
    public bool Release(Vector2I tile) => _reservations.Remove(tile);
    public bool Exchange(Vector2I tileToRemove, Vector2I tileToAdd) => _reservations.RemoveAndAdd(tileToRemove, tileToAdd);
}
