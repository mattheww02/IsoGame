using Accord.Math;
using Accord.Math.Optimization;
using Godot;
using HexLayersTest.Objects;
using HexLayersTest.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace HexLayersTest;

public class GridMoveHelper
{
    private readonly LevelArray _level;

    private const double MaxDistance = double.MaxValue;
    private const float Alpha = 0.5f;

    public GridMoveHelper(
        LevelArray level)
    {
        _level = level;
    }

    public Dictionary<T, Vector2I> GetMovedFormation<T>(List<(T Unit, Vector2I Position)> unitPositions, Vector2I transform) where T : class
    {
        List<MovePlan<T>> movePlans = [];

        foreach (var (unit, position) in unitPositions)
        {
            movePlans.Add(new(unit, position + transform));
        }

        AssignUnits(movePlans);

        return movePlans.Where(x => x.Unit != null).ToDictionary(x => x.Unit, x => x.GridPosition);
    }
    
    private class MovePlan<T>(T unit, Vector2I gridPosition) where T : class
    {
        public T Unit { get; set; } = unit;
        public Vector2I GridPosition { get; set; } = gridPosition;
    }

    private void AssignUnits<T>(List<MovePlan<T>> units) where T : class
    {
        GD.Print("CCCCCCCC");
        double[][] cost = new double[units.Count][];
        Dictionary<MovePlan<T>, Dictionary<Vector2I, double>> distancesMap = [];
        HashSet<Vector2I> viableTilesSet = [];
        float sumX = 0, sumY = 0;
        foreach (var u in units)
        {
            sumX += u.GridPosition.X;
            sumY += u.GridPosition.Y;
        }
        var centroid = new Vector2(sumX / units.Count, sumY / units.Count);

        foreach (var unit in units)
        {
            var distances = ComputeDistances(unit.GridPosition, units.Count, centroid, Alpha);
            foreach (Vector2I tile in distances.Keys) viableTilesSet.Add(tile);
            distancesMap[unit] = distances;
        }

        var viableTiles = viableTilesSet.ToArray();

        for (int i = 0; i < units.Count; i++)
        {
            cost[i] = new double[viableTiles.Length];

            var distances = distancesMap[units[i]];

            for (int j = 0; j < viableTiles.Length; j++)
            {
                var tile = viableTiles[j];
                if (distances.TryGetValue(tile, out double dist))
                    cost[i][j] = dist;
                else
                    cost[i][j] = MaxDistance;
            }
        }
        var munkres = new Munkres(cost);
        munkres.Minimize();

        int[] assignment = munkres.Solution.Select(d => (int)d).ToArray();

        for (int i = 0; i < units.Count; i++)
        {
            int assignedIndex = assignment[i];
            if (assignedIndex != -1)
            {
                units[i].GridPosition = viableTiles[assignedIndex];
            }
        }
    }

    private Dictionary<Vector2I, double> ComputeDistances(Vector2I start, int numTiles, Vector2 centroid, float alpha)
    {
        var distances = new Dictionary<Vector2I, double>();
        var visited = new HashSet<Vector2I>() { start };
        var queue = new Queue<(Vector2I Pos, int Dist)>();
        queue.Enqueue((start, 0));

        while (queue.Count > 0)
        {
            var (pos, dist) = queue.Dequeue();
            float distToCentroid = Math.Abs(centroid.X - pos.X) + Math.Abs(centroid.Y - pos.Y);
            if (TileIsViable(pos)) distances[pos] = dist + alpha * distToCentroid;
            foreach (var newPos in GetUnvisitedNeighbours(pos, visited))
            {
                visited.Add(newPos);
                queue.Enqueue((newPos, dist + 1));
            }
            if (distances.Count >= numTiles) return distances;
        }
        throw new Exception("Not enough walkable tiles found!");//TODO: technically not needed, we could just leave this up to Munkres instead
    }

    private IEnumerable<Vector2I> GetUnvisitedNeighbours(Vector2I position, HashSet<Vector2I> visited)
    {
        List<Vector2I> newPositions = [];
        if (position.X < _level.SizeX - 1) newPositions.Add(new Vector2I(position.X + 1, position.Y));
        if (position.X > 0) newPositions.Add(new Vector2I(position.X - 1, position.Y));
        if (position.Y < _level.SizeY - 1) newPositions.Add(new Vector2I(position.X, position.Y + 1));
        if (position.Y > 0) newPositions.Add(new Vector2I(position.X, position.Y - 1));
        return newPositions.Where(p => !visited.Contains(p));
    }

    private bool TileIsViable(Vector2I gridPosition)
    {
        return _level.GetTile(gridPosition).Navigable; //TODO: check if tile is occupied here too
    }
}