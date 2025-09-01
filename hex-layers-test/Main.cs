using Godot;
using System;

public partial class Main : Node2D
{
	[Export] private GameMap _gameMap;
	[Export] private Hud _hud;

	public override void _Ready()
	{
		_hud.EndTurnButtonPressed += OnPlayerEndTurn;
	}

	private void OnPlayerEndTurn()
	{
		_gameMap.PlayerEndTurn();
	}
}
