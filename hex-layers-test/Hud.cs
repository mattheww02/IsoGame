using Godot;
using System;

public partial class Hud : CanvasLayer
{
	public event Action EndTurnButtonPressed;

	[Export] private Button _endTurnButton;

	public override void _Ready()
	{
		_endTurnButton.Pressed += () => EndTurnButtonPressed();
	}

}
