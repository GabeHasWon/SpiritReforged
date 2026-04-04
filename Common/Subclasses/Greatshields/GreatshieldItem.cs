using Terraria.DataStructures;

namespace SpiritReforged.Common.Subclasses.Greatshields;

internal abstract class GreatshieldItem : ModItem
{
	public static Dictionary<int, Asset<Texture2D>> ShieldToHeldTexture = [];

	public override void SetStaticDefaults() => ShieldToHeldTexture.Add(Type, ModContent.Request<Texture2D>(Texture + "_Held"));

	public virtual void ModifyLayerDrawing(ref DrawData data) { }

	public override bool? UseItem(Player player)
	{
		if (!player.ItemTimeIsZero)
			return false;

		player.SetItemAnimation(Item.useAnimation);
		player.SetItemTime(Item.useTime);

		return true;
	}
}
