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

    private void AssignUnits<T>(List<MovePlan<T>> movePlans) where T : class
    {
        var units = movePlans;
        var walkableTiles = GetViableTiles(movePlans.Select(x => x.GridPosition));

        double[][] cost = new double[units.Count][];

        for (int i = 0; i < units.Count; i++)
        {
            cost[i] = new double[walkableTiles.Count];

            var distances = ComputeDistances(units[i].GridPosition);

            for (int j = 0; j < walkableTiles.Count; j++)
            {
                var tile = walkableTiles[j];
                if (distances.TryGetValue(tile, out int dist))
                    cost[i][j] = dist;
                else
                    cost[i][j] = double.MaxValue;
            }
        }

        var munkres = new Accord.Math.Optimization.Munkres(cost);
        munkres.Minimize();

        int[] assignment = munkres.Solution.Select(d => (int)d).ToArray();

        for (int i = 0; i < units.Count; i++)
        {
            int assignedIndex = assignment[i];
            if (assignedIndex != -1)
            {
                units[i].GridPosition = walkableTiles[assignedIndex];
                GD.Print(walkableTiles[assignedIndex]);
            }
        }
    }

    private Dictionary<Vector2I, int> ComputeDistances(Vector2I start)
    {
        var distances = new Dictionary<Vector2I, int>();
        var visited = new HashSet<Vector2I>() { start };
        var queue = new Queue<(Vector2I Pos, int Dist)>();
        queue.Enqueue((start, 0));

        while (queue.Count > 0)
        {
            var (pos, dist) = queue.Dequeue();
            distances[pos] = dist;
            foreach (var newPos in GetUnvisitedNeighbours(pos, visited))
            {
                visited.Add(newPos);
                queue.Enqueue((newPos, dist + 1));
            }
        }
        return distances;
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

    private List<Vector2I> GetViableTiles(IEnumerable<Vector2I> gridPositions, int marginSize = 2)
    {
        int minX = _level.SizeX - 1, maxX = 0;
        int minY = _level.SizeY - 1, maxY = 0;

        foreach (var p in gridPositions)
        {
            if (p.X < minX) minX = p.X;
            if (p.X > maxX) maxX = p.X;
            if (p.Y < minY) minY = p.Y;
            if (p.Y > maxY) maxY = p.Y;
        }

        minX = Math.Max(0, minX - marginSize);
        maxX = Math.Min(_level.SizeX - 1, maxX + marginSize);
        minY = Math.Max(0, minY - marginSize);
        maxY = Math.Min(_level.SizeX - 1, maxY + marginSize);

        var walkableTiles = new List<Vector2I>();
        for (int x = minX; x <= maxX; x++)
            for (int y = minY; y <= maxY; y++)
                if (_level.GetTile(x, y).Navigable)
                    walkableTiles.Add(new Vector2I(x, y));
        return walkableTiles;
    }
}
