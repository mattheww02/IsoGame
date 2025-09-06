using Godot;
using System;

public partial class Main : Node2D
{
	[Export] private GameMap _gameMap;
	[Export] private Hud _hud;

	public override void _Ready()
	{
		_hud.EndTurnButtonPressed += OnPlayerEndTurn;
		_gameMap.RemainingStockChanged += OnRemainingStockChanged;

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
}
