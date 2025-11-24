using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.TileCommon.PresetTiles;
using SpiritReforged.Common.WorldGeneration;
using SpiritReforged.Content.Underground.Pottery;
using SpiritReforged.Content.Underground.Tiles;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.ItemDropRules;

namespace SpiritReforged.Content.SaltFlats.Tiles;

public class StoneStupas : PotTile, ILootable
{
	private class StoneStupaFairySpawnNPC : GlobalNPC
	{
		public override void EditSpawnPool(IDictionary<int, float> pool, NPCSpawnInfo spawnInfo)
		{
			bool hasStupa = false;
			
			for (int i = spawnInfo.SpawnTileX - 2; i < spawnInfo.SpawnTileX + 2; ++i)
			{
				for (int j = spawnInfo.SpawnTileY - 2; j < spawnInfo.SpawnTileY + 2; ++j)
				{
					Tile tile = Main.tile[i, j];

					if (tile.TileType == ModContent.TileType<StoneStupas>() && tile.HasTile)
					{
						hasStupa = true;
						break;
					}
				}

				if (hasStupa) 
					break;
			}

			if (hasStupa)
			{
				int fairyCount = 0;

				foreach (NPC npc in Main.ActiveNPCs)
				{
					if (npc.type is NPCID.FairyCritterBlue or NPCID.FairyCritterGreen or NPCID.FairyCritterPink)
						fairyCount++;
				}

				if (fairyCount < 4)
					pool[NPCID.FairyCritterBlue] = 0.05f;
			}
		}
	}

	public override Dictionary<string, int[]> TileStyles => new() { { string.Empty, [0, 1, 2] } };

	public static readonly SoundStyle Break = new("SpiritReforged/Assets/SFX/Tile/StoneStupaShatter", 2)
	{
		PitchVariance = 0.2f
	};

	public override void AddItemRecipes(ModItem modItem, NamedStyles.StyleGroup group, Condition condition) => modItem.CreateRecipe()
		.AddRecipeGroup("ClayAndMud", 3).AddIngredient(ItemID.StoneBlock, 3).AddTile(ModContent.TileType<PotteryWheel>()).AddCondition(condition).Register();

	public override void AddObjectData()
	{
		DustType = IsRubble ? -1 : DustID.Stone;
		HitSound = IsRubble ? SoundID.Dig : Break;
		
		base.AddObjectData();
	}

	public override void KillMultiTile(int i, int j, int frameX, int frameY)
	{
		if (IsRubble || WorldMethods.Generating)
			return;

		if (Main.netMode != NetmodeID.MultiplayerClient)
		{
			Pots.SpawnThingsFromPot(default, i, j, i, j, 0);
			TileLootSystem.Resolve(i, j, Type, frameX, frameY);
		}

		if (!Main.dedServ)
			DeathEffects(i, j, frameX, frameY);
	}

	public override void DeathEffects(int i, int j, int frameX, int frameY)
	{
		for (int g = 1; g < 7; g++)
		{
			int goreType = Mod.Find<ModGore>("Stupa" + g).Type;
			var position = Main.rand.NextVector2FromRectangle(new(i * 16, j * 16, 32, 32));

			Gore.NewGore(new EntitySource_TileBreak(i, j), position, Vector2.Zero, goreType);
		}
	}

	public void AddLoot(ILoot loot)
	{
		List<IItemDropRule> branch = [];

		List<int> potions = [ItemID.IronskinPotion, ItemID.ShinePotion, ItemID.NightOwlPotion, ItemID.SwiftnessPotion,
			ItemID.MiningPotion, ItemID.CalmingPotion, ItemID.BuilderPotion, ItemID.RecallPotion, ItemID.ArcheryPotion,
			ItemID.GillsPotion, ItemID.HunterPotion, ItemID.TrapsightPotion, ItemID.FeatherfallPotion, ItemID.WaterWalkingPotion,
			ItemID.GravitationPotion, ItemID.InvisibilityPotion, ItemID.ThornsPotion, ItemID.HeartreachPotion, ItemID.FlipperPotion,
			ItemID.ManaRegenerationPotion, ItemID.ObsidianSkinPotion, ItemID.MagicPowerPotion, ItemID.BattlePotion, ItemID.TitanPotion];

		branch.Add(ItemDropRule.OneFromOptions(15, [.. potions]));
		branch.Add(ItemDropRule.ByCondition(new DropConditions.Standard(Condition.Multiplayer), ItemID.WormholePotion, 30));
		branch.Add(ItemDropRule.Common(ModContent.ItemType<SaltFlatsTorchItem>(), 3, 5, 15));

		if (Main.hardMode)
			branch.Add(ItemDropRule.OneFromOptions(4, ItemID.UnholyArrow, ItemID.Grenade, (WorldGen.SavedOreTiers.Silver == TileID.Silver) ? ItemID.SilverBullet : ItemID.TungstenBullet));
		else
			branch.Add(DropRules.LootPoolDrop.SameStack(10, 20, 1, 8, 3, ItemID.WoodenArrow, ItemID.Shuriken));

		branch.Add(ItemDropRule.Common(Main.hardMode ? ItemID.HealingPotion : ItemID.LesserHealingPotion));
		branch.Add(ItemDropRule.Common(ItemID.Bomb, 8, 1, 4));

		if (!Main.hardMode)
			branch.Add(ItemDropRule.Common(ItemID.Rope, 4, 20, 40));

		// Always least one of any item, but sometimes more
		loot.Add(new AlwaysAtleastOneSuccessDropRule([.. branch]));
	}
}