using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Content.Forest.Cloud.Items;
using SpiritReforged.Content.Underground.NPCs;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.Drawing;

namespace SpiritReforged.Content.Underground.Tiles;

public class BiomePots : ModTile
{
	public enum Style : int
	{
		Cavern, Gold, Ice, Desert, Jungle, Dungeon, Corruption, Crimson, Marble, Hell, Mushroom
	}

	/// <summary> Unit for distance-based vfx. </summary>
	private const int DistMod = 200;
	private static readonly HashSet<Point16> GlowPoints = [];

	#region drawing detours
	public override void Load()
	{
		DrawOrderSystem.DrawTilesNonSolidEvent += DrawGlow;
		On_TileDrawing.PreDrawTiles += ClearAll;
	}

	private static void DrawGlow()
	{
		foreach (var p in GlowPoints)
			DrawGlow(p.ToWorldCoordinates(16, 18));
	}

	private static void ClearAll(On_TileDrawing.orig_PreDrawTiles orig, TileDrawing self, bool solidLayer, bool forRenderTargets, bool intoRenderTargets)
	{
		orig(self, solidLayer, forRenderTargets, intoRenderTargets);

		bool flag = intoRenderTargets || Lighting.UpdateEveryFrame;

		if (!solidLayer && flag)
			GlowPoints.Clear();
	}
	#endregion

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

	public override void NearbyEffects(int i, int j, bool closer)
	{
		if (closer)
		{
			var world = new Vector2(i, j) * 16;
			float strength = Main.LocalPlayer.DistanceSQ(world) / (DistMod * DistMod);

			if (strength < 1 && Main.rand.NextFloat(8f) < 1f - strength)
			{
				var d = Dust.NewDustDirect(world, 16, 16, DustID.TreasureSparkle, 0, 0, Scale: Main.rand.NextFloat());
				d.noGravity = true;
				d.velocity = new Vector2(0, -Main.rand.NextFloat(2f));
			}
		}
	}

	public override bool KillSound(int i, int j, bool fail)
	{
		if (!fail)
		{
			var pos = new Vector2(i, j).ToWorldCoordinates(16, 16);

			SoundEngine.PlaySound(SoundID.Shatter, pos);
			SoundEngine.PlaySound(new SoundStyle("SpiritReforged/Assets/SFX/Tile/PotBreak") with { Volume = .16f, PitchRange = (-.4f, 0), }, pos);

			return false;
		}

		return true;
	}

	public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
	{
		if (TileObjectData.IsTopLeft(i, j) && GetStyle(i, j) is Style.Gold)
			GlowPoints.Add(new Point16(i, j));

		return true;
	}

	private static void DrawGlow(Vector2 position)
	{
		const int squareSize = 32;

		var drawPos = position - Main.screenPosition;
		var region = new Rectangle((int)drawPos.X - squareSize / 2, (int)drawPos.Y - squareSize / 2, squareSize, squareSize);
		Color color = Color.White;

		float opacity = MathHelper.Clamp(1f - Main.LocalPlayer.DistanceSQ(position) / (DistMod * DistMod), 0, .75f) * Lighting.Brightness((int)(position.X / 16), (int)(position.Y / 16));

		short[] indices = [0, 1, 2, 1, 3, 2];

		//Note that corner positions are reversed to flip the effect
		VertexPositionColorTexture[] vertices =
		[
			new(new Vector3(region.BottomRight(), 0), color, new Vector2(0, 0)),
			new(new Vector3(region.BottomLeft(), 0), color, new Vector2(1, 0)),
			new(new Vector3(region.TopRight(), 0), color, new Vector2(0, 1)),
			new(new Vector3(region.TopLeft(), 0), color, new Vector2(1, 1)),
		];

		var projection = Matrix.CreateOrthographicOffCenter(0, Main.screenWidth, Main.screenHeight, 0, -1, 1);
		Matrix view = Main.GameViewMatrix.TransformationMatrix;
		Effect effect = AssetLoader.LoadedShaders["ShadowFade"];

		foreach (EffectPass pass in effect.CurrentTechnique.Passes)
		{
			effect.Parameters["baseShadowColor"].SetValue(Color.Goldenrod.ToVector4() * opacity);
			effect.Parameters["adjustColor"].SetValue(Color.White.ToVector4() * opacity);
			effect.Parameters["noiseScroll"].SetValue(Main.GameUpdateCount * 0.0015f);
			effect.Parameters["noiseStretch"].SetValue(.5f);
			effect.Parameters["uWorldViewProjection"].SetValue(view * projection);
			effect.Parameters["noiseTexture"].SetValue(AssetLoader.LoadedTextures["vnoise"].Value);
			pass.Apply();

			Main.instance.GraphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, vertices, 0, 4, indices, 0, 2);
		}
	}

	public override void KillMultiTile(int i, int j, int frameX, int frameY)
	{
		if (WorldGen.generatingWorld)
			return; //Particularly important for not incrementing Remaining

		var style = (Style)(frameY / 36);
		int variant = frameX / 36;

		var source = new EntitySource_TileBreak(i, j);
		var position = new Vector2(i, j).ToWorldCoordinates(16, 16);
		int dustType = DustID.Pot;

		bool spawnSlime = PotteryTracker.Remaining == 1;
		PotteryTracker.TrackOne();

		if (Main.netMode != NetmodeID.MultiplayerClient)
		{
			HandleLoot(i, j, style); //Drops should only occur on the server/singleplayer

			if (spawnSlime)
				NPC.NewNPCDirect(new EntitySource_TileBreak(i, j), position, ModContent.NPCType<PotterySlime>());

			if (Main.dedServ)
				return;
		}

		switch (style)
		{
			case Style.Cavern:

				for (int g = 0; g < 3; g++)
				{
					int goreType = Mod.Find<ModGore>("PotCavern" + (g + variant * 3 + 1)).Type;
					Gore.NewGore(source, position, Vector2.Zero, goreType);
				}

				dustType = DustID.Pot;

				break;

			case Style.Gold:

				for (int g = 1; g < 4; g++)
				{
					int goreType = Mod.Find<ModGore>("PotGold" + g).Type;
					Gore.NewGore(source, position, Vector2.Zero, goreType);
				}

				dustType = DustID.Gold;
				break;

			case Style.Ice:

				for (int g = 1; g < 4; g++)
				{
					int goreType = Mod.Find<ModGore>("PotIce" + g).Type;
					Gore.NewGore(source, position, Vector2.Zero, goreType);
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

				break;

			case Style.Crimson:

				dustType = DustID.Crimson;

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
		}

		for (int d = 0; d < 20; d++)
			Dust.NewDustPerfect(GetRandom(), dustType, Main.rand.NextVector2Unit(), Scale: Main.rand.NextFloat() + .25f);

		Vector2 GetRandom(float distance = 15f) => position + Main.rand.NextVector2Unit() * Main.rand.NextFloat(distance);
	}

	private static void HandleLoot(int i, int j, Style style)
	{
		var center = new Vector2(i, j).ToWorldCoordinates(16, 16);
		var p = Main.player[Player.FindClosest(center, 0, 0)];

		LootTable table = new();
		if (style is Style.Gold || Player.GetClosestRollLuck(i, j, 100) == 0) //COIN PORTAL
		{
			Projectile.NewProjectileDirect(new EntitySource_TileBreak(i, j), center, Vector2.UnitY * -12f, ProjectileID.CoinPortal, 0, 0);
			return;
		}

		if (style is Style.Dungeon && WorldGen.genRand.NextBool(10))
			table += LootTable.Create(ItemID.GoldenKey);

		if (WorldGen.genRand.NextBool(15)) //POTIONS
		{
			var potions = new LootTable().AddRange(ItemID.SpelunkerPotion, ItemID.HunterPotion,
			ItemID.GravitationPotion, ItemID.LifeforcePotion, ItemID.TitanPotion, ItemID.BattlePotion,
			ItemID.MagicPowerPotion, ItemID.ManaRegenerationPotion, ItemID.BiomeSightPotion, ItemID.HeartreachPotion,
			ModContent.ItemType<DoubleJumpPotion>(), WorldGen.crimson ? ItemID.RagePotion : ItemID.WrathPotion);

			if (style is Style.Cavern)
				potions.AddRange(ItemID.SwiftnessPotion, ItemID.RegenerationPotion, ItemID.SwiftnessPotion);
			else if (style is Style.Jungle)
				potions.Add(ItemID.SummoningPotion);
			else if (style is Style.Hell)
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

			if (style is Style.Jungle)
				flasks.Add(ItemID.FlaskofPoison);
			else if (style is Style.Hell)
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

				if (style == Style.Desert)
					type = ItemID.FossilOre;
				else if (style == Style.Dungeon && NPC.downedBoss3)
					type = ItemID.Bone;
				else if (style == Style.Marble)
					type = ItemID.Javelin;
				else if (style == Style.Hell)
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
					table += LootTable.Create((style == Style.Desert) ? ItemID.ScarabBomb : ItemID.Dynamite, Main.rand.Next(4, 11));
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

		static int GetPair(Style style, string context) //Helper for alternatives based on style
		{
			Dictionary<Style, int[]> dict = new()
			{
				{ Style.Cavern, [ItemID.UltrabrightTorch, ItemID.WoodenArrow] },
				{ Style.Ice, [ItemID.IceTorch, ItemID.FrostburnArrow] },
				{ Style.Desert, [ItemID.DesertTorch, ItemID.FlamingArrow] },
				{ Style.Jungle, [ItemID.JungleTorch, ItemID.FlamingArrow] },
				{ Style.Dungeon, [ItemID.BoneTorch, ItemID.BoneArrow] },
				{ Style.Corruption, [ItemID.CorruptTorch, ItemID.UnholyArrow] },
				{ Style.Crimson, [ItemID.CrimsonTorch, ItemID.UnholyArrow] },
				{ Style.Marble, [ItemID.YellowTorch, ItemID.JestersArrow] },
				{ Style.Hell, [ItemID.DemonTorch, ItemID.HellfireArrow] },
				{ Style.Mushroom, [ItemID.MushroomTorch, ItemID.WoodenArrow] }
			};

			int index = (context == "arrow") ? 1 : 0;
			return dict[style][index];
		}
	}

	private static Style GetStyle(int i, int j)
	{
		int frameY = Main.tile[i, j].TileFrameY;
		return (Style)(frameY / 36);
	}

	/// <summary> Calculates coin values similarly to how vanilla pots do. </summary>
	private static int CalculateCoinValue(Style style)
	{
		float value = 200 + WorldGen.genRand.Next(-100, 101);

		float biomeMult = style switch
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