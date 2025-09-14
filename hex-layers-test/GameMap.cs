using Godot;
using HexLayersTest;
using HexLayersTest.Objects;
using HexLayersTest.Units;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

public partial class GameMap : Node2D
{
    public event Action<bool, int> RemainingStockChanged;
    public event Action<Enums.CombatGameStatus> CombatStatusChanged;
    public event Action<PlayerSetupInfo> PlayerSetupInfoChanged;

    [Export] private PackedScene _mapLayerPackedScene;
    [Export] private PackedScene _unitPackedScene;
    [Export] private GuiMap _guiMap;
    [Export] private ShadowMap _shadowMap;
    [Export] private UnitFactory _unitFactory;
    [Export] private int _tileSetSourceId;

    private Enums.CombatGameStatus GameStatus
    {
        get => _gameStatus;
        set
        {
            _gameStatus = value;
            CombatStatusChanged?.Invoke(value);
        }
    }

    private readonly List<GameMapLayer> _mapLayers;
    private readonly TileSpriteProvider _tileSpriteProvider;
    private readonly int _seed;

    private LevelArray _level;
    private PathManager _pathManager;
    private EnemyCombatManager _enemyCombatManager;
    private readonly List<Unit> _units;
    private readonly ConcurrentBag<Unit> _killedUnits;
    private readonly List<MultiUnitMoveRequest> _unitDestinations;
    private readonly MultiUnitSelection _selectedUnits;

    private readonly List<Team> _teams;
    private Enums.CombatGameStatus _gameStatus;
    private Timer _combatTimer;

    private PlayerSetupInfo _playerSetupInfo;

    private const int SizeX = 150, SizeY = 100, SizeZ = 10;
	private const int MaxLandHeight = 8, GlobalWaterLevel = 4;
    private const int CombatTimeoutLimitMs = 2000;

	public GameMap()
	{
        _tileSpriteProvider = new();
		_mapLayers = [];
		_units = [];
        _killedUnits = [];
        _teams = [];
        _unitDestinations = [];
		_selectedUnits = [];
        GameStatus = Enums.CombatGameStatus.Loading;
		_seed = (int)(new Random().NextInt64(500));
    }

    public void Initialise()
    {
        ArgumentNullException.ThrowIfNull(_unitPackedScene);
        ArgumentNullException.ThrowIfNull(_mapLayerPackedScene);
        ArgumentNullException.ThrowIfNull(_tileSetSourceId);

        _level = new LevelGenerator(SizeX, SizeY, MaxLandHeight, 0, GlobalWaterLevel).GenerateLevel();
        CreateTileMapLayers();

        _shadowMap.Initialise(SizeZ, _level);

        _pathManager = new(_level);

        CreateTeams();
        _enemyCombatManager = new(_level, _teams);
        _cpuMoveTask = _enemyCombatManager.GenerateMovesAsync();

        _combatTimer = new Timer()
        {
            Autostart = false,
            WaitTime = CombatTimeoutLimitMs / 1000d,
            OneShot = true
        };
        _combatTimer.Timeout += CombatEnded;
        AddChild(_combatTimer);

        _playerSetupInfo = new()
        {
            StockRemaining = 50,
            CurrentUnitType = Enums.UnitType.BasicMeleeFighter,
        };
        _playerSetupInfo.SetUnitSupply(Enums.UnitType.BasicMeleeFighter, 30);
        _playerSetupInfo.SetUnitSupply(Enums.UnitType.BasicRangedFighter, 30);
        _playerSetupInfo.Changed += info => PlayerSetupInfoChanged?.Invoke(info);
        PlayerSetupInfoChanged?.Invoke(_playerSetupInfo);

        GameStatus = Enums.CombatGameStatus.PlayerSetup;
    }

    private void CreateTileMapLayers()
    {
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
    }

    private void CreateTeams()
    {
        _teams.Add(new()
        {
            GuiColour = Colors.Aqua,
            IsPlayerControlled = true,
        });
        _teams.Add(new()
        {
            GuiColour = Colors.Salmon,
        });
        //_teamStartPosition = new(10, 10); //TODO
        //for (int i = 0; i < 25; i++) SpawnUnit(_teams[0], _unitFactory.CreateUnit<BasicRangedFighter>);
        //_teamStartPosition = new(10, 20);
        //for (int i = 0; i < 25; i++) SpawnUnit(_teams[0], _unitFactory.CreateUnit<BasicMeleeFighter>);
        Vector2I enemyTeamStartPositionOrigin = new(50, 50);
        for (int i = 0; i < 50; i++) SpawnUnit(_teams[1], enemyTeamStartPositionOrigin, randomisePosition: true);

        UpdateStockCount(true);
        UpdateStockCount(false);
    }

	private Unit SpawnUnit(Team team, Vector2I position, Enums.UnitType? unitType = null, bool randomisePosition = false)
	{
		var rng = new RandomNumberGenerator();
		Vector2I gridPosition = position;

        if (randomisePosition)
        {
            do
            {
                gridPosition += new Vector2I(rng.RandiRange(-2, 2), rng.RandiRange(-2, 2));
            } while (!_pathManager.CheckTileAvailable(gridPosition));
        }

        Unit newUnit = unitType != null
            ? _unitFactory.CreateUnit(unitType.Value)
            : rng.Randf() > 0.5f
                ? _unitFactory.CreateUnit<BasicMeleeFighter>()
                : _unitFactory.CreateUnit<BasicRangedFighter>();
		newUnit.Initialise(_pathManager, gridPosition, GetPositionAdjusted);
        newUnit.OnKilled += unit => _killedUnits.Add(unit);
        team.AddUnit(newUnit);
        _units.Add(newUnit);
        newUnit.OnMoveOrAction += ResetCombatTimer;
        return newUnit;
    }

    public override void _Process(double delta)
	{
        if (GameStatus != Enums.CombatGameStatus.PlayerTurn) return;

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
        if (GameStatus == Enums.CombatGameStatus.PlayerTurn) UnhandledInputPlayerTurn(@event);
        else if (GameStatus == Enums.CombatGameStatus.PlayerSetup) UnhandledInputPlayerSetup(@event);
    }

    private void UnhandledInputPlayerSetup(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseButton && mouseButton.Pressed)
        {
            if (mouseButton.ButtonIndex == MouseButton.Left 
                && MousePositionOnGrid(out var targetPosition) 
                && _pathManager.CheckTileAvailable(targetPosition))
            {
                var unitType = _playerSetupInfo.CurrentUnitType;
                int unitSupply = _playerSetupInfo.UnitSupply[unitType];
                if (unitSupply > 0 && _playerSetupInfo.StockRemaining > 0) //TODO: check stock against unit type stock
                {
                    var newUnit = SpawnUnit(_teams[0], targetPosition, unitType);
                    _playerSetupInfo.StockRemaining -= newUnit.Stock;
                    _playerSetupInfo.SetUnitSupply(unitType, unitSupply - 1);
                }
            }
        }
    }

    private void UnhandledInputPlayerTurn(InputEvent @event)
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

    Task<Dictionary<Unit, Vector2I>> _cpuMoveTask;

    public void CombatEnded()
    {
        //TODO: move to CombatEnded method

        foreach (var killedUnit in _killedUnits.Intersect(_units))
        {
            _units.Remove(killedUnit);
            killedUnit.Team.RemoveUnit(killedUnit);
            killedUnit.OnMoveOrAction -= ResetCombatTimer;
            killedUnit.ProcessMode = ProcessModeEnum.Disabled;
        }
        RemainingStockChanged(true, _teams.Where(t => t.IsPlayerControlled).Sum(t => t.Stock));
        RemainingStockChanged(false, _teams.Where(t => !t.IsPlayerControlled).Sum(t => t.Stock));

        foreach (var unit in _units)
        {
            if (unit.GridPosition != unit.ReservedGridPosition)
            {
                GD.Print("Reserved position different to actual grid position");
                unit.MoveTo(unit.ReservedGridPosition);
            }
        }

        //TODO: set status to end of combat if either teams have no units left
        GameStatus = Enums.CombatGameStatus.PlayerTurn;
    }

    public async void PlayerEndTurn()
    {
        try
        {
            if (GameStatus == Enums.CombatGameStatus.PlayerSetup)
            {
                GameStatus = Enums.CombatGameStatus.PlayerTurn;
                return;
            }
            else if (GameStatus != Enums.CombatGameStatus.PlayerTurn)
            {
                GD.Print("Can't move units");
                return;
            }
            GameStatus = Enums.CombatGameStatus.WaitingForOpponent;
            GD.Print("Player turn ended");

            foreach (var unit in _units) unit.StartCombatPhase();
            foreach (var req in _unitDestinations)
            {
                if (req.MoveRequests.Count == 1)
                {
                    (var unit, Vector2I destination) = req.MoveRequests.First();
                    unit.MoveTo(destination);
                }
                else
                {
                    foreach (var unit in req.MoveRequests.Keys)
                    {
                        unit.MoveTo(req);
                    }
                }
            }
            _unitDestinations.Clear();

            _cpuMoveTask ??= _enemyCombatManager.GenerateMovesAsync();
            var cpuMoves = await _cpuMoveTask;
            foreach ((Unit unit, Vector2I position) in cpuMoves)
            {
                unit.MoveTo(position);
            }
            _combatTimer.Start();
            GameStatus = Enums.CombatGameStatus.Resolving;
        }
        catch (Exception ex)
        {
            GD.PrintErr(ex);
        }
    }

    private void UpdateStockCount(bool isPlayerTeam)
    {
        int remainingStock = isPlayerTeam
            ? _teams[0].Units.Sum(u => u.Stock)
            : _teams.Skip(1).SelectMany(t => t.Units).Sum(u => u.Stock);
        RemainingStockChanged?.Invoke(isPlayerTeam, remainingStock);
    }

    private void MultiSelectUnits(Vector2I start, Vector2I end)
    {
        _selectedUnits.Clear();

        int minX = Math.Min(start.X, end.X);
        int maxX = Math.Max(start.X, end.X);
        int minY = Math.Min(start.Y, end.Y);
        int maxY = Math.Max(start.Y, end.Y);

        foreach (var unit in _units.Where(u => u.Team.IsPlayerControlled)) //TODO: do this faster
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

        if (new GridMoveHelper(new(SizeX, SizeY), _pathManager).GetMovedFormation(unitPositions, targetPosition - startPosition))
        {
            _unitDestinations.Add(new() { MoveRequests = unitPositions });
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

    private void ResetCombatTimer()
    {
        if (_gameStatus == Enums.CombatGameStatus.Resolving && _combatTimer.TimeLeft > 0) 
            _combatTimer.Start();
    }

    public void SetCurrentUnitTypePlayerSetup(Enums.UnitType unitType)
    {
        _playerSetupInfo.CurrentUnitType = unitType;
    }
}
