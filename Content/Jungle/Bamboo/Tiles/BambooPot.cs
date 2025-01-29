using SpiritReforged.Common.TileCommon;
using SpiritReforged.Content.Jungle.Bamboo.Items;

namespace SpiritReforged.Content.Jungle.Bamboo.Tiles;

public class BambooPot : ModTile, IAutoloadTileItem
{
	public void SetItemDefaults(ModItem item)
	{
		item.Item.value = 50;
		item.Item.width = item.Item.height = 16;
	}

	public void AddItemRecipes(ModItem item) => item.CreateRecipe()
		.AddIngredient(ModContent.ItemType<StrippedBamboo>(), 5).AddTile(TileID.Sawmill).Register();

	public override void SetStaticDefaults()
	{
		Main.tileFrameImportant[Type] = true;
		Main.tileNoAttach[Type] = true;
		Main.tileLighted[Type] = true;
		Main.tileLavaDeath[Type] = true;
		TileObjectData.newTile.CopyFrom(TileObjectData.Style1x1);
		TileObjectData.newTile.Height = 1;
		TileObjectData.newTile.CoordinateHeights = [18];
		TileObjectData.addTile(Type);

		AddMapEntry(new Color(100, 100, 60));

		DustType = DustID.PalmWood;
		AdjTiles = [TileID.ClayPot];
	}

	public override void PlaceInWorld(int i, int j, Item item)
	{
		Framing.GetTileSafely(i, j).ClearTile();

		if (WorldGen.PlaceTile(i, j, TileID.ClayPot)) //Instantly turn into a clay pot with a modified appearance
			Framing.GetTileSafely(i, j).TileFrameY = 18;

		if (Main.netMode != NetmodeID.SinglePlayer)
			NetMessage.SendTileSquare(-1, i, j, 1);
	}
}

internal class BambooPotGlobalTile : GlobalTile //Hacky solution for modded clay pot tiles not being feasable
{
	private static bool IsBambooPot(int i, int j)
	{
		ushort type = Framing.GetTileSafely(i, j).TileType;
		return type == TileID.ClayPot && Framing.GetTileSafely(i, j).TileFrameY == 18;
	}

	public override bool PreDraw(int i, int j, int type, SpriteBatch spriteBatch)
	{
		if (!IsBambooPot(i, j))
			return true;

		Tile tile = Framing.GetTileSafely(i, j);
		Texture2D texture = TextureAssets.Tile[ModContent.TileType<BambooPot>()].Value;
		var source = new Rectangle(tile.TileFrameX % 16, tile.TileFrameY % 16, 16, 18);

		Vector2 offset = Lighting.LegacyEngine.Mode > 1 ? Vector2.Zero : Vector2.One * 12;
		Vector2 drawPos = (new Vector2(i, j) + offset) * 16 - Main.screenPosition + new Vector2(0, 2);

		spriteBatch.Draw(texture, drawPos, source, Lighting.GetColor(i, j), 0f, new Vector2(0, 0), 1f, SpriteEffects.None, 0f);

		return false;
	}

	public override void KillTile(int i, int j, int type, ref bool fail, ref bool effectOnly, ref bool noItem)
	{
		if (!IsBambooPot(i, j))
			return;

		Item.NewItem(new Terraria.DataStructures.EntitySource_TileBreak(i, j), i * 16, j * 16, 16, 16, Mod.Find<ModItem>("BambooPotItem").Type);
	}

	public override bool CanDrop(int i, int j, int type) => !IsBambooPot(i, j);
}