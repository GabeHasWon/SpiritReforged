namespace SpiritReforged.Common.NPCCommon;

internal class ShopConditions
{
	public static readonly Condition NotInSnow = new("Mods.SpiritReforged.Conditions.NotSnow", () => !Main.LocalPlayer.ZoneSnow);
}
