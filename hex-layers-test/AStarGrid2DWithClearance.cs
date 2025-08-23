using Godot;
using HexLayersTest.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace HexLayersTest;

public class AStar2DWithClearance
{
    private readonly LevelArray _level;
    private readonly AStar2D[] _aStar2Ds;
    //public AStar2DWithClearance(LevelArray level, int maxUnitSize)
    //{
    //    _level = level;
    //    _aStar2Ds = new AStar2D[maxUnitSize - 1];

    //    for (int unitSize = 1; unitSize <= maxUnitSize; unitSize++)
    //        _aStar2Ds[unitSize - 1] = AStarFactory(unitSize);
    //}

    //private AStar2D AStarFactory(int unitSize)
    //{
    //    var aStar = new AStar2D();
    //    _aStarGrid.DiagonalMode = AStarGrid2D.DiagonalModeEnum.OnlyIfNoObstacles;
    //    _aStarGrid.Update();

    //    _level.ForEach((tile, x, y) =>
    //    {
    //        if (!tile.Navigable) _aStarGrid.SetPointSolid(new Vector2I(x, y));
    //    });
    //}
}
