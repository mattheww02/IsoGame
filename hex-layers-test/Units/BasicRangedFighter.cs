using HexLayersTest.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HexLayersTest.Units;

public partial class BasicRangedFighter : Unit
{
    public override Enums.UnitType UnitType => Enums.UnitType.BasicRangedFighter;
    public override Enums.UnitSpriteType UnitSpriteType => Enums.UnitSpriteType.Cat;
    public override float MoveSpeed => 50.0f;
    public override int MoveRange => 100;
    public override int MaxActionPoints => 2;
    public override int ActionPointsPerTurn => 2;
    public override int MaxHealth => 5;
    public override int Stock => 1;

    protected override void PopulateActionTypes()
    {
        _actionTypes.AddRange(
        [
            new RangedAttackUnitActionType()
            {
                Range = 10,
                Damage = 5,
                DurationMs = 750,
                DelayMs = 600,
            },
        ]);
    }
}
