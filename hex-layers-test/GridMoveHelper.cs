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

    private const double MaxDistance = double.PositiveInfinity;
    private const float Alpha = 0.9f; // between 0 and 1

    public GridMoveHelper(
        LevelArray level)
    {
        _level = level;
    }

    public bool GetMovedFormation<T>(ref Dictionary<T, Vector2I> unitPositions, Vector2I transform) where T : class
    {
        try
        {
            foreach (var (unit, position) in unitPositions)
            {
                unitPositions[unit] = new Vector2I(position.X + transform.X, position.Y + transform.Y);
            }

            AssignUnits(unitPositions);
            return true;
        }
        catch
        { //TODO: catch only Munkres exceptions here
            return false;
        }
    }
    
    private class MovePlan<T>(T unit, Vector2I gridPosition) where T : class
    {
        public T Unit { get; set; } = unit;
        public Vector2I GridPosition { get; set; } = gridPosition;
    }

    private void AssignUnits<T>(Dictionary<T, Vector2I> units) where T : class
    {
        double[][] cost = new double[units.Count][];
        Dictionary<T, Dictionary<Vector2I, float>> distancesMap = [];
        HashSet<Vector2I> viableTilesSet = [];
        var centroid = new Vector2((float)units.Values.Average(u => u.X), (float)units.Values.Average(u => u.Y));

        foreach (var (unit, position) in units)
        {
            var distances = ComputeDistances(position, units.Count, centroid, Alpha);
            foreach (Vector2I tile in distances.Keys) viableTilesSet.Add(tile);
            distancesMap[unit] = distances;
        }

        var viableTiles = viableTilesSet.ToArray();
        var unitsList = units.Keys.ToList();

        for (int i = 0; i < units.Count; i++)
        {
            cost[i] = new double[viableTiles.Length];

            var distances = distancesMap[unitsList[i]];

            for (int j = 0; j < viableTiles.Length; j++)
            {
                var tile = viableTiles[j];
                if (distances.TryGetValue(tile, out float dist))
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
                units[unitsList[i]] = viableTiles[assignedIndex];
            }
        }
    }

    private Dictionary<Vector2I, float> ComputeDistances(Vector2I start, int numTiles, Vector2 centroid, float alpha)
    {
        var distances = new Dictionary<Vector2I, float>();
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