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

    public enum UnitSpriteType
    {
        GoldenRetriever,
        Cat,
    }

    public enum TileWatchEventType
    {
        Entered,
        Left,
        Added,
        Removed,
        StartCombat,
        NewlyWatched,
        GameTick,
    }

    public enum RangeMeasure
    {
        Euclidean,
        Chebyshev,
    }

    public enum UnitsAffected
    {
        Enemies,
        Allies,
        All,
    }

    public enum CombatGameStatus
    {
        Loading,
        PlayerSetup,
        PlayerTurn,
        WaitingForOpponent,
        Resolving,
        CombatFinished,
    }

    public enum UnitType
    {
        BasicMeleeFighter,
        BasicRangedFighter,
    }
}
