using Godot;
using HexLayersTest;
using System;

public partial class GameMapLayer : TileMapLayerWithHeight
{
	public GameMapLayer()
	{
        _relativeHeight = 0;
	}

    public override void _Ready()
	{
		
    }

	public override void _Process(double delta)
	{
	}
}
