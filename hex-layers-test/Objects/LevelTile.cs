using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace HexLayersTest.Objects;

public class LevelTile
{
    public int Height { get; set; }
    public Enums.TileType TileType { get; set; }
    public Enums.TileVariant TileVariant { get; set; }
    public int? WaterLevel { get; set; }
}