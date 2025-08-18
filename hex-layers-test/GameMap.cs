using Godot;
using HexLayersTest;
using HexLayersTest.Objects;
using System;
using System.Collections.Generic;

public partial class GameMap : Node2D
{
	[Export] private PackedScene _mapLayerPackedScene;
    [Export] private PackedScene _playerPackedScene;
    [Export] private GuiMapLayer _guiLayer;
    [Export] private int _tileSetSourceId;
	
	private readonly List<GameMapLayer> _mapLayers;
    private readonly TileSpriteProvider _tileSpriteProvider;
	private readonly Vector2I _tileOffset = new(5, 5);
	private readonly int _seed;

	private LevelArray _level;
    private Player _player;

    private const int SizeX = 150, SizeY = 100, SizeZ = 7;
	private const int MaxLandHeight = 8, GlobalWaterLevel = 4;

	public GameMap()
	{
        _tileSpriteProvider = new();
		_mapLayers = [];
		_guiLayer = new();
		_seed = (int)(new Random().NextInt64(500));
    }

    public override void _Ready()
	{
		ArgumentNullException.ThrowIfNull(_playerPackedScene);
        ArgumentNullException.ThrowIfNull(_mapLayerPackedScene);
        ArgumentNullException.ThrowIfNull(_tileSetSourceId);

        _level = new LevelGenerator(SizeX, SizeY, MaxLandHeight, globalWaterLevel: GlobalWaterLevel).GenerateLevel();
		for (int i = 0; i < SizeZ; i++)
		{
			var mapLayer = _mapLayerPackedScene.Instantiate<GameMapLayer>();
			mapLayer.LayerHeight = i;
            _mapLayers.Add(mapLayer);
			AddChild(mapLayer);
		}

        for (int x = 0; x < SizeX; x++)
		{
			for (int y = 0; y < SizeY; y++)
			{
				LevelTile tile = _level.GetTile(x, y);
				for (int z = 0; z < SizeZ; z++)
				{
					var atlasCoords = _tileSpriteProvider.GetAtlasCoords(tile, z);
					if (atlasCoords == null) continue;
                    _mapLayers[z].SetCell(new Vector2I(x, y), _tileSetSourceId, atlasCoords);
                }
			}
		}

        _player = _playerPackedScene.Instantiate<Player>();
        AddChild(_player);
        _player.Initialise(_level, _mapLayers[0].TileSet.TileSize, new Vector2I(20, 20), GetPositionAdjusted);
    }

	public override void _Process(double delta)
	{
	}

    public override void _UnhandledInput(InputEvent @event)
    {
		if (@event.IsActionPressed("MouseSelect"))
		{
			var mousePosition = GetLocalMousePosition();
			GameMapLayer selectedMapLayer = null;

			foreach (var mapLayer in _mapLayers)
			{
                var mousePositionGrid = mapLayer.LocalToMap(mousePosition - mapLayer.Position);
				if (mapLayer.GetCellSourceId(mousePositionGrid) == -1) continue;
				if (mapLayer.LayerHeight > (selectedMapLayer?.LayerHeight ?? -1)) selectedMapLayer = mapLayer;
            }

			if (selectedMapLayer != null)
			{
                var mousePositionGrid = selectedMapLayer.LocalToMap(mousePosition - selectedMapLayer.Position);
				_guiLayer.HighlightTile(new Vector3I(mousePositionGrid.X, mousePositionGrid.Y, selectedMapLayer.LayerHeight));
				_player.MoveTo(mousePositionGrid);
			}

            GetViewport().SetInputAsHandled();
        }
	}

    private Vector2 GetPositionAdjusted(Vector2I gridPosition)
    {
		int height = _level.GetTile(gridPosition).Height;
		var layer = _mapLayers[height];
        return layer.MapToLocal(new Vector2I(gridPosition.X, gridPosition.Y)) + layer.Position;
    }

    private GameMapLayer GetMapLayer(Player _, int z)
	{
		return _mapLayers[z];
	}

	private int GetTileHeight(Player _, Vector2I gridPosition)
	{
		return _level.GetTile(gridPosition.X, gridPosition.Y).Height;
	}
}
