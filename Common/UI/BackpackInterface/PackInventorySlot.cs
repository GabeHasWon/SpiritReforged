using SpiritReforged.Common.UI.Misc;
using SpiritReforged.Common.Visuals;
using Terraria.UI;

namespace SpiritReforged.Common.UI.BackpackInterface;

internal class PackInventorySlot(Item[] items, int index) : BasicItemSlot(items, index, ItemSlot.Context.ChestItem, .6f)
{
	private static readonly Asset<Texture2D> Favourite = DrawHelpers.RequestLocal(typeof(PackInventorySlot), "Slot_Favourite", false);
	
	protected override void DrawSelf(SpriteBatch spriteBatch)
	{
		if (_items[_index].favorited && Favourite.IsLoaded) //Draw a unique favourite texture
		{
			var oldTexture = TextureAssets.InventoryBack10;
			TextureAssets.InventoryBack10 = Favourite;

			base.DrawSelf(spriteBatch);

			TextureAssets.InventoryBack10 = oldTexture;
			return;
		}

		base.DrawSelf(spriteBatch);
	}
}