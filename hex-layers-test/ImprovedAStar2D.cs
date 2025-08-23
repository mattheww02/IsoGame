using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HexLayersTest
{
    using Godot;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Builds an AStar2D graph over a tile grid for units with an NxN footprint,
    /// where edges are only created if the per-step height difference is within a limit.
    /// This preserves "staircase" reachability (many small climbs to high ground).
    /// </summary>
    public partial class ImprovedAStar2DGrid
    {
        // External map accessors (inject these from your level)
        private Func<int, int, bool> _isNavigable;
        private Func<int, int, int> _getHeight;

        private int _width;
        private int _height;

        // Underlying graph for a given unit config
        private AStar2D _astar;

        // Settings for the baked graph
        private int _unitSize = 1;
        private int _maxClimb = 1;
        private bool _allowDiagonals = false;
        private bool _diagonalOnlyIfNoObstacles = true;

        // Direction vectors (grid)
        private static readonly Vector2I[] CardinalDirs = new[]
        {
        new Vector2I( 1, 0),
        new Vector2I(-1, 0),
        new Vector2I( 0, 1),
        new Vector2I( 0,-1),
    };

        private static readonly Vector2I[] DiagonalDirs = new[]
        {
        new Vector2I( 1, 1),
        new Vector2I( 1,-1),
        new Vector2I(-1, 1),
        new Vector2I(-1,-1),
    };

        // ---------- Public API ----------

        /// <summary>
        /// Initialize grid extents and callbacks. Call once per map.
        /// </summary>
        public void Initialize(
            int width,
            int height,
            Func<int, int, bool> isNavigable,
            Func<int, int, int> getHeight)
        {
            _width = width;
            _height = height;
            _isNavigable = isNavigable ?? throw new ArgumentNullException(nameof(isNavigable));
            _getHeight = getHeight ?? throw new ArgumentNullException(nameof(getHeight));
        }

        /// <summary>
        /// (Re)build the AStar2D graph for a given unit size and climb limit.
        /// You can keep multiple instances of this class (or multiple _astar graphs)
        /// if you need several unit sizes active at once.
        /// </summary>
        public void BuildGraph(
            int unitSize,
            int maxClimb,
            bool allowDiagonals = false,
            bool diagonalOnlyIfNoObstacles = true)
        {
            _unitSize = Math.Max(1, unitSize);
            _maxClimb = Math.Max(0, maxClimb);
            _allowDiagonals = allowDiagonals;
            _diagonalOnlyIfNoObstacles = diagonalOnlyIfNoObstacles;

            _astar = new AStar2D();

            // 1) Add points for every top-left grid cell where the NxN footprint fits on navigable tiles.
            for (int y = 0; y < _height; y++)
                for (int x = 0; x < _width; x++)
                {
                    if (FootprintFits(x, y))
                    {
                        long id = IdOf(x, y);
                        // Store grid coords as the "position" (use your own world transform later)
                        _astar.AddPoint(id, new Vector2(x, y));
                    }
                }

            // 2) Connect edges that satisfy per-step height constraints.
            //    Cardinal neighbors
            for (int y = 0; y < _height; y++)
                for (int x = 0; x < _width; x++)
                {
                    if (!_astar.HasPoint(IdOf(x, y)))
                        continue;

                    foreach (var d in CardinalDirs)
                    {
                        int nx = x + d.X;
                        int ny = y + d.Y;
                        if (!_astar.HasPoint(IdOf(nx, ny)))
                            continue;

                        if (StepAllowed(x, y, nx, ny))
                            ConnectOnce(x, y, nx, ny);
                    }

                    if (_allowDiagonals)
                    {
                        foreach (var d in DiagonalDirs)
                        {
                            int nx = x + d.X;
                            int ny = y + d.Y;
                            if (!_astar.HasPoint(IdOf(nx, ny)))
                                continue;

                            if (DiagonalStepAllowed(x, y, nx, ny))
                                ConnectOnce(x, y, nx, ny);
                        }
                    }
                }
        }

        /// <summary>
        /// Get path in grid coordinates (top-left of the unit's footprint).
        /// Returns empty array if no path.
        /// </summary>
        public Vector2I[] GetPathGrid(Vector2I start, Vector2I goal)
        {
            if (!_astar.HasPoint(IdOf(start.X, start.Y)) ||
                !_astar.HasPoint(IdOf(goal.X, goal.Y)))
                return Array.Empty<Vector2I>();

            var pts = _astar.GetPointPath(IdOf(start.X, start.Y), IdOf(goal.X, goal.Y));
            var result = new Vector2I[pts.Length];
            for (int i = 0; i < pts.Length; i++)
                result[i] = new Vector2I((int)Math.Round(pts[i].X), (int)Math.Round(pts[i].Y));
            return result;
        }

        // ---------- Core checks ----------

        // Unique ID for (x,y)
        private long IdOf(int x, int y) => ((long)y * _width) + x;

        // Does NxN footprint lie within bounds and on navigable tiles?
        // Optional: also ensure footprint isn't wildly uneven (max-min <= _maxClimb).
        private bool FootprintFits(int x, int y)
        {
            if (x < 0 || y < 0 || x + _unitSize > _width || y + _unitSize > _height)
                return false;

            int minH = int.MaxValue;
            int maxH = int.MinValue;

            for (int ox = 0; ox < _unitSize; ox++)
                for (int oy = 0; oy < _unitSize; oy++)
                {
                    int gx = x + ox, gy = y + oy;
                    if (!_isNavigable(gx, gy))
                        return false;

                    int h = _getHeight(gx, gy);
                    if (h < minH) minH = h;
                    if (h > maxH) maxH = h;
                }

            // If a footprint spans tiles with huge internal height deltas,
            // you may want to reject it outright (unit can't "stand" on a cliff edge).
            // Relax or remove this if your game allows it.
            return (maxH - minH) <= _maxClimb;
        }

        // Is a cardinal step from (x,y) -> (nx,ny) allowed under max climb?
        private bool StepAllowed(int x, int y, int nx, int ny)
        {
            int dx = nx - x;
            int dy = ny - y;

            if (dx != 0 && dy != 0)
                throw new InvalidOperationException("Use DiagonalStepAllowed for diagonals.");

            // helper to check bounds
            bool InBounds(int cx, int cy) =>
                cx >= 0 && cx < _width &&
                cy >= 0 && cy < _height;

            if (dx == 1) // moving right
            {
                int colFrom = x + _unitSize - 1;
                int colTo = x + _unitSize;

                // Check that both columns fit vertically
                for (int oy = 0; oy < _unitSize; oy++)
                {
                    int yf = y + oy;
                    if (!InBounds(colFrom, yf) || !InBounds(colTo, yf))
                        return false;

                    int hFrom = _getHeight(colFrom, yf);
                    int hTo = _getHeight(colTo, yf);
                    if (Math.Abs(hTo - hFrom) > _maxClimb) return false;
                }
                return true;
            }

            if (dx == -1) // moving left
            {
                int colFrom = x;
                int colTo = x - 1;

                for (int oy = 0; oy < _unitSize; oy++)
                {
                    int yf = y + oy;
                    if (!InBounds(colFrom, yf) || !InBounds(colTo, yf))
                        return false;

                    int hFrom = _getHeight(colFrom, yf);
                    int hTo = _getHeight(colTo, yf);
                    if (Math.Abs(hTo - hFrom) > _maxClimb) return false;
                }
                return true;
            }

            if (dy == 1) // moving down
            {
                int rowFrom = y + _unitSize - 1;
                int rowTo = y + _unitSize;

                for (int ox = 0; ox < _unitSize; ox++)
                {
                    int xf = x + ox;
                    if (!InBounds(xf, rowFrom) || !InBounds(xf, rowTo))
                        return false;

                    int hFrom = _getHeight(xf, rowFrom);
                    int hTo = _getHeight(xf, rowTo);
                    if (Math.Abs(hTo - hFrom) > _maxClimb) return false;
                }
                return true;
            }

            if (dy == -1) // moving up
            {
                int rowFrom = y;
                int rowTo = y - 1;

                for (int ox = 0; ox < _unitSize; ox++)
                {
                    int xf = x + ox;
                    if (!InBounds(xf, rowFrom) || !InBounds(xf, rowTo))
                        return false;

                    int hFrom = _getHeight(xf, rowFrom);
                    int hTo = _getHeight(xf, rowTo);
                    if (Math.Abs(hTo - hFrom) > _maxClimb) return false;
                }
                return true;
            }

            return false; // same cell or invalid delta
        }

        // Is a diagonal step allowed? (obeys optional "no corner cutting")
        private bool DiagonalStepAllowed(int x, int y, int nx, int ny)
        {
            int dx = nx - x;
            int dy = ny - y;
            if (Math.Abs(dx) != 1 || Math.Abs(dy) != 1)
                return false;

            // If "OnlyIfNoObstacles", require both orthogonal sub-steps to be legal too.
            if (_diagonalOnlyIfNoObstacles)
            {
                if (!_astar.HasPoint(IdOf(x + dx, y)) || !_astar.HasPoint(IdOf(x, y + dy)))
                    return false;

                if (!StepAllowed(x, y, x + dx, y)) return false;
                if (!StepAllowed(x, y, x, y + dy)) return false;
            }

            // Also check height along the diagonal leading corner, using the paired offsets.
            // Require both orthogonal comparisons to satisfy climb limit.
            // Comparing via two sub-steps covers this already if _diagonalOnlyIfNoObstacles is true.
            // If diagonals are allowed even when corners are blocked, do direct pairwise checks:
            if (!_diagonalOnlyIfNoObstacles)
            {
                // Compare overlapping edge columns/rows as if doing both steps.
                // First horizontal edge
                if (!StepAllowed(x, y, x + dx, y)) return false;
                // Then vertical edge from the intermediate pos
                if (!StepAllowed(x + dx, y, x + dx, y + dy)) return false;
            }

            return true;
        }

        private void ConnectOnce(int x, int y, int nx, int ny)
        {
            long a = IdOf(x, y);
            long b = IdOf(nx, ny);
            if (!_astar.ArePointsConnected(a, b))
                _astar.ConnectPoints(a, b, true);
        }
    }

}
