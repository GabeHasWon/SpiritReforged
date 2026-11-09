using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.ModCompat;

namespace SpiritReforged.Content.Crossmod.Spooky.Tiles;

public class DhampirRelic : ModItem
{
	public override bool IsLoadingEnabled(Mod mod) => CrossMod.Spooky.Enabled;
	public override void SetDefaults() => Item.DefaultToRelic(1);
}

public class MummyRelic : ModItem
{
	public override bool IsLoadingEnabled(Mod mod) => CrossMod.Spooky.Enabled;
	public override void SetDefaults() => Item.DefaultToRelic(2);
}