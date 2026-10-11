using SpiritReforged.Common.ItemCommon;

namespace SpiritReforged.Content.Crossmod.Spooky.Tiles;

public class DhampirRelic : ModItem
{
	public override void SetDefaults() => Item.DefaultToRelic(1);
}

public class MummyRelic : ModItem
{
	public override void SetDefaults() => Item.DefaultToRelic(2);
}