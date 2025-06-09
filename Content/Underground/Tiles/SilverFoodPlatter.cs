using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.TileCommon.PresetTiles;
using Terraria.DataStructures;
using Terraria.GameContent.Drawing;
using Terraria.UI;

namespace SpiritReforged.Content.Underground.Tiles;

public class SilverFoodPlatter : SingleSlotTile<PlatterSlot>, IAutoloadTileItem
{
	public override void SetStaticDefaults()
	{
		base.SetStaticDefaults();

		Main.tileSolid[Type] = false;
		Main.tileMergeDirt[Type] = false;
		Main.tileBlockLight[Type] = false;
		Main.tileFrameImportant[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style2x1);
		TileObjectData.newTile.DrawYOffset = 2;
		TileObjectData.newTile.RandomStyleRange = 3;
		TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop | AnchorType.Table, TileObjectData.newTile.Width, 0);
		TileObjectData.newTile.HookPostPlaceMyPlayer = new PlacementHook(entity.Hook_AfterPlacement, -1, 0, false);
		TileObjectData.newTile.StyleHorizontal = true;
		TileObjectData.addTile(Type);

		RegisterItemDrop(ItemType); //Register for all alternative styles
		AddMapEntry(new Color(140, 140, 140));
		DustType = -1;
	}

	public override void PostDraw(int i, int j, SpriteBatch spriteBatch)
	{
		var t = Main.tile[i, j];

		if (!TileDrawing.IsVisible(t) || !TileObjectData.IsTopLeft(i, j))
			return;

		if (Entity(i, j) is PlatterSlot slot && !slot.item.IsAir)
		{
			int type = slot.item.type;

			Main.instance.LoadItem(type);

			var texture = TextureAssets.Item[type].Value;
			var source = texture.Frame(1, 3, 0, 2);
			var origin = new Vector2(source.Width / 2, source.Height);

			var lightColor = Lighting.GetColor(i, j);
			var currentColor = lightColor;
			float scale = 1f;
			ItemSlot.GetItemLight(ref currentColor, ref scale, slot.item);

			int yOffset = 18 - t.TileFrameX / 36 * 2;

			var position = new Vector2(i * 16 - (int)Main.screenPosition.X + 16, j * 16 - (int)Main.screenPosition.Y + yOffset) + TileExtensions.TileOffset;
			spriteBatch.Draw(texture, position, source, currentColor, 0f, origin, scale, default, 0);

			if (slot.item.color != default)
				spriteBatch.Draw(texture, position, source, slot.item.GetColor(lightColor), 0f, origin, scale, default, 0);
		}
	}
}

public class PlatterSlot : SingleSlotEntity
{
	public override bool PlayDroppedAnimation => false;
	public override bool CanAddItem(Item item) => ItemID.Sets.IsFood[item.type];

	public override bool IsTileValidForEntity(int x, int y)
	{
		var t = Framing.GetTileSafely(x, y);
		return t.HasTile && t.TileType == ModContent.TileType<SilverFoodPlatter>();
	}
}