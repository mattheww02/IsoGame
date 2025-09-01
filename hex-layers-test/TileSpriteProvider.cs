using Godot;
using HexLayersTest.Objects;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HexLayersTest;

public class TileSpriteProvider
{
    private readonly Vector2I? _defaultAtlasCoords = null;
    private readonly Vector2I? _waterAtlasCoords = new(0, 2);

    private readonly Dictionary<TileTypeVariant, Vector2I[]> _atlasCoordsMap = new()
    {
        [new TileTypeVariant(Enums.TileType.Grass, Enums.TileVariant.FullBlock)] = [new Vector2I(1, 0)],
        [new TileTypeVariant(Enums.TileType.Grass, Enums.TileVariant.HalfBlockLower)] = [new Vector2I(1, 0)],
        [new TileTypeVariant(Enums.TileType.Rock, Enums.TileVariant.FullBlock)] = [new Vector2I(1, 3)],
        [new TileTypeVariant(Enums.TileType.Rock, Enums.TileVariant.HalfBlockLower)] = [new Vector2I(1, 3)],
        [new TileTypeVariant(Enums.TileType.Sand, Enums.TileVariant.FullBlock)] = [new Vector2I(1, 6)],
        [new TileTypeVariant(Enums.TileType.Sand, Enums.TileVariant.HalfBlockLower)] = [new Vector2I(1, 6)],
    };

    public Vector2I? GetAtlasCoords(LevelTile levelTile, int z)
    {
        if (z > levelTile.Height)
        {
            if ((levelTile.WaterLevel ?? -1) == z) return _waterAtlasCoords;
            return null;
        };

        Enums.TileVariant variantToUse = z == levelTile.Height ? levelTile.TileVariant : Enums.TileVariant.FullBlock;

        if (!_atlasCoordsMap.TryGetValue(new TileTypeVariant(levelTile.TileType, variantToUse), out var coordArray)) 
            return _defaultAtlasCoords;
        return coordArray[new Random().NextInt64(coordArray.Length)];
    }
}

internal record struct TileTypeVariant(Enums.TileType TileType, Enums.TileVariant TileVariant);
