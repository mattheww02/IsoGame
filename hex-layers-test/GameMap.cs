using Godot;
using HexLayersTest;
using HexLayersTest.Objects;
using System;
using System.Collections.Generic;
using System.Linq;

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
	private readonly List<Player> _players;
	private List<Player> _selectedPlayers;

    private const int SizeX = 150, SizeY = 100, SizeZ = 10;
	private const int MaxLandHeight = 8, GlobalWaterLevel = 4;

	public GameMap()
	{
        _tileSpriteProvider = new();
		_mapLayers = [];
		_guiLayer = new();
		_players = [];
		_selectedPlayers = [];
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

		for (int i = 0; i < 10; i++) SpawnPlayer();
    }

	private void SpawnPlayer()
	{
		var rng = new RandomNumberGenerator();
		Vector2I gridPosition;

		do
		{
			gridPosition = new Vector2I(rng.RandiRange(1, SizeX - 1), rng.RandiRange(1, SizeY - 1));
		} while (!_level.GetTile(gridPosition).Walkable);
		
		var newPlayer = _playerPackedScene.Instantiate<Player>();
		AddChild(newPlayer);
		newPlayer.Initialise(_level, _mapLayers[0].TileSet.TileSize, gridPosition, GetPositionAdjusted);
		_players.Add(newPlayer);
	}

	public override void _Process(double delta)
	{
	}

    public override void _UnhandledInput(InputEvent @event)
    {
		if (@event.IsActionPressed("MouseSelect"))
		{
			if (MousPositionOnGrid(out var targetPosition))
			{
				ShowTileSelected(targetPosition);

				if (_selectedPlayers.Count == 0) 
				{
					MultiSelectPlayers(targetPosition);
                }
				else
				{
					MoveSelectedPlayers(targetPosition);
                    _selectedPlayers.Clear();
                }
			}

            GetViewport().SetInputAsHandled();
        }
	}
	
	private void MultiSelectPlayers(Vector2I targetPosition)
    { //TODO: this is temp to test formation movement
        foreach (var player in _players)
        {
            int dx = player.TargetGridPosition.X - targetPosition.X;
            int dy = player.TargetGridPosition.Y - targetPosition.Y;
            if (dx >= 0 && dx < 10 && dy >= 0 && dy < 10)
            {
                _selectedPlayers.Add(player);
            }
        }
        GD.Print($"Players selected: {_selectedPlayers.Count}");
    }

	private void ShowTileSelected(Vector2I targetPosition)
	{
        _guiLayer.HighlightTile(new Vector3I(targetPosition.X, targetPosition.Y, _level.GetTile(targetPosition).Height));
    }

	private void MoveSelectedPlayers(Vector2I targetPosition)
	{
        var startPosition = new Vector2I(
			_selectedPlayers.Min(p => p.TargetGridPosition.X),
			_selectedPlayers.Min(p => p.TargetGridPosition.Y));

        var moves = new GridMoveHelper(_level).GetMovedFormation(
            _selectedPlayers.Select(p => (p, p.TargetGridPosition)).ToList(),
            targetPosition - startPosition);

        foreach (var (player, destination) in moves)
        {
            player.MoveTo(destination);
        }
    }

	private bool MousPositionOnGrid(out Vector2I targetPosition)
	{
        var mousePosition = GetLocalMousePosition();
        GameMapLayer selectedMapLayer = null;

        foreach (var mapLayer in _mapLayers)
        {
            targetPosition = mapLayer.LocalToMap(mousePosition - mapLayer.Position);
            if (mapLayer.GetCellSourceId(targetPosition) == -1) continue;
            if (mapLayer.LayerHeight > (selectedMapLayer?.LayerHeight ?? -1)) selectedMapLayer = mapLayer;
        }

		if (selectedMapLayer == null)
		{
			targetPosition = default;
			return false;
		}

		targetPosition = selectedMapLayer.LocalToMap(mousePosition - selectedMapLayer.Position);
		return true;
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
