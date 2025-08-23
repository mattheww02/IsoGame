using Godot;
using HexLayersTest;
using System;
using System.Collections.Generic;

public partial class GuiMapLayer : TileMapLayerWithHeight
{
	private readonly List<Vector2I> _highlightedTiles;
	[Export] private int _tileSetSourceId;

    public GuiMapLayer()
	{
		_highlightedTiles = [];
    }

	public override void _Ready()
	{
        LayerHeight = 0;
        YSortOrigin = 100;
        ZIndex = 100;
    }

	public override void _Process(double delta)
	{
	}

	//public void HighlightTile(Vector3I coords) //TODO: refactor
	//{
	//	foreach (var tile in _highlightedTiles) SetCell(tile);
	//	_highlightedTiles.Clear();

 //       LayerHeight = coords.Z + 1;
	//	var coords2D = new Vector2I(coords.X, coords.Y);

 //       _highlightedTiles.Add(coords2D);
	//	SetCell(coords2D, _tileSetSourceId, Vector2I.Zero);
 //   }

	public void HighlightTiles(IEnumerable<Vector3I> gridPositions)
	{
        foreach (var tile in _highlightedTiles) SetCell(tile);
        _highlightedTiles.Clear();

		foreach (var coords in gridPositions)
		{
            var coords2D = new Vector2I(coords.X - coords.Z - 1, coords.Y - coords.Z - 1); //TODO: refactor

            _highlightedTiles.Add(coords2D);
            SetCell(coords2D, _tileSetSourceId, Vector2I.Zero);
        }
    }
}
