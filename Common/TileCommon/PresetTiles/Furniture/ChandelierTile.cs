using SpiritReforged.Common.TileCommon.TileSway;
using SpiritReforged.Common.Visuals.Glowmasks;
using Terraria.DataStructures;

namespace SpiritReforged.Common.TileCommon.PresetTiles;

[AutoloadGlowmask("255,165,0", false)]
public abstract class ChandelierTile : FurnitureTile, ISwayTile
{
	/// <summary> Offsets the anchor and how wide it needs to be. Defaults to (1, 1), meaning the anchor only needs 1 tile in the middle of the 3 tile wide chandelier. </summary>
	public virtual (int width, int count) AnchorDataOffsets => (1, 1);

	public override void SetItemDefaults(ModItem item) => item.Item.value = Item.sellPrice(silver: 6);

	public override void AddItemRecipes(ModItem item)
	{
		if (Info.Material != ItemID.None)
			item.CreateRecipe().AddIngredient(Info.Material, 4).AddIngredient(ItemID.Torch, 4).AddIngredient(ItemID.Chain).AddTile(TileID.Anvils).Register();
	}

	public override void StaticDefaults()
	{
		Main.tileFrameImportant[Type] = true;
		Main.tileNoAttach[Type] = true;
		Main.tileLighted[Type] = true;
		Main.tileLavaDeath[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style3x3);
		TileObjectData.newTile.AnchorTop = new AnchorData(AnchorType.SolidTile, AnchorDataOffsets.width, AnchorDataOffsets.count);
		TileObjectData.newTile.AnchorBottom = AnchorData.Empty;
		TileObjectData.newTile.Origin = new Point16(1, 0);
		TileObjectData.addTile(Type);

		AddToArray(ref TileID.Sets.RoomNeeds.CountsAsTorch);
		AddMapEntry(new Color(100, 100, 60), Language.GetText("MapObject.Chandelier"));
		AdjTiles = [TileID.Chandeliers];
		DustType = -1;
	}

	public override void HitWire(int i, int j)
	{
		var data = TileObjectData.GetTileData(Type, 0);
		int width = data.CoordinateFullWidth;

		j -= Framing.GetTileSafely(i, j).TileFrameY / 18; //Move to the multitile's top

		for (int h = 0; h < 2; h++)
		{
			var tile = Framing.GetTileSafely(i, j + h);
			tile.TileFrameX += (short)((tile.TileFrameX < width) ? width : -width);

			Wiring.SkipWire(i, j + h);
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

	public void DrawSway(int i, int j, SpriteBatch spriteBatch, Vector2 offset, float rotation, Vector2 origin)
	{
		if (!TileExtensions.GetVisualInfo(i, j, out var color, out var texture))
			return;

		var t = Main.tile[i, j];
		var data = TileObjectData.GetTileData(t);

		int height = data.CoordinateHeights[t.TileFrameY / data.CoordinateFullHeight];
		var source = new Rectangle(t.TileFrameX, t.TileFrameY, 16, height);
		var position = new Vector2(i * 16 - (int)Main.screenPosition.X, j * 16 - (int)Main.screenPosition.Y) + offset;

		//Draw normally
		spriteBatch.Draw(texture, position, source, color, rotation, origin, 1, SpriteEffects.None, 0);

		var glowTexture = GlowmaskTile.TileIdToGlowmask[Type].Glowmask.Value;

		if (Info is LightedInfo l && l.Blur)
		{
			ulong randSeed = Main.TileFrameSeed ^ (ulong)((long)j << 32 | (uint)i);
			for (int c = 0; c < 7; c++) //Draw our glowmask with a randomized position
			{
				float shakeX = Utils.RandomInt(ref randSeed, -10, 11) * 0.15f;
				float shakeY = Utils.RandomInt(ref randSeed, -10, 1) * 0.35f;
				var shakeOffset = new Vector2(shakeX, shakeY);

				spriteBatch.Draw(glowTexture, position + shakeOffset, source, 
					new Color(100, 100, 100, 0), rotation, origin, 1, SpriteEffects.None, 0f);
			}
		}
		else
		{
			spriteBatch.Draw(glowTexture, position, source, Color.White, 
				rotation, origin, 1, SpriteEffects.None, 0);
		}
	}
}
