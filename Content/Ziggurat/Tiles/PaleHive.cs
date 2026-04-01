using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.TileCommon.TileMerging;
using SpiritReforged.Content.Desert.Tiles;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Ziggurat.Tiles;

public class PaleHive : ModTile, IAutoloadTileItem
{
	public void AddItemRecipes(ModItem item) => item.CreateRecipe(10).AddIngredient(AutoContent.ItemType<PolishedAmber>(), 10).AddCondition(Condition.InGraveyard).Register();

	public override void SetStaticDefaults()
	{
		Main.tileSolid[Type] = true;
		Main.tileBlockLight[Type] = true;

		TileID.Sets.GeneralPlacementTiles[Type] = false;
		TileID.Sets.CanBeClearedDuringOreRunner[Type] = false;

		this.Merge(TileID.Sand, ModContent.TileType<CrackedSandstone>());
		AddMapEntry(new Color(180, 180, 180));

		DustType = DustID.Silk;
		this.AutoItem().ResearchUnlockCount = 100;
	}

	public override void PostTileFrame(int i, int j, int up, int down, int left, int right, int upLeft, int upRight, int downLeft, int downRight)
	{
		var t = Main.tile[i, j];

		if (Main.rand.NextBool(15) && t.TileFrameX is 18 or 36 or 54 && t.TileFrameY is 18) //Plain center frames
		{
			Point16 result = new(162, 72);
			int random = Main.rand.Next(4);

			t.TileFrameX = (short)(result.X + 18 * random);
			t.TileFrameY = result.Y;
		}
	}

	public override void PostDraw(int i, int j, SpriteBatch spriteBatch) => TileMerger.DrawMerge(spriteBatch, i, j, TileID.Sand);
}