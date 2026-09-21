using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.ModCompat;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.TileCommon.PresetTiles;
using SpiritReforged.Common.UI.PotCatalogue;
using SpiritReforged.Content.Forest.Cloud.Items;
using SpiritReforged.Content.Underground.Items;
using SpiritReforged.Content.Underground.NPCs;
using SpiritReforged.Content.Underground.Pottery;
using SpiritReforged.Content.Ziggurat.Tiles;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.ItemDropRules;

namespace SpiritReforged.Content.Crossmod.Spooky.Tiles.Pots;

public class UncommonSpookyPots : PotTile, ILootable
{
	public enum Style : int
	{
		LowerCatacombs, UpperCatacombs, Krampus, TarPits, RottenDepths, FetidFarms, NoseTemple, SpiderGrotto, SpookyForest
	}

	public static readonly SoundStyle Break = new("SpiritReforged/Assets/SFX/Tile/PotBreak")
	{
		Volume = 0.16f,
		PitchRange = (-0.4f, 0)
	};

	public static readonly SoundStyle JungleBreak = new("SpiritReforged/Assets/SFX/NPCHit/HardNaturalHit")
	{
		Volume = 0.5f,
		PitchRange = (0f, 0.3f)
	};

	public static readonly SoundStyle Squish = new("SpiritReforged/Assets/SFX/NPCDeath/Squish")
	{
		Volume = 0.25f
	};

	public override Dictionary<string, int[]> TileStyles
	{
		get
		{
			Dictionary<string, int[]> dict = [];
			int start = 0;

			foreach (string name in Enum.GetNames<Style>())
				dict.Add(name, [start++, start++, start++]);

			return dict;
		}
	}

	public override void AutoloadFromGroup()
	{
		foreach (string name in Styles.Keys)
		{
			string finalName = Name + name;
			NamedStyles.StyleGroup group = new(finalName, Styles[name]);

			NamedStyles.AddStyle(Type, group);

			TileRecord record = AddRecord(Type, group);
			record.AddDisplayName(Language.GetText("Mods.SpiritReforged.Tiles.Records." + name + ".Name"));
			record.AddDescription(Language.GetText("Mods.SpiritReforged.Tiles.Records." + name + ".Entry"));
			RecordHandler.Records.Add(record);

			Mod.AddContent(new AutoloadedPotItem(Name + "Rubble", group, record.Condition, AddItemRecipes));
		}
	}

	public override bool IsLoadingEnabled(Mod mod) => CrossMod.Spooky.Enabled;

	public override TileRecord AddRecord(int type, NamedStyles.StyleGroup group)
	{
		var record = new TileRecord(group.name, type, group.styles).AddRating(2).AddDescription(Language.GetText(TileRecord.DescKey + ".Biome"));
		return record;
	}

	/// <summary> Gets the <see cref="Style"/> associated with the given frame. </summary>
	private static Style GetStyle(int frameY) => (Style)(frameY / 36);

	/// <summary> Gets the coin multiplier value for this pot. </summary>
	private static float GetValue(Style style) => style switch
	{
		Style.SpookyForest => 1.35f,
		Style.Krampus or Style.TarPits => 1.5f,
		Style.FetidFarms or Style.RottenDepths or Style.SpiderGrotto or Style.UpperCatacombs => 2,
		Style.NoseTemple => 2.25f,
		Style.LowerCatacombs => 3,
		_ => 1.25f
	};

	public override void AddItemRecipes(ModItem modItem, NamedStyles.StyleGroup group, Condition condition)
	{
		int type = ModContent.TileType<PotteryWheel>();
		switch (group.name)
		{
			case "BiomePotsCavern":
				modItem.CreateRecipe().AddRecipeGroup("ClayAndMud", 3).AddTile(type).AddCondition(condition).Register();
				break;

			case "BiomePotsIce":
				modItem.CreateRecipe().AddRecipeGroup("ClayAndMud", 3).AddIngredient(ItemID.IceBlock, 3).AddTile(type).AddCondition(condition).Register();
				break;

			case "BiomePotsJungle":
				modItem.CreateRecipe().AddRecipeGroup("ClayAndMud", 3).AddIngredient(ItemID.RichMahogany, 3).AddTile(type).AddCondition(condition).Register();
				break;

			case "BiomePotsDungeon":
				modItem.CreateRecipe().AddRecipeGroup("ClayAndMud", 3).AddIngredient(ItemID.Bone, 3).AddTile(type).AddCondition(condition).Register();
				break;

			case "BiomePotsHell":
				modItem.CreateRecipe().AddRecipeGroup("ClayAndMud", 3).AddIngredient(ItemID.Obsidian, 2).AddTile(type).AddCondition(condition).Register();
				break;

			case "BiomePotsCorruption":
				modItem.CreateRecipe().AddRecipeGroup("ClayAndMud", 3).AddIngredient(ItemID.RottenChunk).AddTile(type).AddCondition(condition).Register();
				break;

			case "BiomePotsSpider":
				modItem.CreateRecipe().AddRecipeGroup("ClayAndMud", 3).AddIngredient(ItemID.Cobweb, 3).AddTile(type).AddCondition(condition).Register();
				break;

			case "BiomePotsCrimson":
				modItem.CreateRecipe().AddRecipeGroup("ClayAndMud", 3).AddIngredient(ItemID.Vertebrae).AddTile(type).AddCondition(condition).Register();
				break;

			case "BiomePotsMarble":
				modItem.CreateRecipe().AddRecipeGroup("ClayAndMud", 3).AddIngredient(ItemID.Marble, 3).AddTile(type).AddCondition(condition).Register();
				break;

			case "BiomePotsDesert":
				modItem.CreateRecipe().AddRecipeGroup("ClayAndMud", 3).AddIngredient(ItemID.Sandstone, 2).AddTile(type).AddCondition(condition).Register();
				break;

			case "BiomePotsMushroom":
				modItem.CreateRecipe().AddRecipeGroup("ClayAndMud", 3).AddIngredient(ItemID.GlowingMushroom).AddTile(type).AddCondition(condition).Register();
				break;

			case "BiomePotsGranite":
				modItem.CreateRecipe().AddRecipeGroup("ClayAndMud", 3).AddIngredient(ItemID.Granite, 3).AddTile(type).AddCondition(condition).Register();
				break;
		}
	}

	public override void AddMapData() => AddMapEntry(new Color(112, 60, 70), Language.GetText("MapObject.Pot"));

	public override void NearbyEffects(int i, int j, bool closer)
	{
		const int distance = 200;

		if (!closer || IsRubble)
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
		if (!fail && !IsRubble)
		{
			var pos = new Vector2(i, j).ToWorldCoordinates(16, 16);
			Style style = GetStyle(Main.tile[i, j].TileFrameY);
			
			if (style is Style.RottenDepths)
			{
				SoundEngine.PlaySound(SoundID.NPCHit1 with { Volume = .3f, Pitch = .25f }, pos);
				SoundEngine.PlaySound(SoundID.NPCDeath1, pos);
			}
			else if (style is Style.NoseTemple)
			{
				SoundEngine.PlaySound(Squish, pos);
				SoundEngine.PlaySound(JungleBreak, pos);
				SoundEngine.PlaySound(SoundID.Dig, pos);
			}
			else if (style == Style.SpookyForest)
				SoundEngine.PlaySound(SoundID.Dig, pos);
			else
			{
				SoundEngine.PlaySound(SoundID.Shatter, pos);
				SoundEngine.PlaySound(Break, pos);
			}

			return false;
		}

		return true;
	}

	public override void KillMultiTile(int i, int j, int frameX, int frameY)
	{
		if (WorldGen.generatingWorld || IsRubble)
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
			TileLootSystem.Resolve(i, j, Type, frameX, frameY);

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

			if (Main.dedServ)
				return;
		}

		Mod spooky = CrossMod.Spooky.Instance;

		switch (style)
		{
			case Style.LowerCatacombs:

				for (int g = 0; g < 3; g++)
				{
					int goreType = Mod.Find<ModGore>("PotCavern" + (g + variant * 3 + 1)).Type;
					Gore.NewGore(source, center, Vector2.Zero, goreType);
				}

				dustType = DustID.Pot;

				break;

			case Style.UpperCatacombs:

				for (int g = 1; g < 4; g++)
				{
					int goreType = Mod.Find<ModGore>("PotIce" + g).Type;
					Gore.NewGore(source, center, Vector2.Zero, goreType);
				}

				dustType = DustID.Ice;

				break;

			case Style.Krampus:

				if (variant == 0)
				{
					dustType = DustID.Confetti_Blue;

					for (int k = 0; k < 3; ++k)
						Gore.NewGore(source, GetRandom(), Vector2.Zero, Mod.Find<ModGore>("BlueGift" + k).Type);
				}
				else if (variant == 1)
				{
					dustType = DustID.Confetti_Green;

					for (int k = 0; k < 3; ++k)
						Gore.NewGore(source, GetRandom(), Vector2.Zero, Mod.Find<ModGore>("GreenGift" + k).Type);
				}
				else
				{
					dustType = DustID.OrangeStainedGlass;

					for (int k = 0; k < 3; ++k)
						Gore.NewGore(source, GetRandom(), Vector2.Zero, Mod.Find<ModGore>("OrangeGift" + k).Type);
				}

				break;

			case Style.TarPits:

				Gore.NewGore(source, GetRandom(), Vector2.Zero, 199);
				Gore.NewGore(source, GetRandom(), Vector2.Zero, 200);
				dustType = DustID.WoodFurniture;

				break;

			case Style.RottenDepths:

				Gore.NewGore(source, GetRandom(), Vector2.Zero, 201);
				Gore.NewGore(source, GetRandom(), Vector2.Zero, 202);
				dustType = DustID.Bone;

				break;

			case Style.FetidFarms:

				dustType = DustID.CorruptGibs;
				Gore.NewGore(source, center, Vector2.Zero, Mod.Find<ModGore>("PotCorrupt1").Type);

				break;

			case Style.NoseTemple:

				for (int k = 0; k < 3; ++k)
					Gore.NewGore(source, GetRandom(), Main.rand.NextVector2Circular(2, 2), Mod.Find<ModGore>("Nose" + Main.rand.Next(4)).Type);

				break;

			case Style.SpiderGrotto:

				if (variant == 0)
				{
					for (int k = 0; k < 2; ++k)
						Gore.NewGore(source, GetCenteredPosition(center), Vector2.Zero, Mod.Find<ModGore>("RedPot" + k).Type);
				}
				else if (variant == 1)
				{
					for (int k = 0; k < 2; ++k)
						Gore.NewGore(source, GetCenteredPosition(center), Vector2.Zero, Mod.Find<ModGore>("GrayPot" + k).Type);
				}
				else
					Gore.NewGore(source, GetCenteredPosition(center), Vector2.Zero, Mod.Find<ModGore>("DarkGrayPot").Type);

				dustType = DustID.Web;
				break;

			case Style.SpookyForest:

				if (variant == 0)
				{
					Gore.NewGore(source, GetCenteredPosition(center), Vector2.Zero, Mod.Find<ModGore>("GreenForest0").Type);

					for (int k = 0; k < 3; ++k)
						Gore.NewGore(source, GetRandom(), Vector2.Zero, Mod.Find<ModGore>("GreenForest" + (k + 1)).Type);
				}
				else if (variant == 1)
				{
					Gore.NewGore(source, GetCenteredPosition(center), Vector2.Zero, Mod.Find<ModGore>("OrangeForest0").Type);

					for (int k = 0; k < 6; ++k)
						Gore.NewGore(source, GetRandom(), Vector2.Zero, Mod.Find<ModGore>("OrangeForest" + (k + 1)).Type);
				}
				else
				{
					for (int k = 0; k < 5; ++k)
						Gore.NewGore(source, GetRandom(), Main.rand.NextVector2Circular(2, 2), GoreID.Smoke1 + Main.rand.Next(3));
				}

				Gore.NewGore(source, GetRandom(), Vector2.Zero, 203);
				Gore.NewGore(source, GetRandom(), Vector2.Zero, 204);
				dustType = DustID.Obsidian;

				break;
		}

		for (int d = 0; d < 20; d++)
			Dust.NewDustPerfect(GetRandom(), dustType, Main.rand.NextVector2Unit(), Scale: Main.rand.NextFloat() + .25f);

		Vector2 GetRandom(float distance = 15f) => center + Main.rand.NextVector2Unit() * Main.rand.NextFloat(distance);
	}

	private static Vector2 GetCenteredPosition(Vector2 center) => center + new Vector2(Main.rand.NextFloat(-24, 0), -12);

	public void AddLoot(ILoot loot)
	{
		var style = GetStyle(loot is TileLootTable t ? t.Style / 3 * 36 : 0);

		if (style == Style.SpookyForest)
			loot.AddOneFromOptions(1, SpookyItem("CandyCorn"), SpookyItem("VampireGummy"), SpookyItem("EyeChocolate"), SpookyItem("FrankenMarshmallow"),
				SpookyItem("GoofyPretzel"), ItemID.ChocolateChipCookie, ItemID.Marshmallow);
		else if (style == Style.FetidFarms)
			loot.AddOneFromOptions(1, ItemID.Apple, ItemID.Apricot, ItemID.BlackCurrant, ItemID.Banana, ItemID.Cherry, ItemID.Lemon, ItemID.Mango, ItemID.Peach, ItemID.Plum,
				ItemID.Rambutan, ItemID.Dragonfruit, ItemID.Grapes);

			List<int> potions = [ItemID.SpelunkerPotion, ItemID.HunterPotion,
			ItemID.GravitationPotion, ItemID.LifeforcePotion, ItemID.TitanPotion, ItemID.BattlePotion,
			ItemID.MagicPowerPotion, ItemID.ManaRegenerationPotion, ItemID.BiomeSightPotion, ItemID.HeartreachPotion,
			ModContent.ItemType<DoubleJumpPotion>(), WorldGen.crimson ? ItemID.RagePotion : ItemID.WrathPotion];

		if (style is Style.FetidFarms)
			potions.Add(ItemID.SummoningPotion);
		else if (style is Style.NoseTemple)
			potions.Add(ItemID.PotionOfReturn);
		else if (style is Style.LowerCatacombs or Style.UpperCatacombs or Style.NoseTemple)
			potions.AddRange([ItemID.PotionOfReturn, ItemID.TrapsightPotion]);

		var pCond0 = ItemDropRule.OneFromOptions(15, [.. potions]);
		var pCond1 = ItemDropRule.OneFromOptions(3, ItemID.PotionOfReturn, ItemID.LuckPotionLesser);

		pCond0.OnSuccess(pCond1);
		pCond1.OnFailedRoll(ItemDropRule.Common(ItemID.LuckPotion, 5));

		loot.Add(pCond0);

		List<int> flasks = [ItemID.FlaskofGold];

		if (style is Style.NoseTemple)
			flasks.Add(ItemID.FlaskofPoison);

		loot.AddOneFromOptions(32, [.. flasks]);
		loot.Add(ItemDropRule.ByCondition(new DropConditions.Standard(Condition.Multiplayer), ItemID.WormholePotion, 30));
		loot.Add(ItemDropRule.NormalvsExpert(ModContent.ItemType<PrefixVoucher>(), 30, 25));

		int type = style switch
		{
			Style.RottenDepths => SpookyItem("FishboneChunk"),
			Style.FetidFarms => SpookyItem("PlantMulch"),
			Style.SpiderGrotto => SpookyItem("SpiderChitin"),
			_ => -1
		};

		if (type != -1)
			loot.AddCommon(type, 2, 10, 15);

		List<IItemDropRule> branch = [];

		branch.Add(ItemDropRule.Common(ItemID.Dynamite, 1, 4, 8));
		branch.Add(ItemDropRule.Common(TorchType(), 1, 15, 20));

		IItemDropRule healingPotRule = ItemDropRule.Common(ItemID.HealingPotion, 1, 1, 3);

		if (style == Style.NoseTemple)
			healingPotRule = ItemDropRule.Common(ItemID.StrangeBrew, 2);

		branch.Add(healingPotRule);

		if (style is Style.SpookyForest or Style.LowerCatacombs or Style.UpperCatacombs or Style.NoseTemple or Style.SpiderGrotto)
			SpookyAmmoBranch(branch);

		loot.Add(new OneFromRulesRule(1, [.. branch]));

		int TorchType()
		{
			int result = style switch
			{
				Style.SpookyForest => SpookyItem("SpookyBiomeTorchItem"),
				Style.UpperCatacombs => SpookyItem("CatacombTorch1Item"),
				Style.LowerCatacombs => SpookyItem("CatacombTorch2Item"),
				Style.NoseTemple => SpookyItem("SpookyHellTorchItem"),
				Style.SpiderGrotto => SpookyItem("SpiderBiomeTorchItem"),
				Style.FetidFarms => ItemID.JungleTorch,
				Style.Krampus => ItemID.IceTorch,
				Style.TarPits => ItemID.DesertTorch,
				Style.RottenDepths => ItemID.CoralTorch,
				_ => ItemID.SpelunkerGlowstick
			};

			return result;
		}
	}

	private static void SpookyAmmoBranch(List<IItemDropRule> branch)
	{
		LeadingConditionRule rule = new(new Conditions.IsHardmode());
		rule.OnSuccess(GetRule("MossyBoulder"));
		rule.OnFailedConditions(GetRule("MossyPebble"));
		branch.Add(new OneFromRulesRule(1, GetRule("OldWoodArrow"), GetRule("RustedBullet"), GetRule("OldWoodArrow"), rule));

		static IItemDropRule GetRule(string name) => ItemDropRule.Common(SpookyItem(name), 1, 15, 30);
	}

	private static int SpookyItem(string name) => CrossMod.Spooky.Find<ModItem>(name).Type;
}