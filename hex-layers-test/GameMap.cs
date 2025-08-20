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
			var mousePositionOnGrid = GetMousePositionOnGrid();

			if (mousePositionOnGrid != null)
			{
				var gridPosition = mousePositionOnGrid.Value;
				_guiLayer.HighlightTile(new Vector3I(gridPosition.X, gridPosition.Y, _level.GetTile(gridPosition).Height));

				if (_selectedPlayers.Count == 0) //TODO: this is temp to test formation movement
				{
					foreach (var player in _players)
					{
						int dx = player.TargetGridPosition.X - gridPosition.X;
						int dy = player.TargetGridPosition.Y - gridPosition.Y;
                        if (dx >= 0 && dx < 10 && dy >= 0 && dy < 10)
                        {
                            _selectedPlayers.Add(player);
                        }
                    }
					GD.Print($"Players selected: {_selectedPlayers.Count}");
                }
				else
				{
					var minX = _selectedPlayers.Min(p => p.TargetGridPosition.X);
					var minY = _selectedPlayers.Min(p => p.TargetGridPosition.Y);
					var maxX = _selectedPlayers.Max(p => p.TargetGridPosition.X);
					var maxY = _selectedPlayers.Max(p => p.TargetGridPosition.Y);
					var formation = new GridMoveHelper.GridFormation<Player>(
						Formation: new Player[maxX - minX + 1, maxY - minY + 1],
						GridPosition: new Vector2I(minX, minY));
					foreach (var player in _selectedPlayers)
					{
						formation.Formation[player.TargetGridPosition.X - minX, player.TargetGridPosition.Y - minY] = player;
					}
					var moves = new GridMoveHelper(_level).GetMovedFormation(formation, gridPosition);
					foreach (var (player, destination) in moves)
					{
						player.MoveTo(new Vector2I(destination.X + minX, destination.Y + minY));
					}
					_selectedPlayers.Clear();
				}

				//TODO: use dict here instead of looping through all players (will get slow)
				//bool playerJustSelected = false;
    //            foreach (var player in _players)
				//{
    //                if (player.TargetGridPosition == gridPosition)
				//	{
				//		_selectedPlayer = player;
				//		playerJustSelected = true;
				//		break;
				//	}
    //            }
				//if (!playerJustSelected)
				//{
    //                _selectedPlayer?.MoveTo(gridPosition);
    //                _selectedPlayer = null;
    //            }
			}

            GetViewport().SetInputAsHandled();
        }
	}

	private Vector2I? GetMousePositionOnGrid()
	{
        var mousePosition = GetLocalMousePosition();
        GameMapLayer selectedMapLayer = null;

        foreach (var mapLayer in _mapLayers)
        {
            var mousePositionGrid = mapLayer.LocalToMap(mousePosition - mapLayer.Position);
            if (mapLayer.GetCellSourceId(mousePositionGrid) == -1) continue;
            if (mapLayer.LayerHeight > (selectedMapLayer?.LayerHeight ?? -1)) selectedMapLayer = mapLayer;
        }

		if (selectedMapLayer == null) return null;

		return selectedMapLayer.LocalToMap(mousePosition - selectedMapLayer.Position);
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
