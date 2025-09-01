using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace HexLayersTest;

public partial class ShadowMapLayer : TileMapLayerWithHeight
{
    private readonly Vector2 ShadowOffset;
    private readonly Vector2I _shadowSpriteAtlasCoords = new(1, 1);
   
    public override int LayerHeight
    {
        get => _layerHeight;
        set
        {
            base.LayerHeight = value;
            Position += ShadowOffset;
        }
    }

    public ShadowMapLayer()
    {
        _relativeHeight = 1;
        ShadowOffset = TileSet.TileSize * new Vector2(-0.1f, -0.1f);
    }

    public void AddShadow(Vector2I position)
    {
        SetCell(position, 0, _shadowSpriteAtlasCoords);
    }

    public void RemoveShadow(Vector2I position)
    {
        SetCell(position);
    }
}
