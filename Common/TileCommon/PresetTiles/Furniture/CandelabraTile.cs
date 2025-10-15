using SpiritReforged.Common.Visuals.Glowmasks;
using Terraria.DataStructures;
using Terraria.GameContent.Drawing;

namespace SpiritReforged.Common.TileCommon.PresetTiles;

[AutoloadGlowmask("255,165,0", false)]
public abstract class CandelabraTile : FurnitureTile
{
	public override void SetItemDefaults(ModItem item) => item.Item.value = Item.sellPrice(silver: 3);
	public override void AddItemRecipes(ModItem item)
	{
		if (Info.Material != ItemID.None)
			item.CreateRecipe().AddIngredient(Info.Material, 5).AddIngredient(ItemID.Torch, 5).AddTile(TileID.WorkBenches).Register();
	}

	public override void StaticDefaults()
	{
		Main.tileFrameImportant[Type] = true;
		Main.tileNoAttach[Type] = true;
		Main.tileLighted[Type] = true;
		Main.tileLavaDeath[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
		TileObjectData.newTile.Origin = new Point16(1, 1);
		TileObjectData.newTile.CoordinateHeights = [16, 18];
		TileObjectData.newTile.DrawYOffset = 2;
		TileObjectData.addTile(Type);

		AddToArray(ref TileID.Sets.RoomNeeds.CountsAsTorch);
		AddMapEntry(CommonColor, Language.GetText("ItemName.Candelabra"));
		AdjTiles = [TileID.Candelabras];
		DustType = -1;
	}

	public override void HitWire(int i, int j)
	{
		var data = TileObjectData.GetTileData(Type, 0);
		int width = data.CoordinateFullWidth;

		//Move to the multitile's top left
		(i, j) = (i - Framing.GetTileSafely(i, j).TileFrameY / 18, j - Framing.GetTileSafely(i, j).TileFrameX % width / 18);

		for (int y = 0; y < 2; y++)
		{
			for (int x = 0; x < 2; x++)
			{
				var tile = Framing.GetTileSafely(i + x, j + y);
				tile.TileFrameX += (short)((tile.TileFrameX < width) ? width : -width);

				Wiring.SkipWire(i + x, j + y);
			}
		}

		NetMessage.SendTileSquare(-1, i, j, data.Width, data.Height);
	}

	public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
	{
		var tile = Main.tile[i, j];

		if (tile.TileFrameX == 18 && tile.TileFrameY == 0)
		{
			var color = (Info is LightedInfo l) ? l.Light : Color.Orange.ToVector3() / 255f;
			(r, g, b) = (color.X, color.Y, color.Z);
		}
	}

	public override void PostDraw(int i, int j, SpriteBatch spriteBatch)
	{
		var tile = Main.tile[i, j];
		if (!TileDrawing.IsVisible(tile))
			return;

		var texture = GlowmaskTile.TileIdToGlowmask[Type].Glowmask.Value;
		var data = TileObjectData.GetTileData(tile);
		int height = data.CoordinateHeights[tile.TileFrameY / data.CoordinateFullHeight];
		var source = new Rectangle(tile.TileFrameX, tile.TileFrameY, 16, height);

		if (Info is LightedInfo l && l.Blur)
		{
			ulong randSeed = Main.TileFrameSeed ^ (ulong)((long)j << 32 | (uint)i);
			for (int c = 0; c < 7; c++) //Draw our glowmask with a randomized position
			{
				float shakeX = Utils.RandomInt(ref randSeed, -10, 11) * 0.15f;
				float shakeY = Utils.RandomInt(ref randSeed, -10, 1) * 0.35f;
				var offset = new Vector2(shakeX, shakeY);

				var position = new Vector2(i, j) * 16 - Main.screenPosition + offset + TileExtensions.TileOffset;
				spriteBatch.Draw(texture, position, source, new Color(100, 100, 100, 0), 0, Vector2.Zero, 1, SpriteEffects.None, 0f);
			}
		}
		else
		{
			var position = new Vector2(i, j) * 16 - Main.screenPosition + TileExtensions.TileOffset;
			spriteBatch.Draw(texture, position, source, Color.White, 0, Vector2.Zero, 1, SpriteEffects.None, 0);
		}
	}
}
