using SpiritReforged.Content.Ocean;
using SpiritReforged.Content.SaltFlats.Tiles.Salt;
using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace SpiritReforged.Common.ConfigurationCommon;

class ReforgedClientConfig : ModConfig
{
	public override ConfigScope Mode => ConfigScope.ClientSide;

	[DefaultValue(OceanGeneration.OceanShape.Piecewise_V)]
	public OceanGeneration.OceanShape OceanShape { get; set; }

	[Range(0, 3)]
	[DrawTicks]
	[Slider]
	[DefaultValue(3)]
	public int ReflectionDetail { get; set; }

	[DefaultValue(true)]
	public bool AmbientSounds { get; set; }

	public override void OnChanged()
	{
		int type = ModContent.TileType<SaltBlockReflective>();
		if (type == TileID.Dirt)
			return;

		if (ReflectionDetail == 0)
		{
			Main.tileBlockLight[type] = true;
			Main.tileNoSunLight[type] = true;
		}
		else
		{
			Main.tileBlockLight[type] = false;
			Main.tileNoSunLight[type] = false;
		}
	}
}