using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HexLayersTest;

public partial class TileMapLayerWithHeight : TileMapLayer
{
    protected const int MaxRelativeHeight = 1;
    protected int _relativeHeight;
    protected int _layerHeight;
    public virtual int LayerHeight
    {
        get => _layerHeight;
        set
        {
            _layerHeight = value;
            Position = new Vector2(0, -_layerHeight * 8);// TileSet.TileSize.Y);
            //YSortOrigin = _relativeHeight + _layerHeight * (MaxRelativeHeight + 1);
            ZIndex = _relativeHeight + _layerHeight * (MaxRelativeHeight + 1);
            YSortEnabled = true;
        }
    }
}
