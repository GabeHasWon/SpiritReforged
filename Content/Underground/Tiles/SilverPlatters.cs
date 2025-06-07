using RubbleAutoloader;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.TileCommon.PresetTiles;
using SpiritReforged.Content.Particles;
using SpiritReforged.Content.Underground.Pottery;
using SpiritReforged.Content.Vanilla.Food;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.ItemDropRules;
using static SpiritReforged.Common.TileCommon.StyleDatabase;
using static SpiritReforged.Common.WorldGeneration.WorldMethods;

namespace SpiritReforged.Content.Underground.Tiles;

public class SilverPlatters : PotTile, ILootTile
{
	public override Dictionary<string, int[]> TileStyles => new() { { string.Empty, [0, 1, 2] } };
	public override void AddRecord(int type, StyleGroup group)
	{
		var desc = Language.GetText("Mods.SpiritReforged.Tiles.Records.Platter");
		RecordHandler.Records.Add(new TileRecord(group.name, type, group.styles).AddDescription(desc).AddRating(3));
	}

	public override void AddItemRecipes(ModItem modItem, StyleGroup group)
	{
		LocalizedText dicovered = AutoloadedPotItem.Discovered;
		var function = (modItem as AutoloadedPotItem).RecordedPot;

		modItem.CreateRecipe().AddRecipeGroup("ClayAndMud", 3).AddRecipeGroup("SilverBars", 2).AddTile(ModContent.TileType<PotteryWheel>()).AddCondition(dicovered, function).Register();
	}

	public override void AddObjectData()
	{
		Main.tileOreFinderPriority[Type] = 575;
		base.AddObjectData();
	}

	public override void AddMapData() => AddMapEntry(Color.Silver, Language.GetText("Mods.SpiritReforged.Items.SilverPlattersItem.DisplayName"));

	public override void NearbyEffects(int i, int j, bool closer)
	{
		const int distance = 200;

		if (!closer || Main.gamePaused || !TileObjectData.IsTopLeft(i, j) || Autoloader.IsRubble(Type))
			return;

		var world = new Vector2(i, j) * 16;
		float strength = Main.LocalPlayer.DistanceSQ(world) / (distance * distance);

		if (strength < 1)
		{
			var spawn = Main.rand.NextVector2FromRectangle(new Rectangle(i * 16, (j + 2) * 16, 32, 2));
			float scale = Main.rand.NextFloat(2f, 4f);
			var velocity = (Vector2.UnitY * -1.5f).RotatedBy(Math.Sin(Main.timeForVisualEffects / 20f) / 3);

			ParticleHandler.SpawnParticle(new SteamParticle(spawn, velocity, scale, 40) { Color = Color.White * (1f - strength) * .15f });
		}
	}

	public override void KillMultiTile(int i, int j, int frameX, int frameY)
	{
		if (Autoloader.IsRubble(Type) || Generating)
			return;

		var center = new Vector2(i, j).ToWorldCoordinates(16, 16);
		var t = Main.tile[i, j];
		WorldGen.PlaceTile(i, j + 1, ModContent.TileType<SilverFoodPlatter>(), true, style: frameX / 36);

		TileEntity.PlaceEntityNet(i, j + 1, ModContent.TileEntityType<PlatterSlot>());

		if (Main.netMode != NetmodeID.MultiplayerClient)
		{
			for (int x = 0; x < 2; x++) //Roll twice
				LootTable.Resolve(i, j, Type, frameX, frameY);
		}

		if (!Main.dedServ)
		{
			var source = new EntitySource_TileBreak(i, j);
			Gore.NewGore(source, new Vector2(i, j) * 16, Vector2.UnitY * -2f, Mod.Find<ModGore>("Platter" + (frameX / 36 + 1)).Type);

			for (int x = 0; x < 15; x++)
			{
				var spawn = Main.rand.NextVector2FromRectangle(new Rectangle(i * 16, (j + 2) * 16, 32, 2));
				ParticleHandler.SpawnParticle(new SteamParticle(spawn, Vector2.UnitY * -Main.rand.NextFloat(), Main.rand.NextFloat(2f, 3f), 40) { Color = Color.White * .5f });

				var d = Dust.NewDustDirect(new Vector2(i, j + 1) * 16, 32, 16, DustID.TreasureSparkle, Scale: Main.rand.NextFloat(.5f, 1f));
				d.velocity = Vector2.UnitY * -Main.rand.NextFloat();
			}

			SoundEngine.PlaySound(SoundID.DrumCymbal1 with { Volume = .5f, PitchRange = (-.4f, 0), }, new Vector2(i, j).ToWorldCoordinates(16, 16));
		}
	}

	public void AddLoot(int objectStyle, ILoot loot)
	{
		loot.AddOneFromOptions(1, ItemID.RoastedBird, ItemID.BunnyStew, ItemID.CookedFish, ItemID.GrilledSquirrel, ItemID.SauteedFrogLegs,
			ModContent.ItemType<CookedMeat>(), ModContent.ItemType<FishChips>(), ModContent.ItemType<HoneySalmon>());

		var rule = ItemDropRule.OneFromOptions(2, ItemID.Burger, ItemID.Pizza, ItemID.Hotdog, ItemID.Steak, ItemID.BBQRibs, ItemID.Bacon);
		rule.OnFailedRoll(ItemDropRule.OneFromOptions(1, ItemID.MonsterLasagna, ItemID.LobsterTail, ItemID.Sashimi, ItemID.CookedShrimp, ItemID.Escargot,
			ItemID.RoastedDuck, ItemID.ChickenNugget, ItemID.SeafoodDinner, ItemID.GrubSoup, ItemID.ShrimpPoBoy, ItemID.Pho, ItemID.FroggleBunwich));

		loot.Add(rule);
	}
}