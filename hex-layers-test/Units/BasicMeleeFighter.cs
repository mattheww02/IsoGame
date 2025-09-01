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
    public override float MoveSpeed => 100.0f;
    public override int MoveRange => 100;
}
