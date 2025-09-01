using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Godot;

namespace HexLayersTest;

public partial class GuiMap : Node2D
{
    [Export] private GuiMapLayer _evenLayer;
    [Export] private GuiMapLayer _oddLayer;

    public override void _Ready()
    {
        _evenLayer.Position += new Vector2(0, 2);
        _oddLayer.Position += new Vector2(0, -6);
    }

    public void HighlightTiles(IEnumerable<Vector3I> gridPositions)
    {
        List<Vector3I> evenPositions = [], oddPositions = [];

        foreach (var position in gridPositions)
        {
            if (position.Z % 2 == 0) evenPositions.Add(position);
            else oddPositions.Add(position);
        }

        _evenLayer.HighlightTiles(evenPositions);
        _oddLayer.HighlightTiles(oddPositions);
    }
}
