using Terraria.DataStructures;

namespace SpiritReforged.Common.Subclasses.Greatshields;

internal abstract class GreatshieldItem : ModItem
{
	public static Dictionary<int, Asset<Texture2D>> ShieldToHeldTexture = [];

	public override void SetStaticDefaults() => ShieldToHeldTexture.Add(Type, ModContent.Request<Texture2D>(Texture + "_Held"));

	public virtual void ModifyLayerDrawing(ref DrawData data) { }

	public override void HoldItem(Player player)
	{
		if (Main.myPlayer == player.whoAmI)
			player.ChangeDir(Math.Sign(Main.MouseWorld.X - player.Center.X));
	}
}
