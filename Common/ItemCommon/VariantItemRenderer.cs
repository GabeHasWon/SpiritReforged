using Terraria.UI;

namespace SpiritReforged.Common.ItemCommon;

/// <summary> Used to create items with variants by setting <see cref="VariantCounts"/> in SetStaticDefaults. This does not exist on the server. </summary>
[ReinitializeDuringResizeArrays]
[Autoload(Side = ModSide.Client)]
public sealed class VariantItemRenderer : GlobalItem
{
	public static readonly int[] VariantCounts = ItemID.Sets.Factory.CreateIntSet();

	#region helpers
	public static int GetVariant(Item item) => (item.TryGetGlobalItem(out VariantItemRenderer global) ? global.subID : 0) % VariantCounts[item.type];

	public static Texture2D GetTexture(Item item, out Rectangle source)
	{
		if (item.ModItem is ModItem modItem && VariantCounts[item.type] > 0)
		{
			Texture2D result = AssetLoader.GetTexture(modItem.Name, modItem.Texture + GetVariant(item)).Value;
			source = (Main.itemAnimations[item.type] != null) ? Main.itemAnimations[item.type].GetFrame(result) : result.Frame();

			return result;
		}

		Texture2D texture = TextureAssets.Item[item.type].Value;
		source = (Main.itemAnimations[item.type] != null) ? Main.itemAnimations[item.type].GetFrame(texture) : texture.Frame();

		return texture;
	}
	#endregion

	public override bool InstancePerEntity => true;

	public int subID = -1;

	public override bool AppliesToEntity(Item entity, bool lateInstantiation) => VariantCounts[entity.type] > 0;

	public override bool PreDrawInWorld(Item item, SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
	{
		if (subID == -1)
			subID = Main.rand.Next(VariantCounts[item.type]);

		Texture2D texture = GetTexture(item, out Rectangle source);
		Vector2 position = item.position + item.Size - source.Size() - Main.screenPosition;

		spriteBatch.Draw(texture, position, source, GetAlpha(item, lightColor) ?? lightColor, rotation, Vector2.Zero, scale, 0, 0);
		return false;
	}

	public override bool PreDrawInInventory(Item item, SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
	{
		if (subID == -1)
			subID = Main.rand.Next(VariantCounts[item.type]);

		Texture2D texture = GetTexture(item, out Rectangle source);

		//Scale the item according to 'source' instead of 'frame'
		ItemSlot.DrawItem_GetColorAndScale(item, Main.inventoryScale, ref drawColor, 32, ref source, out _, out scale);

		spriteBatch.Draw(texture, position, source, drawColor, 0, source.Size() / 2, scale, 0, 0);
		return false;
	}
}