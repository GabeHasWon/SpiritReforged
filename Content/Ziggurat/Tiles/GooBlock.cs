using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.TileCommon.TileMerging;
using SpiritReforged.Content.Desert.Tiles;

namespace SpiritReforged.Content.Ziggurat.Tiles;

public class GooBlock : ModTile, IAutoloadTileItem
{
	public void AddItemRecipes(ModItem item) => item.CreateRecipe(10).AddIngredient(AutoContent.ItemType<PolishedAmber>(), 10).AddCondition(Condition.InGraveyard).Register();

	public override void SetStaticDefaults()
	{
		Main.tileSolid[Type] = true;
		Main.tileBlockLight[Type] = false;

		TileID.Sets.GeneralPlacementTiles[Type] = false;
		TileID.Sets.CanBeClearedDuringOreRunner[Type] = false;

		this.Merge(ModContent.TileType<PaleHive>(), ModContent.TileType<GooeyHive>());
		AddMapEntry(new Color(220, 115, 25));

		DustType = DustID.OrangeStainedGlass;
	}

	public override void FloorVisuals(Player player)
	{
		if (!Main.gamePaused && (int)player.velocity.X != 0 && Main.rand.NextBool(4))
		{
			Vector2 velocity = player.velocity * 0.2f + Vector2.UnitY * -0.5f;
			Vector2 position = player.Bottom + new Vector2(0, 6 * player.gravDir);

			var dust = Dust.NewDustPerfect(position, DustID.BunnySlime, velocity, 180, Color.Orange);
			dust.noGravity = true;
		}
	}

	public override void PostDraw(int i, int j, SpriteBatch spriteBatch) => TileMerger.DrawMerge(spriteBatch, i, j, ModContent.TileType<PaleHive>(), ModContent.TileType<GooeyHive>());
}