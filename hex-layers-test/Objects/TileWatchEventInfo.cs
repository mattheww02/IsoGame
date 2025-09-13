using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HexLayersTest.Objects;

public record TileWatchEventInfo(
    Unit Unit,
    Vector2I Position,
    Enums.TileWatchEventType Type)
{
}
