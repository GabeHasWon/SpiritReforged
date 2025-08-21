using SpiritReforged.Common.Misc;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.TileCommon.PresetTiles;
using SpiritReforged.Content.Underground.Pottery;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Underground.Tiles;

public class CommonPots : PotTile, ILootable
{
	public override Dictionary<string, int[]> TileStyles => new()
	{
		{ "Mushroom", [0, 1, 2] },
		{ "Granite", [3, 4, 5] }
	};

	private static int GetStyle(Tile t) => t.TileFrameY / 36;

	public override void AddItemRecipes(ModItem modItem, StyleDatabase.StyleGroup group, Condition condition)
	{
		int type = ModContent.TileType<PotteryWheel>();
		switch (group.name)
		{
			case "CommonPotsMushroom":
				modItem.CreateRecipe().AddRecipeGroup("ClayAndMud", 3).AddIngredient(ItemID.GlowingMushroom).AddTile(type).AddCondition(condition).Register();
				break;

			case "CommonPotsGranite":
				modItem.CreateRecipe().AddRecipeGroup("ClayAndMud", 3).AddIngredient(ItemID.Granite, 3).AddTile(type).AddCondition(condition).Register();
				break;
		}
	}

	public override bool CreateDust(int i, int j, ref int type)
	{
		if (!IsRubble)
		{
			type = GetStyle(Main.tile[i, j]) switch
			{
				0 => DustID.Pot,
				1 => DustID.Granite,
				_ => -1,
			};
		}

		return true;
	}

	public override void KillTile(int i, int j, ref bool fail, ref bool effectOnly, ref bool noItem)
	{
		if (effectOnly || fail || IsRubble)
			return;

		var tile = Main.tile[i, j];
		int style = GetStyle(tile);

		FallingPot.BreakPot(i, j, (style == 0) ? tile.TileFrameX / 36 * 3 : 2000 / 16);

		if (TileObjectData.IsTopLeft(i, j))
		{
			if (style == 1)
			{
				for (int g = 1; g < 5; g++)
				{
					int goreType = Mod.Find<ModGore>("Granite" + g).Type;
					Gore.NewGore(new EntitySource_TileBreak(i, j), new Vector2(i, j) * 16, Vector2.Zero, goreType);
				}
			}
		}
	}

	public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
	{
		if (GetStyle(Main.tile[i, j]) == 0)
			Lighting.AddLight(new Vector2(i, j).ToWorldCoordinates(), Color.Blue.ToVector3() * 0.8f);

		return true;
	}

	public void AddLoot(ILoot loot)
	{
		if (TileLootHandler.TryGetLootPool(ModContent.TileType<Pots>(), out var dele))
			dele.Invoke(loot);
	}
}