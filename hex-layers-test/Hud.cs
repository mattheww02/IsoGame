using Godot;
using System;

public partial class Hud : CanvasLayer
{
	public event Action EndTurnButtonPressed;

	[Export] private Button _endTurnButton;
	[Export] private Label _playerStockLabel;
	[Export] private Label _enemyStockLabel;

	public override void _Ready()
	{
		_endTurnButton.Pressed += () => EndTurnButtonPressed();
	}

	public void UpdatePlayerStock(int value) => _playerStockLabel.Text = value.ToString("D3");
    public void UpdateEnemyStock(int value) => _enemyStockLabel.Text = value.ToString("D3");
}
