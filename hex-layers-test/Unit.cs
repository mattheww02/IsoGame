using Godot;
using HexLayersTest;
using HexLayersTest.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

public abstract partial class Unit : Node2D
{
	public event Func<Vector2I, Vector2> GetPositionAdjusted;
	public event Action<Unit> OnKilled;

    public abstract Enums.UnitSpriteType UnitSpriteType { get; }
    public AnimatedSprite2D Sprite { get; set; }
    public abstract float MoveSpeed { get; }
    public abstract int MoveRange { get; }
	public abstract int MaxActionPoints { get; }
	public abstract int ActionPointsPerTurn { get; }
	public abstract int MaxHealth { get; }
	public abstract int Stock { get; }

	public int CurrentHealth { get; private set; }
	public bool IsInCombat { get; set; }
	public bool IsMoving { get; set; }
	public bool IsActing => _currentActionType != null;
    public Vector2I GridPosition => _targetGridPosition;
	public Team Team { get; set; }

	protected readonly List<UnitActionType> _actionTypes = [];
	private UnitActionType _currentActionType = null;
	private int _timeSinceActionMs;
	private int _actionPointsRemaining;
	private readonly HashSet<Vector2I> _tileWatches = [];

	private Vector2 _targetPosition;
	private Vector2I _targetGridPosition;
	private readonly Queue<Vector2I> _gridPath = [];
	private PathManager _pathManager;

    public override void _Ready()
    {
    }

	public void Initialise(PathManager pathManager, Vector2I gridPosition, Func<Vector2I, Vector2> getPositionAdjusted)
	{
		_pathManager = pathManager;
		GetPositionAdjusted += getPositionAdjusted;
		
		_targetGridPosition = gridPosition;
		_targetPosition = GetPositionAdjusted(_targetGridPosition);
		Position = _targetPosition;
		_pathManager.RegisterStartPosition(this);
		PopulateActionTypes();
		PopulateTileWatches();
    }

	protected abstract void PopulateActionTypes();

	private void PopulateTileWatches()
	{
		int radius = _actionTypes.Max(t => t.Range);

		for (int x = GridPosition.X - radius; x <= GridPosition.X + radius; x++)
		{
			for (int y = GridPosition.Y - radius; y <= GridPosition.Y + radius; y++)
			{
				Vector2I watchPosition = new(x, y);
                if (_pathManager.WatchTile(watchPosition, ProcessWatchedTileEvent))
                    _tileWatches.Add(watchPosition);
            }
		}
	}

	private void AdjustTileWatches()
	{
		int radius = _actionTypes.Max(t => t.Range);
		HashSet<Vector2I> newWatches = [];

		for (int x = GridPosition.X - radius; x <= GridPosition.X + radius; x++)
		{
			for (int y = GridPosition.Y - radius; y <= GridPosition.Y + radius; y++)
			{
				newWatches.Add(new Vector2I(x, y));
			}
		}

		foreach (Vector2I oldWatch in _tileWatches.Except(newWatches))
		{
			_pathManager.UnwatchTile(oldWatch, ProcessWatchedTileEvent);
			_tileWatches.Remove(oldWatch);
		}

		foreach (Vector2I newWatch in newWatches.Except(_tileWatches))
		{
			if (_pathManager.WatchTile(newWatch, ProcessWatchedTileEvent))
				_tileWatches.Add(newWatch);
		}
	}

    public override void _Process(double delta)
	{
		// act
		if (IsActing)
		{
			Rotation = 0.5f; //TODO: attack animations
			_timeSinceActionMs += (int)(1000 * delta);
			if (_timeSinceActionMs >= _currentActionType.DurationMs)
			{
                Rotation = 0.0f;
                if (_timeSinceActionMs >= _currentActionType.DurationWithCooldownMs)
				{
                    _timeSinceActionMs = 0;
                    _currentActionType = null;

					foreach (Vector2I watchPosition in _tileWatches)
						_pathManager.CheckTileOccupied(watchPosition, ProcessWatchedTileEvent);
                }
			}
		}
		else
		{
            Rotation = 0.0f;
        }

		// move
		if (_targetPosition == Position)
		{
			if (_gridPath.Count > 0)
			{
				IsMoving = true;
				if (_pathManager.RegisterMove(this, _gridPath.Peek()))
				{
					_targetGridPosition = _gridPath.Dequeue();
					_targetPosition = GetPositionAdjusted(_targetGridPosition);
					AdjustTileWatches();
					if (!IsActing)
					{
						foreach (Vector2I watchPosition in _tileWatches)
							_pathManager.CheckTileOccupied(watchPosition, ProcessWatchedTileEvent);
					}
				}
			}
			else
			{
				IsMoving = false;
			}
		}

		// render
		var direction = (_targetPosition - Position) * (_currentActionType?.MoveSpeedMultiplier ?? 1f);
		Sprite.Play(UnitSpriteProvider.GetSpriteByDirection(direction));

        Position = Position.MoveToward(_targetPosition, MoveSpeed * (float)delta * (_currentActionType?.MoveSpeedMultiplier ?? 1f));
	}

	public void MoveTo(Vector2I gridTo)
	{
		var path = _pathManager.GetPath(_targetGridPosition, gridTo).Skip(1).Take(MoveRange);

        foreach (var pos in path ?? []) _gridPath.Enqueue(pos);
	}

	public void StartCombatPhase()
	{
		IsInCombat = true;
		_pathManager.RegisterStartOfCombat(this); //TODO: not working?
		_actionPointsRemaining = Math.Min(MaxActionPoints, _actionPointsRemaining + ActionPointsPerTurn);
		_currentActionType = null;
    }

	public void ApplyAction(UnitActionType actionType)
	{
		CurrentHealth = Mathf.Clamp(CurrentHealth - actionType.Damage, 0, MaxHealth);
		if (CurrentHealth == 0) Kill();
	}

	private void Kill()
	{
        IsInCombat = false;
		//Team.RemoveUnit(this); //TODO: avoid collection modified issues here
		OnKilled(this);

        _actionPointsRemaining = 0;

        foreach (var watch in _tileWatches) _pathManager.UnwatchTile(watch, ProcessWatchedTileEvent);
		_tileWatches.Clear();
		_pathManager.RegisterRemoval(this);

		Visible = false;
    }

	private void ProcessWatchedTileEvent(TileWatchEventInfo eventInfo)
	{
		if (IsActing) return;

		foreach (var actionType in _actionTypes)
		{
			if (ActionCanBeApplied(eventInfo, actionType))
			{
				_currentActionType = actionType;
				_actionPointsRemaining -= actionType.ActionPoints;
				ScheduleAction(actionType, eventInfo.Unit);
				return;
			}
		}
	}

	private void ScheduleAction(UnitActionType actionType, Unit target)
	{
		if (actionType.DelayMs == 0)
		{
			target.ApplyAction(actionType);
			return;
		}

        var timer = new Timer()
        {
            OneShot = true,
            WaitTime = actionType.DelayMs / 1000.0
        };
        AddChild(timer);
        timer.Timeout += () => { target.ApplyAction(actionType); timer.QueueFree(); };
        timer.Start();
    }

	private bool ActionCanBeApplied(TileWatchEventInfo eventInfo, UnitActionType actionType)
	{
        var watchedPosition = eventInfo.Unit.GridPosition;

		if (eventInfo.Type == Enums.TileWatchEventType.Left || 
			eventInfo.Type == Enums.TileWatchEventType.Removed) return false;

        switch (actionType.UnitsAffected)
        {
            case Enums.UnitsAffected.All:
                break;
            case Enums.UnitsAffected.Allies:
                if (eventInfo.Unit.Team != Team) return false;
                break;
            case Enums.UnitsAffected.Enemies:
                if (eventInfo.Unit.Team == Team) return false;
                break;
            default:
                return false;
        }

		switch (actionType.RadiusType)
		{
			case Enums.ActionRadiusType.Rectangular:
                if (Math.Abs(watchedPosition.X - GridPosition.X) > actionType.Range
					|| Math.Abs(watchedPosition.Y - GridPosition.Y) > actionType.Range) return false;
				break;
			case Enums.ActionRadiusType.Circular:
                if (GridPosition.DistanceTo(watchedPosition) > actionType.Range) return false;
				break;
			default:
				return false;
        }

		if (_actionPointsRemaining < actionType.ActionPoints) return false;

		return true;
	}
}
