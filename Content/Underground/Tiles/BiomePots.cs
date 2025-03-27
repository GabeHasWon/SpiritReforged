using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Content.Forest.Cloud.Items;
using Terraria.Audio;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Underground.Tiles;

public class BiomePots : ModTile
{
	public enum STYLE : int
	{
		CAVERN, GOLD, ICE, DESERT, JUNGLE, DUNGEON, CORRUPTION, CRIMSON, MARBLE, HELL
	}

	public override void SetStaticDefaults()
	{
		const int row = 3;

		Main.tileSolid[Type] = false;
		Main.tileBlockLight[Type] = false;
		Main.tileCut[Type] = true;
		Main.tileFrameImportant[Type] = true;
		Main.tileSpelunker[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
		TileObjectData.newTile.Origin = new(0, 1);
		TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop, TileObjectData.newTile.Width, 0);
		TileObjectData.newTile.StyleWrapLimit = row;
		TileObjectData.newTile.RandomStyleRange = row;
		TileObjectData.newTile.StyleHorizontal = true;
		TileObjectData.newTile.UsesCustomCanPlace = true;
		TileObjectData.newTile.DrawYOffset = 2;
		TileObjectData.addTile(Type);

		AddMapEntry(new Color(100, 90, 35), Language.GetText("MapObject.Pot"));
		DustType = -1;
	}

	public override bool KillSound(int i, int j, bool fail)
	{
		if (!fail)
		{
			SoundEngine.PlaySound(SoundID.Shatter, new Vector2(i, j).ToWorldCoordinates());
			return false;
		}

		return true;
	}

	public override void KillMultiTile(int i, int j, int frameX, int frameY)
	{
		var style = (STYLE)(frameY / 36);
		int variant = frameX / 36;

		var source = new EntitySource_TileBreak(i, j);
		var position = new Vector2(i, j).ToWorldCoordinates(16, 16);

		int dustType = DustID.Pot;

		if (Main.netMode != NetmodeID.MultiplayerClient)
		{
			HandleLoot(i, j, style); //Drops should only occur on the server/singleplayer

			if (Main.dedServ)
				return;
		}

		switch (style)
		{
			case STYLE.CAVERN:

				for (int g = 0; g < 3; g++)
				{
					int goreType = Mod.Find<ModGore>("PotCavern" + ((g + variant * 3) + 1)).Type;
					Gore.NewGore(source, position, Vector2.Zero, goreType);
				}

				dustType = DustID.Pot;

				break;

			case STYLE.GOLD:

				dustType = DustID.Gold;
				break;

			case STYLE.ICE:

				for (int g = 0; g < 3; g++)
				{
					int goreType = Mod.Find<ModGore>("PotIce" + g).Type;
					Gore.NewGore(source, position, Vector2.Zero, goreType);
				}

				dustType = DustID.Ice;

				break;

			case STYLE.DESERT:

				Gore.NewGore(source, GetRandom(), Vector2.Zero, GoreID.DesertPot1);
				Gore.NewGore(source, GetRandom(), Vector2.Zero, GoreID.DesertPot2);
				Gore.NewGore(source, GetRandom(), Vector2.Zero, GoreID.DesertPot3);
				dustType = DustID.DesertPot;

				break;

			case STYLE.JUNGLE:

				Gore.NewGore(source, GetRandom(), Vector2.Zero, 199);
				Gore.NewGore(source, GetRandom(), Vector2.Zero, 200);
				dustType = DustID.WoodFurniture;

				break;

			case STYLE.DUNGEON:

				Gore.NewGore(source, GetRandom(), Vector2.Zero, 201);
				Gore.NewGore(source, GetRandom(), Vector2.Zero, 202);
				dustType = DustID.Bone;

				break;

			case STYLE.CORRUPTION:

				dustType = DustID.CorruptGibs;

				break;

			case STYLE.CRIMSON:

				dustType = DustID.Crimson;

				break;

			case STYLE.MARBLE:

				Gore.NewGore(source, GetRandom(), Vector2.Zero, GoreID.GreekPot1);
				Gore.NewGore(source, GetRandom(), Vector2.Zero, GoreID.GreekPot2);
				Gore.NewGore(source, GetRandom(), Vector2.Zero, GoreID.GreekPot3);
				dustType = DustID.MarblePot;

				break;

			case STYLE.HELL:

				Gore.NewGore(source, GetRandom(), Vector2.Zero, 203);
				Gore.NewGore(source, GetRandom(), Vector2.Zero, 204);
				dustType = DustID.Obsidian;

				break;
		}

		for (int d = 0; d < 20; d++)
			Dust.NewDustPerfect(GetRandom(), dustType, Main.rand.NextVector2Unit(), Scale: Main.rand.NextFloat() + .25f);

		Vector2 GetRandom(float distance = 15f) => position + Main.rand.NextVector2Unit() * Main.rand.NextFloat(distance);
	}

	private static void HandleLoot(int i, int j, STYLE style)
	{
		var center = new Vector2(i, j).ToWorldCoordinates(16, 16);
		var p = Main.player[Player.FindClosest(center, 0, 0)];

		LootTable table = new();
		if (Player.GetClosestRollLuck(i, j, 100) == 0) //COIN PORTAL
		{
			Projectile.NewProjectileDirect(new EntitySource_TileBreak(i, j), center, Vector2.Zero, ProjectileID.CoinPortal, 0, 0);
			return;
		}

		if (style is STYLE.DUNGEON && WorldGen.genRand.NextBool(10))
			table += LootTable.Create(ItemID.GoldenKey);

		if (WorldGen.genRand.NextBool(15)) //POTIONS
		{
			var potions = new LootTable().AddRange(ItemID.SpelunkerPotion, ItemID.HunterPotion,
			ItemID.GravitationPotion, ItemID.LifeforcePotion, ItemID.TitanPotion, ItemID.BattlePotion,
			ItemID.MagicPowerPotion, ItemID.ManaRegenerationPotion, ItemID.BiomeSightPotion, ItemID.HeartreachPotion,
			ModContent.ItemType<DoubleJumpPotion>(), WorldGen.crimson ? ItemID.RagePotion : ItemID.WrathPotion);

			if (style is STYLE.CAVERN)
				potions.AddRange(ItemID.SwiftnessPotion, ItemID.RegenerationPotion, ItemID.SwiftnessPotion);
			else if (style is STYLE.JUNGLE)
				potions.Add(ItemID.SummoningPotion);
			else if (style is STYLE.HELL)
				potions.AddRange(ItemID.InfernoPotion, ItemID.ObsidianSkinPotion);

			if (WorldGen.genRand.NextBool(3))
				potions += new LootTable().AddRange(ItemID.PotionOfReturn, ItemID.LuckPotionLesser);
			else if (WorldGen.genRand.NextBool(5))
				potions += new LootTable().Add(ItemID.LuckPotion);

			table += potions;
		}
		else if (WorldGen.genRand.NextBool(45)) //FLASKS
		{
			var flasks = new LootTable().Add(ItemID.FlaskofGold);

			if (style is STYLE.JUNGLE)
				flasks.Add(ItemID.FlaskofPoison);
			else if (style is STYLE.HELL)
				flasks.Add(ItemID.FlaskofFire);

			table += flasks;
		}
		else if (WorldGen.genRand.NextBool(30) && Main.dedServ) //WORMHOLE POTION (multiplayer only)
		{
			table += LootTable.Create(ItemID.WormholePotion);
		}
		else
		{
			if (Main.rand.NextBool(4))
			{
				int type = -1;

				if (style == STYLE.DESERT)
					type = ItemID.FossilOre;
				else if (style == STYLE.DUNGEON)
					type = ItemID.Bone;
				else if (style == STYLE.MARBLE)
					type = ItemID.Javelin;
				else if (style == STYLE.HELL)
					type = ItemID.LivingFireBlock;

				if (type != -1)
					table += LootTable.Create(type, Main.rand.Next(10, 16));
			}

			switch (WorldGen.genRand.Next(4))
			{
				case 0:
					table += LootTable.Create(GetPair(style, "arrow"), Main.rand.Next(20, 41));
					break;

				case 1:
					table += LootTable.Create(ItemID.HealingPotion, Main.rand.Next(1, 4));
					break;

				case 2:
					table += LootTable.Create((style == STYLE.DESERT) ? ItemID.ScarabBomb : ItemID.Dynamite, Main.rand.Next(4, 11));
					break;

				case 3:
					table += LootTable.Create((WorldGen.SavedOreTiers.Silver == TileID.Silver) ? ItemID.SilverBullet : ItemID.TungstenBullet, Main.rand.Next(20, 41));
					break;
			}
		}

		if (p.statLife < p.statLifeMax2)
			table += LootTable.Create(ItemID.Heart, Main.rand.Next(3, 6));
		else if (p.CountItem(GetPair(style, "torch"), 20) < 20)
			table += LootTable.Create(GetPair(style, "torch"), Main.rand.Next(15, 21));

		foreach (var item in table.Release()) //Drop all of our rolled items
			Item.NewItem(new EntitySource_TileBreak(i, j), center, item, noGrabDelay: true);

		ItemMethods.SplitCoins(CalculateCoinValue(style), delegate (int type, int stack)
		{
			Item.NewItem(new EntitySource_TileBreak(i, j), center, new Item(type, stack), noGrabDelay: true);
		}); //Always drop coins

		static int GetPair(STYLE style, string context) //Helper for alternatives based on style
		{
			Dictionary<STYLE, int[]> dict = new()
			{
				{ STYLE.CAVERN, [ItemID.UltrabrightTorch, ItemID.WoodenArrow] },
				{ STYLE.ICE, [ItemID.IceTorch, ItemID.FrostburnArrow] },
				{ STYLE.DESERT, [ItemID.DesertTorch, ItemID.FlamingArrow] },
				{ STYLE.JUNGLE, [ItemID.JungleTorch, ItemID.FlamingArrow] },
				{ STYLE.DUNGEON, [ItemID.BoneTorch, ItemID.BoneArrow] },
				{ STYLE.CORRUPTION, [ItemID.CorruptTorch, ItemID.UnholyArrow] },
				{ STYLE.CRIMSON, [ItemID.CrimsonTorch, ItemID.UnholyArrow] },
				{ STYLE.MARBLE, [ItemID.YellowTorch, ItemID.JestersArrow] },
				{ STYLE.HELL, [ItemID.DemonTorch, ItemID.HellfireArrow] }
			};

			int index = (context == "arrow") ? 1 : 0;
			return dict[style][index];
		}
	}

	/// <summary> Calculates coin values similarly to how vanilla pots do. </summary>
	private static int CalculateCoinValue(STYLE style)
	{
		float value = 200 + WorldGen.genRand.Next(-100, 101);

		float biomeMult = style switch
		{
			STYLE.ICE => 1.4f,
			STYLE.DESERT => 2.25f,
			STYLE.JUNGLE => 2f,
			STYLE.DUNGEON => 2.25f,
			STYLE.CORRUPTION => 1.9f,
			STYLE.CRIMSON => 1.9f,
			STYLE.MARBLE => 2.2f,
			STYLE.HELL => 2.5f,
			_ => 1.25f
		};

		value *= biomeMult;
		value *= 1f + Main.rand.Next(-20, 21) * 0.01f;

		if (Main.hardMode)
			value *= 2;

		if (Main.rand.NextBool(4))
			value *= 1f + Main.rand.Next(5, 11) * 0.01f;

		if (Main.rand.NextBool(8))
			value *= 1f + Main.rand.Next(10, 21) * 0.01f;

		if (Main.rand.NextBool(12))
			value *= 1f + Main.rand.Next(20, 41) * 0.01f;

		if (Main.rand.NextBool(16))
			value *= 1f + Main.rand.Next(40, 81) * 0.01f;

		if (Main.rand.NextBool(20))
			value *= 1f + Main.rand.Next(50, 101) * 0.01f;

		if (Main.expertMode)
			value *= 2.5f;

		if (Main.expertMode && Main.rand.NextBool(2))
			value *= 1.25f;

		if (Main.expertMode && Main.rand.NextBool(3))
			value *= 1.5f;

		if (Main.expertMode && Main.rand.NextBool(4))
			value *= 1.75f;

		if (NPC.downedBoss1)
			value *= 1.1f;

		if (NPC.downedBoss2)
			value *= 1.1f;

		if (NPC.downedBoss3)
			value *= 1.1f;

		if (NPC.downedMechBoss1)
			value *= 1.1f;

		if (NPC.downedMechBoss2)
			value *= 1.1f;

		if (NPC.downedMechBoss3)
			value *= 1.1f;

		if (NPC.downedPlantBoss)
			value *= 1.1f;

		if (NPC.downedQueenBee)
			value *= 1.1f;

		if (NPC.downedGolemBoss)
			value *= 1.1f;

		if (NPC.downedPirates)
			value *= 1.1f;

		if (NPC.downedGoblins)
			value *= 1.1f;

		if (NPC.downedFrost)
			value *= 1.1f;

		return (int)value;
	}
}