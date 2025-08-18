using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HexLayersTest;

public static class Enums
{
    public enum TileType
    {
        None,
        Grass,
        Water,
        Rock,
        Sand
    }

    public enum TileVariant
    {
        FullBlock,
        HalfBlockLower,
        HalfBlockUpper,
        SlopeEast,
        SlopeWest,
        SlopeSouth,
        SlopeNorth,
        HalfBlockEast,
        HalfBlockWest,
        HalfBlockSouth,
        HalfBlockNorth
    }
}
