using Godot;
using HexLayersTest;
using HexLayersTest.Objects;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class Unit : Node2D
{
	public event Func<Vector2I, Vector2> GetPositionAdjusted;

	public Vector2I GridPosition => _targetGridPosition;
	public Team Team 
	{
		get => _team;
		set
		{
			if (_team != null) throw new InvalidOperationException($"{nameof(Team)} already has a value");
			_team = value;
		}
	}

	private Team _team;
	private Vector2 _targetPosition;
	private Vector2I _targetGridPosition;
	private readonly Queue<Vector2I> _gridPath;
	private LevelArray _level;
	private PathManager _pathManager;
	private AnimatedSprite2D _sprite;

	private const float MoveSpeed = 100.0f;
	private const int MoveRange = 10;

	public Unit()
	{
		_gridPath = [];
	}

    public override void _Ready()
    {
        _sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
    }

	public void Initialise(LevelArray levelArray, PathManager pathManager, Vector2I gridPosition, Func<Vector2I, Vector2> getPositionAdjusted)
	{
		_level = levelArray;
		_pathManager = pathManager;
		GetPositionAdjusted += getPositionAdjusted;
		
		_targetGridPosition = gridPosition;
		_targetPosition = GetPositionAdjusted(_targetGridPosition);
		Position = _targetPosition;
		_pathManager.RegisterStartPosition(this);
    }

    public override void _Process(double delta)
	{
		if (_targetPosition == Position && _gridPath.Count > 0 && _pathManager.RegisterMove(this, _gridPath.Peek()))
		{
			_targetGridPosition = _gridPath.Dequeue();
            _targetPosition = GetPositionAdjusted(_targetGridPosition);
        }

		var direction = _targetPosition - Position;
		_sprite.Play(UnitSpriteProvider.GetSprite(direction));

        Position = Position.MoveToward(_targetPosition, MoveSpeed * (float)delta);
	}

	public void MoveTo(Vector2I gridTo)
	{
		var path = _pathManager.GetPath(_targetGridPosition, gridTo).Skip(1).Take(MoveRange);

        foreach (var pos in path ?? []) _gridPath.Enqueue(pos);
	}
}
