using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace SpiritReforged.Common.ConfigurationCommon;

class ReforgedServerConfig : ModConfig
{
	public override ConfigScope Mode => ConfigScope.ServerSide;

	[DefaultValue(true)]
	public bool VentCritters { get; set; }

	[Range(0, 400)]
	[DefaultValue(12)]
	public int MaxFloatingItemCount { get; set; }
}