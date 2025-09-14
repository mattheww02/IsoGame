using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HexLayersTest.Objects;

public class MultiUnitMoveRequest
{
    public Dictionary<Unit, Vector2I> MoveRequests { get; set; }
    public Vector2I[] FirstUnitPath { get; set; } = null;
}
