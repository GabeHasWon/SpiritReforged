using SpiritReforged.Common.Misc;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.TileCommon.PresetTiles;
using SpiritReforged.Common.WorldGeneration;
using SpiritReforged.Content.Underground.Pottery;
using SpiritReforged.Content.Underground.Tiles;
using Terraria.Audio;
using Terraria.DataStructures;

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
					pool[NPCID.FairyCritterBlue] = 20.005f;
			}
		}
	}

	public override Dictionary<string, int[]> TileStyles => new() { { string.Empty, [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11] } };

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
		if (TileLootSystem.TryGetLootPool(ModContent.TileType<Pots>(), out var dele))
			dele.Invoke(loot);
	}
}