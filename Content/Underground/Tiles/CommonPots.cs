using RubbleAutoloader;
using SpiritReforged.Common.TileCommon.PresetTiles;
using SpiritReforged.Content.Underground.Pottery;
using static SpiritReforged.Common.TileCommon.StyleDatabase;

namespace SpiritReforged.Content.Underground.Tiles;

public class CommonPots : PotTile, ILootTile
{
	public override Dictionary<string, int[]> TileStyles => new()
	{
		{ "Mushroom", [0, 1, 2] },
		{ "Granite", [3, 4, 5] }
	};

	private static int GetStyle(Tile t) => t.TileFrameY / 36;

	public override void AddItemRecipes(ModItem modItem, StyleGroup group)
	{
		int wheel = ModContent.TileType<PotteryWheel>();
		LocalizedText dicovered = AutoloadedPotItem.Discovered;
		var function = (modItem as AutoloadedPotItem).RecordedPot;

		switch (group.name)
		{
			case "CommonPotsMushroom":
				modItem.CreateRecipe().AddRecipeGroup("ClayAndMud", 3).AddIngredient(ItemID.GlowingMushroom).AddTile(wheel).AddCondition(dicovered, function).Register();
				break;

			case "CommonPotsGranite":
				modItem.CreateRecipe().AddRecipeGroup("ClayAndMud", 3).AddIngredient(ItemID.Granite, 3).AddTile(wheel).AddCondition(dicovered, function).Register();
				break;
		}
	}

	public override bool CreateDust(int i, int j, ref int type)
	{
		if (!Autoloader.IsRubble(Type))
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
		if (effectOnly || fail || Autoloader.IsRubble(Type))
			return;

		//Do vanilla pot break effects
		var t = Main.tile[i, j];
		short oldFrameY = t.TileFrameY;

		t.TileFrameY = (GetStyle(t) == 0) ? t.TileFrameX : (short)2000; //2000 means no additional gores or effects
		WorldGen.CheckPot(i, j);
		t.TileFrameY = oldFrameY;
	}

	public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
	{
		if (GetStyle(Main.tile[i, j]) == 0)
			Lighting.AddLight(new Vector2(i, j).ToWorldCoordinates(), Color.Blue.ToVector3() * .8f);

		return true;
	}

	public void AddLoot(int objectStyle, ILoot loot) => ModContent.GetInstance<Pots>().AddLoot(objectStyle, loot);
}