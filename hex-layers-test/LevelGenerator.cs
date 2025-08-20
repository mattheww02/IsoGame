using Godot;
using HexLayersTest.Objects;
using LibNoise.Primitive;
using LibNoise.Transformer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HexLayersTest;

public class LevelGenerator
{
    private readonly LevelArray _level;
    private readonly int _sizeX, _sizeY, _maxLandHeight;
    private readonly int _seed;
    private readonly int _globalWaterLevel;

    private float[,] _heightMap;

    private const float NoiseMapScale = 5.0f;
    private const int MinTileHeight = 0;

    public LevelGenerator(
        int sizeX, int sizeY, 
        int maxLandHeight, 
        int seed = 0, 
        int globalWaterLevel = 0)
    {
        _level = new(sizeX, sizeY);
        _sizeX = sizeX;
        _sizeY = sizeY;
        _maxLandHeight = maxLandHeight;
        _seed = seed;
        _globalWaterLevel = globalWaterLevel;
    }

    public LevelArray GenerateLevel()
    {
        _level.Initialise();
        GenerateHeightMap();
        SetTileTypes();
        SetTileVariants();
        SetTileNavigability();
        return _level;
    }

    private void GenerateHeightMap()
    {
        _heightMap = new float[_sizeX, _sizeY];
        var noiseGenerator = new FastNoiseLite
        {
            NoiseType = FastNoiseLite.NoiseTypeEnum.Perlin,
            FractalOctaves = _maxLandHeight,
            FractalGain = 0.2f,
            Seed = _seed,
        };

        foreach (var (x, y) in _level.GetIndices())
        {
            var noise = (noiseGenerator.GetNoise2D(x * NoiseMapScale, y * NoiseMapScale) + 1.0f) * _maxLandHeight / 2.0f;
            _heightMap[x, y] = noise;
        }
            
        _level.ForEach((tile, x, y) => tile.Height = Mathf.Clamp((int)Math.Round(_heightMap[x, y]), MinTileHeight, _maxLandHeight - 1));
        _level.ForEach(tile => tile.WaterLevel = _globalWaterLevel);
    }

    private void SetTileTypes()
    {
        _level.ForEach(tile => tile.TileType = (tile.Height / (float)_maxLandHeight) switch
        {
            < 0.35f => Enums.TileType.Rock,
            < 0.6f => Enums.TileType.Sand,
            _ => Enums.TileType.Grass
        });
    }

    private void SetTileVariants()
    {
        _level.ForEach((tile, x, y) =>
        {
            var roundedDiff =  _heightMap[x, y] - tile.Height;
            if (roundedDiff > 0.25f && tile.Height < _maxLandHeight)
            {
                tile.Height++;
                tile.TileVariant = Enums.TileVariant.HalfBlockLower;
            }
            else if (roundedDiff < -0.25f)
            {
                tile.TileVariant = Enums.TileVariant.HalfBlockLower;
            }
            else
            {
                tile.TileVariant = Enums.TileVariant.FullBlock;
            }
        });
    }

    private void SetTileNavigability()
    {
        _level.ForEach(tile =>
        {
            tile.Navigable = tile.WaterLevel <= tile.Height;
        });
    }
}
