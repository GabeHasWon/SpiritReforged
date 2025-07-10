using SpiritReforged.Content.Ocean;
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
	[DefaultValue(2)]
	public int ReflectionDetail { get; set; }
}