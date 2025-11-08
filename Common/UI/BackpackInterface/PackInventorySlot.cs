using SpiritReforged.Common.UI.Misc;
using SpiritReforged.Common.Visuals;
using Terraria.UI;

namespace SpiritReforged.Common.UI.BackpackInterface;

internal class PackInventorySlot(Item[] items, int index) : BasicItemSlot(items, index, ItemSlot.Context.ChestItem, .6f)
{
	private static readonly Asset<Texture2D> Favourite = DrawHelpers.RequestLocal(typeof(PackInventorySlot), "Slot_Favourite", false);

	internal bool Selected = false;
	
	protected override void DrawSelf(SpriteBatch spriteBatch)
	{
		if ((_items[_index].favorited || Selected) && Favourite.IsLoaded) //Draw a unique favourite texture
		{
			var oldChestBack = TextureAssets.InventoryBack5;
			ref var back = ref TextureAssets.InventoryBack5;

			if (Selected)
				back = TextureAssets.InventoryBack14;

			var oldTexture = TextureAssets.InventoryBack10;

			if (_items[_index].favorited)
				TextureAssets.InventoryBack10 = Favourite;

			base.DrawSelf(spriteBatch);

			TextureAssets.InventoryBack10 = oldTexture;
			back = oldChestBack;

			Selected = false;
			return;
		}

		base.DrawSelf(spriteBatch);
	}
}