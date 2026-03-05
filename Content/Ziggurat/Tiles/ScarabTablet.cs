using SpiritReforged.Common.Misc;
using SpiritReforged.Common.PlayerCommon;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.TileCommon.PresetTiles;
using System.Linq;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Ziggurat.Tiles;

public class ScarabTablet : ModTile
{
	public class ScarabTabletOneItem : ModItem
	{
		public void StaticItemDefaults() => ItemID.Sets.ShimmerTransformToItem[Type] = ModContent.ItemType<ScarabTabletTwoItem>();
		public override void SetDefaults() => Item.DefaultToPlaceableTile(ModContent.TileType<ScarabTablet>(), 0);
	}

	public class ScarabTabletTwoItem : ModItem
	{
		public void StaticItemDefaults() => ItemID.Sets.ShimmerTransformToItem[Type] = ModContent.ItemType<ScarabTabletOneItem>();
		public override void SetDefaults() => Item.DefaultToPlaceableTile(ModContent.TileType<ScarabTablet>(), 1);
	}

	public override void SetStaticDefaults()
	{
		const int width = 6;
		const int height = 4;

		Main.tileFrameImportant[Type] = true;
		Main.tileNoAttach[Type] = true;
		Main.tileLavaDeath[Type] = true;

		TileID.Sets.FramesOnKillWall[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
		TileObjectData.newTile.Width = width;
		TileObjectData.newTile.Height = height;
		TileObjectData.newTile.CoordinateHeights = [.. Enumerable.Repeat(16, height)];
		TileObjectData.newTile.Origin = new(width / 2, height / 2);

		TileObjectData.newTile.AnchorBottom = AnchorData.Empty;
		TileObjectData.newTile.AnchorTop = AnchorData.Empty;
		TileObjectData.newTile.AnchorWall = true;
		TileObjectData.addTile(Type);

		DustType = DustID.DynastyShingle_Red;
		AddMapEntry(FurnitureTile.CommonColor, Language.GetText("MapObject.Painting"));
	}

	public override void PostDraw(int i, int j, SpriteBatch spriteBatch)
	{
		if (Main.LocalPlayer.CheckFlag("Bedouin") != true)
			return;

		Vector2 worldPosition = new Vector2(i, j) * 16;
		float opacity = 1f - (float)(Main.LocalPlayer.DistanceSQ(worldPosition) / (100f * 100f));

		if (opacity > 0)
		{
			Tile tile = Main.tile[i, j];
			Texture2D texture = TextureAssets.Tile[Type].Value;
			Rectangle source = new(tile.TileFrameX + 108, tile.TileFrameY, 16, 16);
			Color color = (Color.Lerp(Color.PaleVioletRed, Color.Goldenrod, opacity) * opacity * 0.5f).Additive();

			spriteBatch.Draw(texture, new Vector2(i, j) * 16 - Main.screenPosition + TileExtensions.TileOffset, source, color, 0, Vector2.Zero, 1, 0, 0);
		}
	} //Bedouin armor set proximity glow
}