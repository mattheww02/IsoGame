using HexLayersTest.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace HexLayersTest.Units;

public partial class BasicMeleeFighter : Unit
{
    public override Enums.UnitSpriteType UnitSpriteType => Enums.UnitSpriteType.GoldenRetriever;
    public override float MoveSpeed => 30.0f;
    public override int MoveRange => 100;
    public override int MaxActionPoints => 2;
    public override int ActionPointsPerTurn => 2;
    public override int MaxHealth => 10;
    public override int Stock => 1;

    protected override void PopulateActionTypes()
    {
        _actionTypes.Add(new MeleeAttackUnitActionType()
        {
            Damage = 10,
            DurationMs = 500,
            DelayMs = 200,
        });
    }
}
