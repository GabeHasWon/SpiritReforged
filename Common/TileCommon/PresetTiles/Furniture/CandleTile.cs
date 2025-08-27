using SpiritReforged.Common.Visuals.Glowmasks;
using Terraria.GameContent.Drawing;

namespace SpiritReforged.Common.TileCommon.PresetTiles;

[AutoloadGlowmask("191,124,0", false)]
public abstract class CandleTile : FurnitureTile
{
	public override void SetItemDefaults(ModItem item) => item.Item.value = Item.sellPrice(copper: 30);

	public override void AddItemRecipes(ModItem item)
	{
		if (Info.Material != ItemID.None)
			item.CreateRecipe().AddIngredient(Info.Material, 4).AddIngredient(ItemID.Torch).AddTile(TileID.WorkBenches).Register();
	}

	public override void StaticDefaults()
	{
		Main.tileFrameImportant[Type] = true;
		Main.tileNoAttach[Type] = true;
		Main.tileLighted[Type] = true;
		Main.tileLavaDeath[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.StyleOnTable1x1);
		TileObjectData.addTile(Type);

		AddToArray(ref TileID.Sets.RoomNeeds.CountsAsTorch);
		AddMapEntry(new Color(100, 100, 60), Language.GetText("ItemName.Candle"));
		AdjTiles = [TileID.Candles];
		DustType = -1;
	}

	public override bool RightClick(int i, int j)
	{
		HitWire(i, j);
		return true;
	}

	public override void MouseOver(int i, int j)
	{
		Player Player = Main.LocalPlayer;
		Player.noThrow = 2;
		Player.cursorItemIconEnabled = true;
		Player.cursorItemIconID = Info.Item.Type;
	}

	public override void HitWire(int i, int j)
	{
		var tile = Framing.GetTileSafely(i, j);
		tile.TileFrameX = (short)((tile.TileFrameX == 0) ? 18 : 0);

		NetMessage.SendTileSquare(-1, i, j);
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

	public virtual bool BlurGlowmask => true;

	public override void PostDraw(int i, int j, SpriteBatch spriteBatch)
	{
		var tile = Framing.GetTileSafely(i, j);
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
