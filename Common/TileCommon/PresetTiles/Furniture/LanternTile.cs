using SpiritReforged.Common.Visuals.Glowmasks;
using Terraria.DataStructures;
using Terraria.GameContent.Drawing;

namespace SpiritReforged.Common.TileCommon.PresetTiles;

[AutoloadGlowmask("255,165,0", false)]
public abstract class LanternTile : FurnitureTile
{
	public override void SetItemDefaults(ModItem item) => item.Item.value = Item.sellPrice(copper: 30);

	public override void AddItemRecipes(ModItem item)
	{
		if (Info.Material != ItemID.None)
			item.CreateRecipe().AddIngredient(Info.Material, 6).AddIngredient(ItemID.Torch).AddTile(TileID.WorkBenches).Register();
	}

	public override void StaticDefaults()
	{
		Main.tileFrameImportant[Type] = true;
		Main.tileNoAttach[Type] = true;
		Main.tileLighted[Type] = true;
		Main.tileLavaDeath[Type] = true;

		TileID.Sets.MultiTileSway[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style1x2);
		TileObjectData.newTile.StyleHorizontal = true;
		TileObjectData.newTile.AnchorTop = new AnchorData(AnchorType.SolidTile, 1, 0);
		TileObjectData.newTile.AnchorBottom = AnchorData.Empty;
		TileObjectData.newTile.Origin = new Point16(0, 0);
		TileObjectData.newTile.CoordinateHeights = [16, 18];

		TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile);
		TileObjectData.newAlternate.AnchorTop = new AnchorData(AnchorType.Platform, 1, 0);
		TileObjectData.newAlternate.DrawYOffset = -8;
		TileObjectData.addAlternate(0);
		TileObjectData.addTile(Type);

		AddToArray(ref TileID.Sets.RoomNeeds.CountsAsTorch);
		AddMapEntry(CommonColor, Language.GetText("MapObject.Lantern"));
		AdjTiles = [TileID.HangingLanterns];
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

	public override void SetDrawPositions(int i, int j, ref int width, ref int offsetY, ref int height, ref short tileFrameX, ref short tileFrameY) => offsetY += 2;

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

		if (tile.TileFrameX < 18 && tile.TileFrameY == 18)
		{
			var color = (Info is LightedInfo l) ? l.Light : Color.Orange.ToVector3() / 255f;
			(r, g, b) = (color.X, color.Y, color.Z);
		}
	}
}
