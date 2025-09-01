using Godot;
using HexLayersTest;
using HexLayersTest.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

public partial class GameMap : Node2D
{
	[Export] private PackedScene _mapLayerPackedScene;
    [Export] private PackedScene _unitPackedScene;
    [Export] private GuiMap _guiMap;
    [Export] private ShadowMap _shadowMap;
    [Export] private int _tileSetSourceId;
	
	private readonly List<GameMapLayer> _mapLayers;
    private readonly TileSpriteProvider _tileSpriteProvider;
	private readonly Vector2I _tileOffset = new(5, 5);
	private readonly int _seed;

	private LevelArray _level;
    private PathManager _pathManager;
	private readonly List<Unit> _units;
    private readonly Dictionary<Unit, Vector2I> _unitDestinations;
    private readonly MultiUnitSelection _selectedUnits;

    private bool _isPlayerTurn;

    private const int SizeX = 150, SizeY = 100, SizeZ = 10;
	private const int MaxLandHeight = 8, GlobalWaterLevel = 4;

	public GameMap()
	{
        _tileSpriteProvider = new();
		_mapLayers = [];
		_units = [];
        _unitDestinations = [];
		_selectedUnits = [];
		_seed = (int)(new Random().NextInt64(500));
    }

    public override void _Ready()
	{
		ArgumentNullException.ThrowIfNull(_unitPackedScene);
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

        _shadowMap.Initialise(SizeZ, _level);

        _pathManager = new(_level);

		for (int i = 0; i < 100; i++) SpawnUnit();

        _isPlayerTurn = true;
    }

	private void SpawnUnit()
	{
		var rng = new RandomNumberGenerator();
		Vector2I gridPosition;

		do
		{
			gridPosition = new Vector2I(rng.RandiRange(1, SizeX - 1), rng.RandiRange(1, SizeY - 1));
		} while (!_level.GetTile(gridPosition).Navigable);
		
		var newUnit = _unitPackedScene.Instantiate<Unit>();
		AddChild(newUnit);
		newUnit.Initialise(_level, _pathManager, gridPosition, GetPositionAdjusted);
        _units.Add(newUnit);
    }

    public override void _Process(double delta)
	{
        if (MousePositionOnGrid(out var targetPosition))
        {
            if (_dragStart != null)
            {
                ShowRectSelected(_dragStart.Value, targetPosition);
            }
            else if (_selectedUnits.Count > 0)
            {
                ShowSelectedUnitProjections(targetPosition);
            }
        }
	}

    private Vector2I? _dragStart = null;
    private Vector2I? _dragEnd = null;
    private const float DragThreshold = 4f;

    public override void _UnhandledInput(InputEvent @event) //TODO: need SelectionManager, GameController, PlayerController(?)
    {
        if (@event is InputEventMouseButton mouseButton) //TODO: some cases where selection thinks mouse stays pressed
        {
            if (mouseButton.ButtonIndex == MouseButton.Left)
            {
                if (mouseButton.Pressed)
                {
                    if (MousePositionOnGrid(out var targetPosition))
                    {
                        _dragStart = targetPosition;
                    }
                }
                else
                {
                    GD.Print(_mapLayers.Count);
                    GD.Print(MousePositionOnGrid(out var p));
                    if (_dragStart != null && MousePositionOnGrid(out var targetPosition))
                    {
                        _dragEnd = targetPosition;

                        float dist = (_dragEnd.Value - _dragStart.Value).Length();
                        if (dist < DragThreshold)
                        {
                            if (_selectedUnits.Count > 0)
                            {
                                MoveSelectedUnits(targetPosition);
                                _selectedUnits.Clear();
                            }
                            else
                            {
                                MultiSelectUnits(targetPosition, targetPosition);
                            }
                        }
                        else
                        {
                            MultiSelectUnits(_dragStart.Value, _dragEnd.Value);
                        }
                    }

                    _dragStart = null;
                    _dragEnd = null;
                }
            }
        }
    }

    public void PlayerEndTurn()
    {
        if (!_isPlayerTurn) return;
        //_isPlayerTurn = false; //TODO: add this back when enemy turns are able to be completed
        GD.Print("Player turn ended");
        foreach ((Unit unit, Vector2I position) in _unitDestinations)
        {
            unit.MoveTo(position);
        }
    }

    private void MultiSelectUnits(Vector2I start, Vector2I end)
    {
        _selectedUnits.Clear();

        int minX = Math.Min(start.X, end.X);
        int maxX = Math.Max(start.X, end.X);
        int minY = Math.Min(start.Y, end.Y);
        int maxY = Math.Max(start.Y, end.Y);

        foreach (var unit in _units)
        {
            var pos = unit.GridPosition;
            if (pos.X >= minX && pos.X <= maxX &&
                pos.Y >= minY && pos.Y <= maxY)
            {
                _selectedUnits.Add(unit);
            }
        }

        _guiMap.HighlightTiles(_selectedUnits.Select(p => new Vector3I(p.GridPosition.X, p.GridPosition.Y, _level.GetTile(p.GridPosition).Height)));

        GD.Print($"Units selected: {_selectedUnits.Count}");
    }

    private void ShowTileSelected(Vector2I targetPosition)
	{
        _guiMap.HighlightTiles([new Vector3I(targetPosition.X, targetPosition.Y, _level.GetTile(targetPosition).Height)]);
    }

    private void ShowRectSelected(Vector2I position1, Vector2I position2)
    {
        List<Vector3I> tilesToHighlight = [];
        for (int x = Math.Min(position1.X, position2.X); x <= Math.Max(position1.X, position2.X); x++)
            for (int  y = Math.Min(position1.Y, position2.Y); y <= Math.Max(position1.Y, position2.Y); y++)
                tilesToHighlight.Add(new Vector3I(x, y, _level.GetTile(x, y).Height));

        _guiMap.HighlightTiles(tilesToHighlight);
    }

    private void ShowSelectedUnitProjections(Vector2I targetPosition)
    { //TODO: check level bounds
        List<Vector3I> tilesToHighlight = [];
        var formation = _selectedUnits.GetFormation();
        int w = formation.GetLength(0);
        int h = formation.GetLength(1);
        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                if (formation[x, y] != null)
                {
                    var newX = x + targetPosition.X;
                    var newY = y + targetPosition.Y;
                    tilesToHighlight.Add(new Vector3I(newX, newY, _level.GetTile(newX, newY).Height));
                }
            }
        }
        _guiMap.HighlightTiles(tilesToHighlight);
    }

	private void MoveSelectedUnits(Vector2I targetPosition)
	{
        _guiMap.HighlightTiles([]);
        var startPosition = new Vector2I(
			_selectedUnits.Min(p => p.GridPosition.X),
			_selectedUnits.Min(p => p.GridPosition.Y));

        var unitPositions = _selectedUnits.ToDictionary(u => u, u => u.GridPosition);

        if (new GridMoveHelper(_level).GetMovedFormation(ref unitPositions, targetPosition - startPosition))
        {
            foreach (var (unit, destination) in unitPositions)
                _unitDestinations[unit] = destination;
        }
        else
        {
            GD.Print("Unit movement failed");
        }       
    }

	private bool MousePositionOnGrid(out Vector2I targetPosition)
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

    private GameMapLayer GetMapLayer(Unit _, int z)
	{
		return _mapLayers[z];
	}

	private int GetTileHeight(Unit _, Vector2I gridPosition)
	{
		return _level.GetTile(gridPosition.X, gridPosition.Y).Height;
	}
}
