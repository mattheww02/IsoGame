using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HexLayersTest.Objects;

public class UnitActionType
{
    public int Range { get; set; }
    public Enums.ActionRadiusType RadiusType { get; set; }
    public Enums.UnitsAffected UnitsAffected { get; set; }
    public int Damage { get; set; }
    public int DurationMs { get; set; }
    public int DelayMs { get; set; }
    public int ActionPoints { get; set; }
    public float MoveSpeedMultiplier { get; set; }
    public int DurationWithCooldownMs => DurationMs + 50;
}

public class MeleeAttackUnitActionType : UnitActionType
{
    public MeleeAttackUnitActionType()
    {
        Range = 1;
        RadiusType = Enums.ActionRadiusType.Rectangular;
        UnitsAffected = Enums.UnitsAffected.Enemies;
        ActionPoints = 1;
    }
}

public class RangedAttackUnitActionType : UnitActionType
{
    public RangedAttackUnitActionType()
    {
        RadiusType = Enums.ActionRadiusType.Circular;
        UnitsAffected = Enums.UnitsAffected.Enemies;
        ActionPoints = 1;
    }
}
