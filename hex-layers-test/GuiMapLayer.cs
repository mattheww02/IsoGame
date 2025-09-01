using Godot;
using HexLayersTest;
using System;
using System.Collections.Generic;

public partial class GuiMapLayer : TileMapLayerWithHeight
{
	private readonly List<Vector2I> _highlightedTiles = [];
	private readonly Vector2I _tileHighlightAtlasCoords = new(0, 3);
	[Export] private int _tileSetSourceId;

    public GuiMapLayer()
	{

    }

	public override void _Ready()
	{
        LayerHeight = 0;
        YSortOrigin = 100;
        ZIndex = 100;
        _relativeHeight = 1;
    }

	public override void _Process(double delta)
	{
	}

	public void HighlightTiles(IEnumerable<Vector3I> gridPositions)
		//TODO: now that tiles are no longer cubes, this will have to adjust position based on tile height
		//TODO: we can achieve this by using 2 GuiMapLayers (probably inside some GuiMap node), one for odd tile heights and one for even
		//TODO: it might also help to consider adjusting the sprite/tileset used here (maybe a flat square sprite rather than a box)
	{
        foreach (var tile in _highlightedTiles) SetCell(tile);
        _highlightedTiles.Clear();

		foreach (var coords in gridPositions)
		{
            var coords2D = new Vector2I(coords.X - coords.Z / 2, coords.Y - coords.Z / 2);

            _highlightedTiles.Add(coords2D);
            SetCell(coords2D, _tileSetSourceId, _tileHighlightAtlasCoords);
        }
    }
}
