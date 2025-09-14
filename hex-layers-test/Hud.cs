using Godot;
using HexLayersTest;
using HexLayersTest.Objects;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class Hud : CanvasLayer
{
	public event Action EndTurnButtonPressed;
	public event Action<Enums.UnitType> NewUnitTypeSelected;

	[Export] private Button _endTurnButton;
	[Export] private Label _playerStockLabel;
	[Export] private Label _enemyStockLabel;
	[Export] private ItemList _unitStockIndicatorList;
	[Export] private UnitSpriteProvider _unitSpriteProvider; //TODO: have this injected into here and gamemap

	private readonly Dictionary<Enums.UnitType, int> _unitTypeIndexes = [];
	private readonly List<Enums.UnitType> _unitTypes = [];

	public override void _Ready()
	{
		_endTurnButton.Pressed += () => EndTurnButtonPressed?.Invoke();
		_unitStockIndicatorList.ItemSelected += idx => NewUnitTypeSelected?.Invoke(_unitTypes[(int)idx]);
	}

	public void UpdatePlayerStock(int value) => _playerStockLabel.Text = value.ToString("D3");
    public void UpdateEnemyStock(int value) => _enemyStockLabel.Text = value.ToString("D3");

	public void UpdateCombatStatus(Enums.CombatGameStatus status)
	{
		if (status == Enums.CombatGameStatus.PlayerSetup) _unitStockIndicatorList.Visible = true;
		else _unitStockIndicatorList.Visible = false;

		switch (status)
		{
			case Enums.CombatGameStatus.PlayerTurn:
                _endTurnButton.Text = "End Turn";
                _endTurnButton.Modulate = Colors.White;
                break;
			case Enums.CombatGameStatus.PlayerSetup:
				_endTurnButton.Text = "Ready";
				_endTurnButton.Modulate = Colors.White;
				break;
			default:
                _endTurnButton.Modulate = Colors.Red;
				break;
		}
	}

	public void UpdatePlayerSetupInfo(PlayerSetupInfo newInfo)
	{
		var newUnitTypes = newInfo.UnitSupply.Keys.Except(_unitTypes);
        foreach (var unitType in newUnitTypes)
		{
			Enums.UnitSpriteType spriteType = unitType switch
			{
				Enums.UnitType.BasicMeleeFighter => Enums.UnitSpriteType.GoldenRetriever,
				Enums.UnitType.BasicRangedFighter => Enums.UnitSpriteType.Cat,
				_ => throw new NotImplementedException(),
			};
			AnimatedSprite2D unitSprite = _unitSpriteProvider.GetAnimatedSprite(spriteType);
            Texture2D iconTexture = unitSprite.SpriteFrames.GetFrameTexture(unitSprite.Animation, 0);

            int idx = _unitStockIndicatorList.AddItem($"{newInfo.UnitSupply[unitType]}", iconTexture);
			_unitTypeIndexes[unitType] = idx;
			_unitTypes.Add(unitType);
		}

		foreach ((var unitType, int supplyCount) in newInfo.UnitSupply)
		{
			int idx = _unitTypeIndexes[unitType];
            _unitStockIndicatorList.SetItemText(idx, $"{supplyCount}");
			_unitStockIndicatorList.SetItemDisabled(idx, supplyCount == 0);
        }

		_unitStockIndicatorList.Select(_unitTypeIndexes[newInfo.CurrentUnitType]);
		//TODO: add a total remaining stock label
	}
}
