using SpiritReforged.Common.TileCommon.TileSway;
using SpiritReforged.Common.Visuals.Glowmasks;
using Terraria.DataStructures;
using Terraria.GameContent.Drawing;

namespace SpiritReforged.Common.TileCommon.PresetTiles;

[AutoloadGlowmask("255,165,0", false)]
public abstract class ChandelierTile : FurnitureTile
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

		TileID.Sets.MultiTileSway[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style3x3);
		TileObjectData.newTile.StyleHorizontal = true;
		TileObjectData.newTile.AnchorTop = new AnchorData(AnchorType.SolidTile, AnchorDataOffsets.width, AnchorDataOffsets.count);
		TileObjectData.newTile.AnchorBottom = AnchorData.Empty;
		TileObjectData.newTile.Origin = new Point16(1, 0);
		TileObjectData.addTile(Type);

		AddToArray(ref TileID.Sets.RoomNeeds.CountsAsTorch);
		AddMapEntry(CommonColor, Language.GetText("MapObject.Chandelier"));
		AdjTiles = [TileID.Chandeliers];
		DustType = -1;
	}

	public override void AdjustMultiTileVineParameters(int i, int j, ref float? overrideWindCycle, ref float windPushPowerX, ref float windPushPowerY, ref bool dontRotateTopTiles,
		ref float totalWindMultiplier, ref Texture2D glowTexture, ref Color glowColor)
	{
		overrideWindCycle = 1;
		windPushPowerY = 0;

		if (GlowmaskTile.TileIdToGlowmask.TryGetValue(Type, out var glowmask))
		{
			glowColor = glowmask.GetDrawColor(new Point(i, j));
			glowTexture = glowmask.Glowmask.Value;
		}
	}

	public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
	{
		Tile tile = Main.tile[i, j];

		if (TileObjectData.IsTopLeft(tile))
			Main.instance.TilesRenderer.AddSpecialPoint(i, j, TileDrawing.TileCounterType.MultiTileVine);

		return false;
	}

	public override void HitWire(int i, int j)
	{
		var data = TileObjectData.GetTileData(Type, 0);
		int width = data.CoordinateFullWidth;

		TileExtensions.GetTopLeft(ref i, ref j);

		for (int x = i; x < i + 3; x++)
		{
			for (int y = j; y < j + 3; y++)
			{
				var tile = Framing.GetTileSafely(x, y);
				tile.TileFrameX += (short)((tile.TileFrameX < width) ? width : -width);

				Wiring.SkipWire(x, y);
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

	public virtual float Physics(Point16 topLeft)
	{
		var data = TileObjectData.GetTileData(Framing.GetTileSafely(topLeft));
		float rotation = Main.instance.TilesRenderer.GetWindCycle(topLeft.X, topLeft.Y, TileSwaySystem.SunflowerWindCounter);

		if (!WorldGen.InAPlaceWithWind(topLeft.X, topLeft.Y, data.Width, data.Height))
			rotation = 0f;

		return rotation + TileSwayHelper.GetHighestWindGridPushComplex(topLeft.X, topLeft.Y, data.Width, data.Height, 60, 1.26f, 3, true);
	}
}
