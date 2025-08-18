using Godot;
using System;

public partial class GameCamera : Camera2D
{
	private const float Speed = 300.0f;

	public override void _Ready()
	{
	}

	public override void _Process(double delta)
	{
		var newPosition = Position;
		if (Input.IsActionPressed("MoveCameraUp")) newPosition.Y -= Speed * (float)delta;
		if (Input.IsActionPressed("MoveCameraDown")) newPosition.Y += Speed * (float)delta;
		if (Input.IsActionPressed("MoveCameraLeft")) newPosition.X -= Speed * (float)delta;
		if (Input.IsActionPressed("MoveCameraRight")) newPosition.X += Speed * (float)delta;

		Position = newPosition;
	}
}
