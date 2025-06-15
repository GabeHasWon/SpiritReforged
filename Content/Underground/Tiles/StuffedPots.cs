using RubbleAutoloader;
using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.TileCommon.PresetTiles;
using SpiritReforged.Content.Underground.Pottery;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.ItemDropRules;
using Terraria.GameContent.UI;
using static SpiritReforged.Common.TileCommon.StyleDatabase;
using static SpiritReforged.Common.WorldGeneration.WorldMethods;

namespace SpiritReforged.Content.Underground.Tiles;

public class StuffedPots : PotTile, ILootTile
{
	public override Dictionary<string, int[]> TileStyles => new() { { string.Empty, [0, 1, 2] } };
	public override void AddRecord(int type, StyleGroup group)
	{
		var desc = Language.GetText("Mods.SpiritReforged.Tiles.Records.Stuffed");
		RecordHandler.Records.Add(new TileRecord(group.name, type, group.styles).AddDescription(desc).AddRating(5));
	}

	public override void AddObjectData()
	{
		Main.tileOreFinderPriority[Type] = 575;
		base.AddObjectData();
	}

	public override void AddMapData() => AddMapEntry(new Color(180, 90, 95), Language.GetText("Mods.SpiritReforged.Items.StuffedPotsItem.DisplayName"));

	public override bool KillSound(int i, int j, bool fail)
	{
		if (fail || Autoloader.IsRubble(Type))
			return true;

		var pos = new Vector2(i, j).ToWorldCoordinates(16, 16);

		SoundEngine.PlaySound(SoundID.Shatter, pos);
		SoundEngine.PlaySound(new SoundStyle("SpiritReforged/Assets/SFX/Tile/PotBreak") with { Volume = .16f, PitchRange = (-.4f, 0), }, pos);

		return true;
	}

	public override void NearbyEffects(int i, int j, bool closer)
	{
		if (!closer || !TileObjectData.IsTopLeft(i, j))
			return;

		var position = new Vector2(i, j).ToWorldCoordinates(20, 8);
		float chance = Main.LocalPlayer.DistanceSQ(position) / (200 * 200) + 5;

		if (Main.rand.NextFloat(chance) < .1f)
			EmoteBubble.NewBubble(EmoteID.EmotionAnger, new WorldUIAnchor(position + new Vector2(12, 0)), 60);
	}

	public override void KillMultiTile(int i, int j, int frameX, int frameY)
	{
		var spawn = new Vector2(i, j).ToWorldCoordinates(16, 16);
		if (Main.netMode != NetmodeID.MultiplayerClient && !Autoloader.IsRubble(Type) && !Generating)
		{
			var source = new EntitySource_TileBreak(i, j);
			Projectile.NewProjectile(source, new Vector2(i, j).ToWorldCoordinates(16, 16), Vector2.UnitY * -4f, ProjectileID.CoinPortal, 0, 0);

			ItemMethods.SplitCoins(Main.rand.Next(5000, 7000), delegate (int type, int stack)
			{
				Item.NewItem(new EntitySource_TileBreak(i, j), spawn, new Item(type, stack), noGrabDelay: true);
			});

			NPC.NewNPCDirect(new EntitySource_TileBreak(i, j), new Vector2(i, j).ToWorldCoordinates(16, 16), NPCID.SkeletonMerchant);
		}

		base.KillMultiTile(i, j, frameX, frameY);
	}

	public override void DeathEffects(int i, int j, int frameX, int frameY)
	{
		const int fullWidth = 36;

		var source = new EntitySource_TileBreak(i, j);
		var position = new Vector2(i, j) * 16;

		for (int g = 0; g < 3; g++)
		{
			int goreType = 51 + g;
			Gore.NewGore(source, position, Vector2.Zero, goreType);
		}

		if (frameX / fullWidth == 2)
		{
			for (int d = 2; d < 4; d++)
				Gore.NewGore(source, new Vector2(i, j) * 16, Vector2.UnitY * -2f, Mod.Find<ModGore>("Stuffed" + d).Type);
		}
		else if (frameX / fullWidth == 0)
		{
			Gore.NewGore(source, new Vector2(i, j) * 16, Vector2.UnitY * -2f, Mod.Find<ModGore>("Stuffed1").Type);
		}
	}

	public void AddLoot(int objectStyle, ILoot loot)
	{
		loot.Add(ItemDropRule.Common(ItemID.Glowstick, 1, 10, 25));
		loot.Add(ItemDropRule.Common(ItemID.StrangeBrew, 1, 2, 8));
	}
}