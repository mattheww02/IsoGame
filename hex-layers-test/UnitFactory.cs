using Godot;
using HexLayersTest;
using HexLayersTest.Units;
using System;
using System.Collections.Generic;

public partial class UnitFactory : Node
{
    [Export] private UnitSpriteProvider _unitSpriteProvider;

	public T CreateUnit<T>() where T : Unit, new()
	{
        var unit = new T();
        unit.Sprite = _unitSpriteProvider.GetAnimatedSprite(unit);
        AddChild(unit);
        unit.AddChild(unit.Sprite);
        return unit;
	}
}
