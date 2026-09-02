using Terraria.UI;

namespace SpiritReforged.Common.ItemCommon;

/// <summary> Used to create items with variants by setting <see cref="VariantCounts"/> in SetStaticDefaults. This does not exist on the server. </summary>
[ReinitializeDuringResizeArrays]
[Autoload(Side = ModSide.Client)]
public sealed class VariantItemRenderer : GlobalItem
{
	public static readonly int[] VariantCounts = ItemID.Sets.Factory.CreateIntSet();

	#region helpers
	public static int GetVariant(Item item) => (item.TryGetGlobalItem(out VariantItemRenderer global) ? global.subID ?? 0 : 0) % VariantCounts[item.type];

	public static Texture2D GetTexture(Item item, out Rectangle source)
	{
		if (item.ModItem is ModItem modItem && VariantCounts[item.type] > 0)
		{
			string texturePath = modItem.Texture;

			if (texturePath[texturePath.Length - 1] == '0') //If the last character is zero (one of the variants), remove the character
				texturePath = texturePath.Remove(texturePath.Length - 1);

			Texture2D result = AssetLoader.GetTexture(modItem.Name + GetVariant(item), texturePath + GetVariant(item)).Value;
			source = (Main.itemAnimations[item.type] != null) ? Main.itemAnimations[item.type].GetFrame(result) : result.Frame();

			return result;
		}

		Texture2D texture = TextureAssets.Item[item.type].Value;
		source = (Main.itemAnimations[item.type] != null) ? Main.itemAnimations[item.type].GetFrame(texture) : texture.Frame();

		return texture;
	}
	#endregion

	public override bool InstancePerEntity => true;

	public int? subID;

	public override bool AppliesToEntity(Item entity, bool lateInstantiation) => VariantCounts[entity.type] > 0;

	public override void OnStack(Item destination, Item source, int numToTransfer)
	{
		if (source.TryGetGlobalItem(out VariantItemRenderer global))
			subID = global.subID;
	}

	public override bool PreDrawInWorld(Item item, SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
	{
		subID ??= Main.rand.Next(VariantCounts[item.type]);

		Texture2D texture = GetTexture(item, out _);
		ItemMethods.DrawInWorld(item, lightColor, rotation, scale, texture);
		return false;
	}

	public override bool PreDrawInInventory(Item item, SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
	{
		subID ??= Main.rand.Next(VariantCounts[item.type]);

		Texture2D texture = GetTexture(item, out Rectangle source);

		//Scale the item according to 'source' instead of 'frame'
		ItemSlot.DrawItem_GetColorAndScale(item, Main.inventoryScale, ref drawColor, 32, ref source, out _, out scale);

		spriteBatch.Draw(texture, position, source, drawColor, 0, source.Size() / 2, scale, 0, 0);
		return false;
	}
}