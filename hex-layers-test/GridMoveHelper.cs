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

    public Dictionary<T, Vector2I> GetMovedFormation<T>(GridFormation<T> startFormation, Vector2I targetGridPosition) where T : class
    {
        List<MovePlan<T>> movePlans = [];

        for (int x = 0; x < startFormation.Formation.GetLength(0); x++)
        {
            for (int y = 0; y < startFormation.Formation.GetLength(1); y++)
            {
                T unit = startFormation.Formation[x, y];
                int newX = x + targetGridPosition.X - startFormation.GridPosition.X;
                int newY = y + targetGridPosition.Y - startFormation.GridPosition.Y;

                movePlans.Add(new MovePlan<T>(unit, new Vector2I(newX, newY)));
            }
        }

        AssignUnits(movePlans);

        return movePlans.Where(x => x.Unit != null).ToDictionary(x => x.Unit, x => x.GridPosition);
        //foreach (var movePlan in movePlans)
        //{
        //    if (_level.GetTile(movePlan.GridPosition).Walkable) continue;

        //    var currentMovePlan = movePlan;
        //    while (currentMovePlan != null)
        //    {
        //        var movePlanPositions = movePlans.ToDictionary(plan => plan.GridPosition, plan => plan);
        //        currentMovePlan = MoveToNearestAvailablePosition(movePlan, movePlanPositions);
        //    }
        //}
    }

    private MovePlan<T> MoveToNearestAvailablePosition<T>(MovePlan<T> currentMovePlan, Dictionary<Vector2I, MovePlan<T>> movePlanPositions) where T : class
    {
        HashSet<Vector2I> visited = [currentMovePlan.GridPosition];
        Queue<(Vector2I Position, int TotalMoves)> searchQueue = [];

        searchQueue.EnqueueRange(GetNeighbouringWalkableTiles(currentMovePlan.GridPosition, visited, currentMovePlan.MovesSoFar));
        while (searchQueue.Count > 0)
        {
            var (newPosition, totalMoves) = searchQueue.Dequeue();
            visited.Add(newPosition);

            if (_level.GetTile(newPosition).Walkable)
            {
                if (movePlanPositions.TryGetValue(newPosition, out var movePlan))
                {
                    if (movePlan.MovesSoFar <= totalMoves)
                    {
                        currentMovePlan.GridPosition = newPosition;
                        currentMovePlan.MovesSoFar = totalMoves;
                        return movePlan;
                    }
                }
                else
                {
                    currentMovePlan.GridPosition = newPosition;
                    currentMovePlan.MovesSoFar = totalMoves;
                    return null;
                }
            }
            searchQueue.EnqueueRange(GetNeighbouringWalkableTiles(newPosition, visited, totalMoves));
        }
        throw new Exception("Couldn't find a valid space for unit");
    }

    private IEnumerable<(Vector2I Position, int TotalMoves)> GetNeighbouringWalkableTiles(Vector2I position, HashSet<Vector2I> visited, int movesSoFar)
    {
        List<Vector2I> newPositions = [];
        if (position.X < _level.SizeX) newPositions.Add(new Vector2I(position.X + 1, position.Y));
        if (position.X > 0) newPositions.Add(new Vector2I(position.X - 1, position.Y));
        if (position.Y < _level.SizeY) newPositions.Add(new Vector2I(position.X, position.Y + 1));
        if (position.Y > 0) newPositions.Add(new Vector2I(position.X, position.Y - 1));
        return newPositions.Where(p => !visited.Contains(p))
            .Select(p => (p, movesSoFar + 1));
    }
    
    private class MovePlan<T>(T unit, Vector2I gridPosition) where T : class
    {
        public T Unit { get; set; } = unit;
        public Vector2I GridPosition { get; set; } = gridPosition;
        public int MovesSoFar { get; set; } = 0;
    }

    public record GridFormation<T>(T[,] Formation, Vector2I GridPosition) where T : class;


    private Dictionary<int, List<int>> BuildGraph<T>(
        List<MovePlan<T>> units,
        List<Vector2I> walkableTiles,
        Dictionary<int, Dictionary<Vector2I, int>> allDistances,
        int maxDist) where T : class
    {
        var graph = new Dictionary<int, List<int>>();
        for (int i = 0; i < units.Count; i++)
        {
            graph[i] = [];
            foreach (var (tile, j) in walkableTiles.Select((t, idx) => (t, idx)))
            {
                if (allDistances[i].TryGetValue(tile, out int dist) && dist <= maxDist)
                    graph[i].Add(j);
            }
        }
        return graph;
    }

    private Dictionary<Vector2I, int> ComputeDistances(Vector2I start)
    {
        var distances = new Dictionary<Vector2I, int>();
        var visited = new HashSet<Vector2I>();
        var queue = new Queue<(Vector2I Pos, int Dist)>();
        queue.Enqueue((start, 0));

        while (queue.Count > 0)
        {
            var (pos, dist) = queue.Dequeue();
            distances[pos] = dist;
            foreach (var newPos in GetNeighbours(pos, visited))
            {
                visited.Add(newPos);
                queue.Enqueue((newPos, dist + 1));
            }
        }
        return distances;
    }

    private IEnumerable<Vector2I> GetNeighbours(Vector2I position, HashSet<Vector2I> visited)
    {
        List<Vector2I> newPositions = [];
        if (position.X < _level.SizeX) newPositions.Add(new Vector2I(position.X + 1, position.Y));
        if (position.X > 0) newPositions.Add(new Vector2I(position.X - 1, position.Y));
        if (position.Y < _level.SizeY) newPositions.Add(new Vector2I(position.X, position.Y + 1));
        if (position.Y > 0) newPositions.Add(new Vector2I(position.X, position.Y - 1));
        return newPositions.Where(p => !visited.Contains(p));
    }

    private void AssignUnits<T>(List<MovePlan<T>> units) where T : class
    {
        // Collect all walkable tiles
        var walkableTiles = new List<Vector2I>();
        for (int x = 0; x < _level.SizeX; x++)
            for (int y = 0; y < _level.SizeY; y++)
                if (_level.GetTile(new Vector2I(x, y)).Walkable)
                    walkableTiles.Add(new Vector2I(x, y));

        // Precompute BFS distances for each unit
        var allDistances = new Dictionary<int, Dictionary<Vector2I, int>>();
        for (int i = 0; i < units.Count; i++)
            allDistances[i] = ComputeDistances(units[i].GridPosition);

        int low = 0, high = _level.SizeX + _level.SizeY; // worst-case distance
        while (low < high)
        {
            int mid = (low + high) / 2;
            var graph = BuildGraph(units, walkableTiles, allDistances, mid);
            var hk = new HopcroftKarp(graph, units.Count, walkableTiles.Count);
            if (hk.MaxMatching() == units.Count)
                high = mid; // feasible, try smaller
            else
                low = mid + 1; // infeasible, increase distance
        }

        GD.Print($"Minimum maximum distance = {low}");
        // One more run at 'low' to get the actual assignment
        var finalGraph = BuildGraph(units, walkableTiles, allDistances, low);
        var matcher = new HopcroftKarp(finalGraph, units.Count, walkableTiles.Count);
        matcher.MaxMatching();

        // Now you can map each unit to its assigned tile via matcher.pairU[]
    }
}

public class HopcroftKarp
{
    private readonly Dictionary<int, List<int>> graph;
    private readonly int nLeft, nRight;
    private int[] pairU, pairV, dist;

    public HopcroftKarp(Dictionary<int, List<int>> graph, int nLeft, int nRight)
    {
        this.graph = graph;
        this.nLeft = nLeft;
        this.nRight = nRight;
        pairU = new int[nLeft];
        pairV = new int[nRight];
        dist = new int[nLeft];
        Array.Fill(pairU, -1);
        Array.Fill(pairV, -1);
    }

    public int MaxMatching()
    {
        int matching = 0;
        while (BFS())
        {
            for (int u = 0; u < nLeft; u++)
                if (pairU[u] == -1 && DFS(u))
                    matching++;
        }
        return matching;
    }

    private bool BFS()
    {
        var queue = new Queue<int>();
        for (int u = 0; u < nLeft; u++)
        {
            if (pairU[u] == -1)
            {
                dist[u] = 0;
                queue.Enqueue(u);
            }
            else dist[u] = int.MaxValue;
        }

        bool foundAugPath = false;
        while (queue.Count > 0)
        {
            int u = queue.Dequeue();
            foreach (var v in graph[u])
            {
                int u2 = pairV[v];
                if (u2 != -1 && dist[u2] == int.MaxValue)
                {
                    dist[u2] = dist[u] + 1;
                    queue.Enqueue(u2);
                }
                else if (u2 == -1)
                    foundAugPath = true;
            }
        }
        return foundAugPath;
    }

    private bool DFS(int u)
    {
        foreach (var v in graph[u])
        {
            int u2 = pairV[v];
            if (u2 == -1 || (dist[u2] == dist[u] + 1 && DFS(u2)))
            {
                pairU[u] = v;
                pairV[v] = u;
                return true;
            }
        }
        dist[u] = int.MaxValue;
        return false;
    }
}
