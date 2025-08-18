using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HexLayersTest;

public partial class TileMapLayerWithHeight : TileMapLayer
{
    private int _layerHeight;
    public int LayerHeight
    {
        get => _layerHeight;
        set
        {
            _layerHeight = value;
            Position = new Vector2(0, -_layerHeight * TileSet.TileSize.Y);
            YSortOrigin = _layerHeight;
            ZIndex = _layerHeight;
            YSortEnabled = true;
        }
    }
}
