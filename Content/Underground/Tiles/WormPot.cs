using RubbleAutoloader;
using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.TileCommon.PresetTiles;
using SpiritReforged.Common.TileCommon.TileSway;
using SpiritReforged.Content.Particles;
using SpiritReforged.Content.Underground.Pottery;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.ItemDropRules;
using Terraria.Utilities;
using static SpiritReforged.Common.TileCommon.StyleDatabase;
using static SpiritReforged.Common.WorldGeneration.WorldMethods;

namespace SpiritReforged.Content.Underground.Tiles;

public class WormPot : PotTile, ISwayTile, ILootTile, ICutAttempt
{
	public override Dictionary<string, int[]> TileStyles => new() { { string.Empty, [0, 1] } };

	public override void AddRecord(int type, StyleGroup group)
	{
		var desc = Language.GetText(TileRecord.DescKey + ".Worm");
		RecordHandler.Records.Add(new TileRecord(group.name, type, group.styles).AddDescription(desc).AddRating(3));
	}

	public override void AddItemRecipes(ModItem modItem, StyleGroup group)
	{
		LocalizedText dicovered = AutoloadedPotItem.Discovered;
		var function = (modItem as AutoloadedPotItem).RecordedPot;

		modItem.CreateRecipe().AddRecipeGroup("ClayAndMud", 3).AddIngredient(ItemID.DirtBlock, 5).AddIngredient(ItemID.Worm)
			.AddTile(ModContent.TileType<PotteryWheel>()).AddCondition(dicovered, function).Register();
	}

	public override void AddObjectData()
	{
		Main.tileOreFinderPriority[Type] = 575;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
		TileObjectData.newTile.Origin = new(0, 1);
		TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop, TileObjectData.newTile.Width, 0);
		TileObjectData.newTile.StyleWrapLimit = 2;
		TileObjectData.newTile.StyleHorizontal = true;
		TileObjectData.newTile.UsesCustomCanPlace = true;
		TileObjectData.newTile.DrawYOffset = 2;
		TileObjectData.addTile(Type);

		DustType = DustID.Plantera_Pink;
	}

	public override void AddMapData() => AddMapEntry(Color.MediumVioletRed, Language.GetText("Mods.SpiritReforged.Items.WormPotItem.DisplayName"));

	public override void NumDust(int i, int j, bool fail, ref int num) => num = fail ? 1 : 3;
	public override void KillTile(int i, int j, ref bool fail, ref bool effectOnly, ref bool noItem)
	{
		if (effectOnly || !fail || Autoloader.IsRubble(Type) || Generating)
			return;

		fail = AdjustFrame(i, j);
		ISwayTile.SetInstancedRotation(i, j, Main.rand.NextFloat(-1f, 1f) * 4f, fail);
	}

	public bool OnCutAttempt(int i, int j)
	{
		if (ClubQuickKill(i, j))
			return true;

		bool fail = AdjustFrame(i, j);
		ISwayTile.SetInstancedRotation(i, j, Main.rand.NextFloat(-1f, 1f) * 4f, fail);

		var cache = Main.tile[i, j];
		WorldGen.KillTile_MakeTileDust(i, j, cache);
		WorldGen.KillTile_PlaySounds(i, j, true, cache);

		return !fail;
	}

	public override bool KillSound(int i, int j, bool fail)
	{
		if (Autoloader.IsRubble(Type))
			return true;

		var pos = new Vector2(i, j).ToWorldCoordinates(16, 16);
		SoundEngine.PlaySound(SoundID.NPCHit1 with { Volume = .3f, Pitch = .25f }, pos);

		if (!fail)
			SoundEngine.PlaySound(SoundID.NPCDeath1, pos);

		return true;
	}

	private static bool AdjustFrame(int i, int j)
	{
		const int fullWidth = 36;

		TileExtensions.GetTopLeft(ref i, ref j);

		if (Main.tile[i, j].TileFrameX != 0)
			return false; //Frame has already been adjusted

		for (int x = i; x < i + 2; x++)
		{
			for (int y = j; y < j + 2; y++)
			{
				var t = Main.tile[x, y];
				t.TileFrameX += fullWidth;
			}
		}

		return true;
	}

	public override void KillMultiTile(int i, int j, int frameX, int frameY)
	{
		if (Autoloader.IsRubble(Type) || Generating)
			return;

		if (Main.netMode != NetmodeID.MultiplayerClient)
		{
			var position = new Vector2(i, j).ToWorldCoordinates(16, 16);

			WeightedRandom<int> type = new();
			type.Add(NPCID.Worm);
			type.Add(NPCID.EnchantedNightcrawler, .25);
			type.Add(NPCID.GoldWorm, .05);

			int wormCount = Main.rand.Next(3, 7);

			for (int w = 0; w < wormCount; w++)
			{
				var npc = NPC.NewNPCDirect(new EntitySource_TileBreak(i, j), position + Main.rand.NextVector2Unit() * Main.rand.NextFloat(10f), (int)type);
				npc.velocity = (Vector2.UnitY * -Main.rand.NextFloat(.5f, 2f)).RotatedByRandom(2f);
			}

			ItemMethods.SplitCoins(Main.rand.Next(30000, 50000), delegate (int type, int stack)
			{
				Item.NewItem(new EntitySource_TileBreak(i, j), position, new Item(type, stack), noGrabDelay: true);
			});

			LootTable.Resolve(i, j, Type, frameX, frameY);
		}

		if (!Main.dedServ)
		{
			var source = new EntitySource_TileBreak(i, j);
			var position = new Vector2(i, j) * 16;

			for (int g = 1; g < 4; g++)
			{
				int goreType = Mod.Find<ModGore>("PotWorm" + g).Type;
				Gore.NewGore(source, position, Vector2.Zero, goreType);
			}

			ParticleHandler.SpawnParticle(new SmokeCloud(new Vector2(i, j).ToWorldCoordinates(16, 16),
				-Vector2.UnitY, Color.LightSeaGreen, .2f, Common.Easing.EaseFunction.EaseSine, 60));
		}
	}

	public void DrawSway(int i, int j, SpriteBatch spriteBatch, Vector2 offset, float rotation, Vector2 origin)
	{
		var tile = Main.tile[i, j];
		var data = TileObjectData.GetTileData(tile);

		var drawPos = new Vector2(i * 16 - (int)Main.screenPosition.X, j * 16 - (int)Main.screenPosition.Y);
		var source = new Rectangle(tile.TileFrameX, tile.TileFrameY, data.CoordinateWidth, data.CoordinateHeights[tile.TileFrameY / 18]);
		var dataOffset = new Vector2(data.DrawXOffset, data.DrawYOffset);

		var color = Lighting.GetColor(i, j);

		if (Main.LocalPlayer.findTreasure)
			color = TileExtensions.GetSpelunkerTint(color);

		spriteBatch.Draw(TextureAssets.Tile[tile.TileType].Value, drawPos + origin + dataOffset,
			source, color, rotation, origin, 1, SpriteEffects.None, 0);
	}

	public void AddLoot(int objectStyle, ILoot loot)
	{
		loot.Add(ItemDropRule.NotScalingWithLuckWithNumerator(ItemID.WhoopieCushion, 100, 15));
		loot.AddCommon(ItemID.CanOfWorms, 1, 1, 2);
	}
}