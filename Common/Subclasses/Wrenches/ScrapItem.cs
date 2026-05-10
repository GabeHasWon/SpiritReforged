using Terraria.Audio;

namespace SpiritReforged.Common.Subclasses.Wrenches;

internal class ScrapItem : ModItem
{
	public override void SetDefaults()
	{
		Item.CloneDefaults(ItemID.Silk);
		Item.Size = new(22);
	}

	public override bool ItemSpace(Player player) => true;

	public override bool OnPickup(Player player)
	{
		player.GetModPlayer<WrenchPlayer>().ModifyScrap(Item.stack);
		SoundEngine.PlaySound(SoundID.Grab with { PitchRange = (-0.3f, 0.3f) }, player.Center);
		return false;
	}
}
