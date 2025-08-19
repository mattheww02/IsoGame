using Godot;
using HexLayersTest;
using HexLayersTest.Objects;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class Player : Node2D
{
	public event Func<Vector2I, Vector2> GetPositionAdjusted;

	public Vector2I TargetGridPosition => _targetGridPosition;

    private Vector2 _targetPosition;
	private Vector2I _targetGridPosition;
    private readonly AStarGrid2D _aStarGrid;
	private readonly Queue<Vector2I> _gridPath;
	private LevelArray _levelArray;
	private AnimatedSprite2D _sprite;

	private const float MoveSpeed = 100.0f;

	public Player()
	{
		_aStarGrid = new();
		_gridPath = [];
	}

    public override void _Ready()
    {
        _sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
    }

	public void Initialise(LevelArray levelArray, Vector2I tileSize, Vector2I gridPosition, Func<Vector2I, Vector2> getPositionAdjusted)
	{
		_levelArray = levelArray;
		GetPositionAdjusted += getPositionAdjusted;
		
		_targetGridPosition = gridPosition;
		_targetPosition = GetPositionAdjusted(_targetGridPosition);
		Position = _targetPosition;

        _aStarGrid.Region = new Rect2I(0, 0, levelArray.SizeX, levelArray.SizeY);
        _aStarGrid.CellSize = tileSize;
        _aStarGrid.CellShape = AStarGrid2D.CellShapeEnum.IsometricDown;
        _aStarGrid.DiagonalMode = AStarGrid2D.DiagonalModeEnum.OnlyIfNoObstacles;
        _aStarGrid.Update();

		_levelArray.ForEach((tile, x, y) =>
		{
			if (!tile.Walkable) _aStarGrid.SetPointSolid(new Vector2I(x, y));
		});
    }

    public override void _Process(double delta)
	{
		if (_targetPosition == Position && _gridPath.Count > 0)
		{
			_targetGridPosition = _gridPath.Dequeue();
            _targetPosition = GetPositionAdjusted(_targetGridPosition);
        }

		var direction = _targetPosition - Position;
		_sprite.Play(PlayerSpriteProvider.GetCharacterSprite(direction));

        Position = Position.MoveToward(_targetPosition, MoveSpeed * (float)delta);
	}

	public void MoveTo(Vector2I gridTo)
	{
		var path = _aStarGrid.GetIdPath(_targetGridPosition, new Vector2I(gridTo.X, gridTo.Y)).Skip(1);

		foreach (var pos in path ?? []) _gridPath.Enqueue(pos);
	}
}
