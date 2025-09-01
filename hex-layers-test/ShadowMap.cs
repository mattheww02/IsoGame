using Godot;
using HexLayersTest;
using HexLayersTest.Objects;
using System;
using System.Collections.Generic;

public partial class ShadowMap : BackBufferCopy
{
    [Export] private PackedScene _shadowMapLayerPackedScene;
	private ShadowMapLayer[] _shadowLayers;

	public void Initialise(int numLayers, LevelArray level)
	{
		_shadowLayers = new ShadowMapLayer[numLayers];

		for (int i = 0; i < numLayers; i++)
		{
			_shadowLayers[i] = _shadowMapLayerPackedScene.Instantiate<ShadowMapLayer>();
			_shadowLayers[i].LayerHeight = i;
			AddChild(_shadowLayers[i]);
		}

		level.ForEach((tile, x, y) =>
		{
			for (int i = 0; i < tile.Height; i++)
			{
				_shadowLayers[i].AddShadow(new Vector2I(x, y));
			}
		});
	}

}
