using RubbleAutoloader;
using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.TileCommon.PresetTiles;
using SpiritReforged.Common.Visuals.Glowmasks;
using SpiritReforged.Content.Forest.Cloud.Items;
using SpiritReforged.Content.Underground.NPCs;
using SpiritReforged.Content.Underground.Pottery;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.ItemDropRules;

namespace SpiritReforged.Content.Underground.Tiles;

[AutoloadGlowmask("200,200,200")]
public class BiomePots : PotTile, ILootTile
{
	/// <summary> Mirrors <see cref="Styles"/>. </summary>
	public enum Style : int
	{
		Cavern, Ice, Desert, Jungle, Dungeon, Corruption, Crimson, Marble, Hell, Mushroom, Granite
	}

	public static readonly SoundStyle Break = new("SpiritReforged/Assets/SFX/Tile/PotBreak")
	{
		Volume = .16f,
		PitchRange = (-.4f, 0)
	};

	public static readonly SoundStyle JungleBreak = new("SpiritReforged/Assets/SFX/NPCHit/HardNaturalHit")
	{
		Volume = .5f,
		PitchRange = (0f, .3f)
	};

	public static readonly SoundStyle Squish = new("SpiritReforged/Assets/SFX/NPCDeath/Squish")
	{
		Volume = .25f
	};

	public override void AddRecord(int type, StyleDatabase.StyleGroup group)
	{
		var record = new TileRecord(group.name, type, group.styles).AddRating(2).AddDescription(Language.GetText(TileRecord.DescKey + ".Biome"));

		if (group.name == "BiomePotsCrimson")
		{
			record.Hide(() => !WorldGen.crimson); //Conditionally hide some entries
		}
		else if (group.name == "BiomePotsCorruption")
		{
			record.Hide(() => WorldGen.crimson);
		}

		RecordHandler.Records.Add(record);
	}

	public override Dictionary<string, int[]> TileStyles => new()
	{
		{ "Cavern", [0, 1, 2] },
		{ "Ice", [3, 4, 5] },
		{ "Desert", [6, 7, 8] },
		{ "Jungle", [9, 10, 11] },
		{ "Dungeon", [12, 13, 14] },
		{ "Corruption", [15, 16, 17] },
		{ "Crimson", [18, 19, 20] },
		{ "Marble", [21, 22, 23] },
		{ "Hell", [24, 25, 26] },
		{ "Mushroom", [27, 28, 29] },
        { "Granite", [30, 31, 32] }
    };

	/// <summary> Gets the <see cref="Style"/> associated with the given frame. </summary>
	private static Style GetStyle(int frameY) => (Style)(frameY / 36);
	/// <summary> Gets the coin multiplier value for this pot. </summary>
	private static float GetValue(Style style) => style switch
	{
		Style.Ice => 1.4f,
		Style.Desert => 2.25f,
		Style.Jungle => 2f,
		Style.Dungeon => 2.25f,
		Style.Corruption => 1.9f,
		Style.Crimson => 1.9f,
		Style.Marble => 2.2f,
		Style.Hell => 2.5f,
		_ => 1.25f
	};

	public override void AddItemRecipes(ModItem modItem, StyleDatabase.StyleGroup group)
	{
		int wheel = ModContent.TileType<PotteryWheel>();
		LocalizedText dicovered = AutoloadedPotItem.Discovered;
		var function = (modItem as AutoloadedPotItem).RecordedPot;

		switch (group.name)
		{
			case "BiomePotsCavern":
				modItem.CreateRecipe().AddRecipeGroup("ClayAndMud", 3).AddTile(wheel).AddCondition(dicovered, function).Register();
				break;

			case "BiomePotsIce":
				modItem.CreateRecipe().AddRecipeGroup("ClayAndMud", 3).AddIngredient(ItemID.IceBlock, 3).AddTile(wheel).AddCondition(dicovered, function).Register();
				break;

			case "BiomePotsJungle":
				modItem.CreateRecipe().AddRecipeGroup("ClayAndMud", 3).AddIngredient(ItemID.RichMahogany, 3).AddTile(wheel).AddCondition(dicovered, function).Register();
				break;

			case "BiomePotsDungeon":
				modItem.CreateRecipe().AddRecipeGroup("ClayAndMud", 3).AddIngredient(ItemID.Bone, 3).AddTile(wheel).AddCondition(dicovered, function).Register();
				break;

			case "BiomePotsHell":
				modItem.CreateRecipe().AddRecipeGroup("ClayAndMud", 3).AddIngredient(ItemID.Obsidian, 2).AddTile(wheel).AddCondition(dicovered, function).Register();
				break;

			case "BiomePotsCorruption":
				modItem.CreateRecipe().AddRecipeGroup("ClayAndMud", 3).AddIngredient(ItemID.RottenChunk).AddTile(wheel).AddCondition(dicovered, function).Register();
				break;

			case "BiomePotsSpider":
				modItem.CreateRecipe().AddRecipeGroup("ClayAndMud", 3).AddIngredient(ItemID.Cobweb, 3).AddTile(wheel).AddCondition(dicovered, function).Register();
				break;

			case "BiomePotsCrimson":
				modItem.CreateRecipe().AddRecipeGroup("ClayAndMud", 3).AddIngredient(ItemID.Vertebrae).AddTile(wheel).AddCondition(dicovered, function).Register();
				break;

			case "BiomePotsMarble":
				modItem.CreateRecipe().AddRecipeGroup("ClayAndMud", 3).AddIngredient(ItemID.Marble, 3).AddTile(wheel).AddCondition(dicovered, function).Register();
				break;

			case "BiomePotsDesert":
				modItem.CreateRecipe().AddRecipeGroup("ClayAndMud", 3).AddIngredient(ItemID.Sandstone, 2).AddTile(wheel).AddCondition(dicovered, function).Register();
				break;

			case "BiomePotsMushroom":
				modItem.CreateRecipe().AddRecipeGroup("ClayAndMud", 3).AddIngredient(ItemID.GlowingMushroom).AddTile(wheel).AddCondition(dicovered, function).Register();
				break;

			case "BiomePotsGranite":
				modItem.CreateRecipe().AddRecipeGroup("ClayAndMud", 3).AddIngredient(ItemID.Granite, 3).AddTile(wheel).AddCondition(dicovered, function).Register();
				break;
		}
	}

	public override void AddMapData() => AddMapEntry(new Color(112, 60, 70), Language.GetText("MapObject.Pot"));

	public override void NearbyEffects(int i, int j, bool closer)
	{
		const int distance = 200;

		if (!closer || Autoloader.IsRubble(Type))
			return;

		var world = new Vector2(i, j) * 16;
		float strength = Main.LocalPlayer.DistanceSQ(world) / (distance * distance);

		if (strength < 1 && Main.rand.NextFloat(35f) < 1f - strength)
		{
			var d = Dust.NewDustDirect(world, 16, 16, DustID.TreasureSparkle, 0, 0, Scale: Main.rand.NextFloat());
			d.noGravity = true;
			d.velocity = new Vector2(0, -Main.rand.NextFloat(2f));
			d.fadeIn = 1f;
		}
	}

	public override bool KillSound(int i, int j, bool fail)
	{
		if (!fail && !Autoloader.IsRubble(Type))
		{
			var pos = new Vector2(i, j).ToWorldCoordinates(16, 16);

			if (GetStyle(Main.tile[i, j].TileFrameY) is Style.Mushroom or Style.Corruption or Style.Crimson)
			{
				SoundEngine.PlaySound(SoundID.NPCHit1 with { Volume = .3f, Pitch = .25f }, pos);
				SoundEngine.PlaySound(SoundID.NPCDeath1, pos);
			}
			else if (GetStyle(Main.tile[i, j].TileFrameY) is Style.Jungle)
			{
				SoundEngine.PlaySound(Squish, pos);
				SoundEngine.PlaySound(JungleBreak, pos);
				SoundEngine.PlaySound(SoundID.Dig, pos);
			}
			else
			{
				SoundEngine.PlaySound(SoundID.Shatter, pos);
				SoundEngine.PlaySound(Break, pos);
			}

			return false;
		}

		return true;
	}

	public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
	{
		var style = GetStyle(Main.tile[i, j].TileFrameY);

		if (style is Style.Mushroom)
			Lighting.AddLight(new Vector2(i, j).ToWorldCoordinates(), Color.Blue.ToVector3());

		return true;
	}

	public override void KillMultiTile(int i, int j, int frameX, int frameY)
	{
		if (WorldGen.generatingWorld || Autoloader.IsRubble(Type))
			return; //Particularly important for not incrementing Remaining

		var style = GetStyle(frameY);
		int variant = frameX / 36;

		var source = new EntitySource_TileBreak(i, j);
		var center = new Vector2(i, j).ToWorldCoordinates(16, 16);
		int dustType = DustID.Pot;

		bool spawnSlime = PotteryTracker.Remaining == 1;
		PotteryTracker.TrackOne();

		if (Main.netMode != NetmodeID.MultiplayerClient)
		{
			#region loot
			var p = Main.player[Player.FindClosest(center, 0, 0)];
			LootTable.Resolve(i, j, Type, frameX, frameY);

			ItemMethods.SplitCoins((int)(CalculateCoinValue() * GetValue(style)), delegate (int type, int stack)
			{
				Item.NewItem(source, center, new Item(type, stack), noGrabDelay: true);
			}); //Always drop coins

			if (p.statLife < p.statLifeMax2)
			{
				int stack = Main.rand.Next(3, 6);

				for (int h = 0; h < stack; h++)
					Item.NewItem(source, center, ItemID.Heart);
			}

			if (Main.rand.NextBool(100))
				Projectile.NewProjectile(source, center, Vector2.UnitY * -4f, ProjectileID.CoinPortal, 0, 0);
			#endregion

			if (spawnSlime)
				NPC.NewNPCDirect(source, center, ModContent.NPCType<PotterySlime>());

			if (style is Style.Mushroom && NPC.CountNPCS(ModContent.NPCType<StompableGnome>()) < 5)
			{
				int count = Main.rand.Next(1, 4);

				for (int c = 0; c < count; c++)
					NPC.NewNPCDirect(source, center + Main.rand.NextVector2Unit() * Main.rand.NextFloat(10f), ModContent.NPCType<StompableGnome>());
			}

			if (Main.dedServ)
				return;
		}

		switch (style)
		{
			case Style.Cavern:

				for (int g = 0; g < 3; g++)
				{
					int goreType = Mod.Find<ModGore>("PotCavern" + (g + variant * 3 + 1)).Type;
					Gore.NewGore(source, center, Vector2.Zero, goreType);
				}

				dustType = DustID.Pot;

				break;

			case Style.Ice:

				for (int g = 1; g < 4; g++)
				{
					int goreType = Mod.Find<ModGore>("PotIce" + g).Type;
					Gore.NewGore(source, center, Vector2.Zero, goreType);
				}

				dustType = DustID.Ice;

				break;

			case Style.Desert:

				Gore.NewGore(source, GetRandom(), Vector2.Zero, GoreID.DesertPot1);
				Gore.NewGore(source, GetRandom(), Vector2.Zero, GoreID.DesertPot2);
				Gore.NewGore(source, GetRandom(), Vector2.Zero, GoreID.DesertPot3);
				dustType = DustID.DesertPot;

				break;

			case Style.Jungle:

				Gore.NewGore(source, GetRandom(), Vector2.Zero, 199);
				Gore.NewGore(source, GetRandom(), Vector2.Zero, 200);
				dustType = DustID.WoodFurniture;

				break;

			case Style.Dungeon:

				Gore.NewGore(source, GetRandom(), Vector2.Zero, 201);
				Gore.NewGore(source, GetRandom(), Vector2.Zero, 202);
				dustType = DustID.Bone;

				break;

			case Style.Corruption:

				dustType = DustID.CorruptGibs;
				Gore.NewGore(source, center, Vector2.Zero, Mod.Find<ModGore>("PotCorrupt1").Type);

				break;

			case Style.Crimson:

				dustType = DustID.Crimson;
				Gore.NewGore(source, center, Vector2.Zero, Mod.Find<ModGore>("PotCrimson1").Type);

				break;

			case Style.Marble:

				Gore.NewGore(source, GetRandom(), Vector2.Zero, GoreID.GreekPot1);
				Gore.NewGore(source, GetRandom(), Vector2.Zero, GoreID.GreekPot2);
				Gore.NewGore(source, GetRandom(), Vector2.Zero, GoreID.GreekPot3);
				dustType = DustID.MarblePot;

				break;

			case Style.Hell:

				Gore.NewGore(source, GetRandom(), Vector2.Zero, 203);
				Gore.NewGore(source, GetRandom(), Vector2.Zero, 204);
				dustType = DustID.Obsidian;

				break;

			case Style.Mushroom:

				for (int g = 1; g < 4; g++)
				{
					int goreType = Mod.Find<ModGore>("PotMushroom" + g).Type;
					Gore.NewGore(source, center, Vector2.Zero, goreType);
				}

				dustType = DustID.MushroomSpray;

				break;

			case Style.Granite:

				for (int g = 1; g < 4; g++)
				{
					int goreType = Mod.Find<ModGore>("PotGranite" + g).Type;
					Gore.NewGore(source, center, Vector2.Zero, goreType);
				}

				dustType = DustID.Granite;

				break;
		}

		for (int d = 0; d < 20; d++)
			Dust.NewDustPerfect(GetRandom(), dustType, Main.rand.NextVector2Unit(), Scale: Main.rand.NextFloat() + .25f);

		Vector2 GetRandom(float distance = 15f) => center + Main.rand.NextVector2Unit() * Main.rand.NextFloat(distance);
	}

	public void AddLoot(int objectStyle, ILoot loot)
	{
		var style = GetStyle(objectStyle / 3 * 36);

		if (style is Style.Dungeon)
			loot.AddCommon(ItemID.GoldenKey, 10);

		List<int> potions = [ItemID.SpelunkerPotion, ItemID.HunterPotion,
			ItemID.GravitationPotion, ItemID.LifeforcePotion, ItemID.TitanPotion, ItemID.BattlePotion,
			ItemID.MagicPowerPotion, ItemID.ManaRegenerationPotion, ItemID.BiomeSightPotion, ItemID.HeartreachPotion,
			ModContent.ItemType<DoubleJumpPotion>(), WorldGen.crimson ? ItemID.RagePotion : ItemID.WrathPotion];

		if (style is Style.Cavern)
			potions.AddRange([ItemID.SwiftnessPotion, ItemID.RegenerationPotion, ItemID.SwiftnessPotion]);
		else if (style is Style.Jungle)
			potions.Add(ItemID.SummoningPotion);
		else if (style is Style.Hell)
			potions.AddRange([ItemID.InfernoPotion, ItemID.ObsidianSkinPotion]);

		var pCond0 = ItemDropRule.OneFromOptions(15, [.. potions]);
		var pCond1 = ItemDropRule.OneFromOptions(3, ItemID.PotionOfReturn, ItemID.LuckPotionLesser);

		pCond0.OnSuccess(pCond1);
		pCond1.OnFailedRoll(ItemDropRule.Common(ItemID.LuckPotion, 5));

		loot.Add(pCond0);

		List<int> flasks = [ItemID.FlaskofGold];

		if (style is Style.Jungle)
			flasks.Add(ItemID.FlaskofPoison);
		else if (style is Style.Hell)
			flasks.Add(ItemID.FlaskofFire);

		loot.AddOneFromOptions(32, [.. flasks]);
		loot.Add(ItemDropRule.ByCondition(new DropConditions.Standard(Condition.Multiplayer), ItemID.WormholePotion, 30));

		int type = style switch
		{
			Style.Desert => ItemID.FossilOre,
			Style.Dungeon => ItemID.Bone,
			Style.Marble => ItemID.Javelin,
			Style.Hell => ItemID.LivingFireBlock,
			Style.Granite => ItemID.Geode,
			_ => -1
		};

		if (type != -1)
		{
			if (style is Style.Dungeon)
				loot.Add(ItemDropRule.ByCondition(new DropConditions.Standard(Condition.DownedSkeletron), ItemID.Bone, 2, 10, 15));
			if (style is Style.Granite)
				loot.AddCommon(ItemID.Geode, 3);
			else
				loot.AddCommon(type, 2, 10, 15);
		}

		List<IItemDropRule> branch = [];

		branch.Add(ItemDropRule.Common((style is Style.Desert) ? ItemID.ScarabBomb : ItemID.Dynamite, 1, 4, 8));
		branch.Add(ItemDropRule.Common(TorchType(), 1, 15, 20));
		branch.Add(ItemDropRule.Common(ArrowType(), 1, 20, 40));
		branch.Add(ItemDropRule.Common((WorldGen.SavedOreTiers.Silver == TileID.Silver) ? ItemID.SilverBullet : ItemID.TungstenBullet, 1, 20, 40));
		branch.Add(ItemDropRule.Common(ItemID.HealingPotion, 1, 1, 3));

		loot.Add(new OneFromRulesRule(1, [.. branch]));

		int ArrowType()
		{
			int result = style switch
			{
				Style.Ice => ItemID.FrostburnArrow,
				Style.Dungeon => ItemID.BoneArrow,
				Style.Marble => ItemID.JestersArrow,
				Style.Hell => ItemID.HellfireArrow,
				_ => ItemID.WoodenArrow
			};

			if (style is Style.Desert or Style.Jungle)
				result = ItemID.FlamingArrow;
			else if (style is Style.Corruption or Style.Crimson)
				result = ItemID.UnholyArrow;

			return result;
		}

		int TorchType()
		{
			int result = style switch
			{
				Style.Ice => ItemID.IceTorch,
				Style.Desert => ItemID.DesertTorch,
				Style.Jungle => ItemID.JungleTorch,
				Style.Dungeon => ItemID.BoneTorch,
				Style.Marble => ItemID.YellowTorch,
				Style.Hell => ItemID.DemonTorch,
				Style.Mushroom => ItemID.MushroomTorch,
				_ => ItemID.UltrabrightTorch
			};

			return result;
		}
	}
}