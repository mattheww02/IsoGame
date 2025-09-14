using Godot;
using HexLayersTest;
using HexLayersTest.Objects;
using System;
using System.Collections.Generic;

public partial class Main : Node2D
{
	[Export] private GameMap _gameMap;
	[Export] private Hud _hud;

	public override void _Ready()
	{
		_hud.EndTurnButtonPressed += OnPlayerEndTurn;
        _hud.NewUnitTypeSelected += OnNewUnitTypeSelected;
        _gameMap.RemainingStockChanged += OnRemainingStockChanged;
		_gameMap.CombatStatusChanged += OnCombatStatusChanged;
		_gameMap.PlayerSetupInfoChanged += OnPlayerSetupInfoChanged;

		_gameMap.Initialise();
	}

	private void OnPlayerEndTurn()
	{
		_gameMap.PlayerEndTurn();
	}

	private void OnRemainingStockChanged(bool isPlayerTeam, int count)
	{
		if (isPlayerTeam) _hud.UpdatePlayerStock(count);
		else _hud.UpdateEnemyStock(count);
	}

	private void OnCombatStatusChanged(Enums.CombatGameStatus status)
	{
		_hud.UpdateCombatStatus(status);
	}

	private void OnPlayerSetupInfoChanged(PlayerSetupInfo playerSetupInfo)
	{
		_hud.UpdatePlayerSetupInfo(playerSetupInfo);
	}

	private void OnNewUnitTypeSelected(Enums.UnitType unitType)
	{
		_gameMap.SetCurrentUnitTypePlayerSetup(unitType);
	}
}
