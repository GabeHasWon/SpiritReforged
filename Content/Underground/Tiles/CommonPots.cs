using RubbleAutoloader;
using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.TileCommon.PresetTiles;
using SpiritReforged.Content.Savanna.Tiles;
using SpiritReforged.Content.Underground.Pottery;
using Terraria.DataStructures;
using Terraria.GameContent.ItemDropRules;
using static SpiritReforged.Common.TileCommon.StyleDatabase;

namespace SpiritReforged.Content.Underground.Tiles;

public class CommonPots : PotTile, ILootTile
{
	public override Dictionary<string, int[]> TileStyles => new()
	{
		{ "Mushroom", [0, 1, 2] },
		{ "Granite", [3, 4, 5] },
		{ "Savanna", [6, 7, 8] }
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

			case "CommonPotsSavanna":
				modItem.CreateRecipe().AddRecipeGroup("ClayAndMud", 3).AddIngredient(AutoContent.ItemType<SavannaDirt>(), 3).AddTile(wheel).AddCondition(dicovered, function).Register();
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
				2 => DustID.Clay,
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
		int style = GetStyle(t);

		t.TileFrameY = (GetStyle(t) == 0) ? t.TileFrameX : (short)2000; //2000 means no additional gores or effects
		WorldGen.CheckPot(i, j);
		t.TileFrameY = oldFrameY;

		if (style == 1)
		{
			for (int g = 1; g < 5; g++)
			{
				int goreType = Mod.Find<ModGore>("Granite" + g).Type;
				Gore.NewGore(new EntitySource_TileBreak(i, j), new Vector2(i, j) * 16, Vector2.Zero, goreType);
			}
		}
		else if (style == 2)
		{
			for (int g = 1; g < 4; g++)
			{
				int goreType = Mod.Find<ModGore>("Savanna" + g).Type;
				Gore.NewGore(new EntitySource_TileBreak(i, j), new Vector2(i, j) * 16, Vector2.Zero, goreType);
			}
		}
	}

	public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
	{
		if (GetStyle(Main.tile[i, j]) == 0)
			Lighting.AddLight(new Vector2(i, j).ToWorldCoordinates(), Color.Blue.ToVector3() * 0.8f);

		return true;
	}

	public void AddLoot(int objectStyle, ILoot loot)
	{
		ModContent.GetInstance<Pots>().AddLoot(0, loot);

		if (objectStyle / 3 == 2) //Savanna
		{
			foreach (IItemDropRule item in loot.Get())
			{
				if (item is OneFromRulesRule chain)
				{
					foreach (var c in chain.options)
					{
						if (c is CommonDrop drop && drop.itemId == ItemID.Torch)
							drop.itemId = ModContent.ItemType<SavannaTorchItem>(); //Replace the default torch
					}
				}
			}
		}
	}
}