using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HexLayersTest.Units;

public partial class BasicRangedFighter : Unit
{
    public override Enums.UnitSpriteType UnitSpriteType => Enums.UnitSpriteType.Cat;
    public override float MoveSpeed => 200.0f;
    public override int MoveRange => 100;
}
