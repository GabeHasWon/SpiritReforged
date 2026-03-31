using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.TileCommon.TileMerging;
using SpiritReforged.Content.Desert.Tiles;
using SpiritReforged.Content.Ziggurat.NPCs;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Ziggurat.Tiles;

public class GooeyHive : ModTile
{
	public void AddItemRecipes(ModItem item) => item.CreateRecipe(10).AddIngredient(AutoContent.ItemType<PolishedAmber>(), 10).AddCondition(Condition.InGraveyard).Register();

	public override void SetStaticDefaults()
	{
		Main.tileSolid[Type] = true;
		Main.tileBlockLight[Type] = true;

		TileID.Sets.GeneralPlacementTiles[Type] = false;
		TileID.Sets.CanBeClearedDuringOreRunner[Type] = false;

		this.Merge(TileID.Sand, ModContent.TileType<CrackedSandstone>(), ModContent.TileType<PaleHive>());
		AddMapEntry(new Color(180, 180, 180));

		DustType = DustID.Silk;
		RegisterItemDrop(AutoContent.ItemType<PaleHive>());
	}

	public override void PostTileFrame(int i, int j, int up, int down, int left, int right, int upLeft, int upRight, int downLeft, int downRight)
	{
		Tile t = Main.tile[i, j];

		if (Main.rand.NextBool(15) && t.TileFrameX is 18 or 36 or 54 && t.TileFrameY is 18) //Plain center frames
		{
			Point16 result = new(162, 72);

			t.TileFrameX = result.X;
			t.TileFrameY = result.Y;
		}
	}

	public override void KillTile(int i, int j, ref bool fail, ref bool effectOnly, ref bool noItem)
	{
		if (Main.netMode != NetmodeID.MultiplayerClient && Main.rand.NextBool(4))
			NPC.NewNPCDirect(new EntitySource_TileBreak(i, j), new Vector2(i, j).ToWorldCoordinates(), ModContent.NPCType<TinyGrub>());
	}

	public override void PostDraw(int i, int j, SpriteBatch spriteBatch) => TileMerger.DrawMerge(spriteBatch, i, j, TileID.Sand);
}