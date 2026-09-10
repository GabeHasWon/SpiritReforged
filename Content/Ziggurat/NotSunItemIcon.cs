namespace SpiritReforged.Content.Ziggurat;

internal class NotSunItemIcon : ModItem
{
	public override void SetStaticDefaults() => Item.ResearchUnlockCount = 0;
	public override void SetDefaults() => Item.CloneDefaults(ItemID.SleepingIcon);
}
