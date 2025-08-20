using CommunityToolkit.HighPerformance;
using Godot;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace HexLayersTest.Objects;

public class LevelArray
{
    private readonly LevelTile[,] _tileArray;
    private readonly int _sizeX, _sizeY;

    public int SizeX => _sizeX;
    public int SizeY => _sizeY;

    public LevelArray(int sizeX, int sizeY)
    {
        _sizeX = sizeX;
        _sizeY = sizeY;
        _tileArray = new LevelTile[sizeX, sizeY];
    }

    public void Initialise()
    {
        for (int x = 0; x < _sizeX; x++)
            for (int y = 0; y < _sizeY; y++)
                _tileArray[x, y] = new LevelTile();
    }

    public LevelTile GetTile(Vector2I position) => GetTile(position.X, position.Y);
    public LevelTile GetTile(int x, int y) => _tileArray[x, y];
    public Span2D<LevelTile> GetSpan(Rect2I rect) => GetSpan(rect.Position.X, rect.Position.Y, rect.Size.X, rect.Size.Y);
    public Span2D<LevelTile> GetSpan(int x, int y, int w, int h) => new(_tileArray, x, y, w, h);

    public void ForEach(Action<LevelTile> action)
    {
        for (int x = 0; x < _sizeX; x++)
            for (int y = 0; y < _sizeY; y++)
                action(_tileArray[x, y]);
    }

    public void ForEach(Action<LevelTile, int, int> action)
    {
        for (int x = 0; x < _sizeX; x++)
            for (int y = 0; y < _sizeY; y++)
                action(_tileArray[x, y], x, y);
    }

    public IEnumerable<(int x, int y)> GetIndices()
    {
        for (int x = 0; x < _sizeX; x++)
            for (int y = 0; y < _sizeY; y++)
                yield return (x, y);
    }
}
