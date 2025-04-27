using RubbleAutoloader;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.TileCommon.PresetTiles;
using SpiritReforged.Content.Underground.Pottery;
using Terraria.Audio;
using Terraria.DataStructures;
using static SpiritReforged.Content.Underground.Tiles.BiomePots;
using Terraria.GameContent.ItemDropRules;
using SpiritReforged.Common.ItemCommon;

namespace SpiritReforged.Content.Underground.Tiles;

public class OrnatePots : PotTile, ILootTile
{
	public override Dictionary<string, int[]> TileStyles => new() { { string.Empty, [0, 1, 2] } };

	public override void AddRecord(int type, StyleDatabase.StyleGroup group)
	{
		var record = new TileRecord(group.name, type, group.styles);
		RecordHandler.Records.Add(record.AddRating(5).AddDescription(Language.GetText(TileRecord.DescKey + ".CoinPortal")));
	}

	public override void SetStaticDefaults()
	{
		base.SetStaticDefaults();

		Main.tileOreFinderPriority[Type] = 575;
		DustType = DustID.Gold;
	}

	public override void AddMapData() => AddMapEntry(new Color(180, 180, 77), Language.GetText("Mods.SpiritReforged.Items.OrnatePotsItem.DisplayName"));

	public override bool KillSound(int i, int j, bool fail)
	{
		if (!fail && !Autoloader.IsRubble(Type))
		{
			var pos = new Vector2(i, j).ToWorldCoordinates(16, 16);
			SoundEngine.PlaySound(SoundID.Shatter, pos);

			return false;
		}

		return true;
	}

	public override void KillMultiTile(int i, int j, int frameX, int frameY)
	{
		var spawn = new Vector2(i, j).ToWorldCoordinates(16, 16);
		if (!Autoloader.IsRubble(Type) && Main.netMode != NetmodeID.MultiplayerClient)
		{
			var source = new EntitySource_TileBreak(i, j);
			Projectile.NewProjectile(source, new Vector2(i, j).ToWorldCoordinates(16, 16), Vector2.UnitY * -4f, ProjectileID.CoinPortal, 0, 0);

			ItemMethods.SplitCoins(Main.rand.Next(10000, 20000), delegate (int type, int stack)
			{
				Item.NewItem(new EntitySource_TileBreak(i, j), spawn, new Item(type, stack), noGrabDelay: true);
			});

			var p = Main.player[Player.FindClosest(spawn, 0, 0)];
			AddLoot(TileObjectData.GetTileStyle(Main.tile[i, j])).Resolve(new Rectangle((int)spawn.X - 16, (int)spawn.Y - 16, 32, 32), p);
		}

		base.KillMultiTile(i, j, frameX, frameY);
	}

	public override void DeathEffects(int i, int j, int frameX, int frameY)
	{
		var source = new EntitySource_TileBreak(i, j);
		var position = new Vector2(i, j) * 16;

		for (int g = 1; g < 4; g++)
		{
			int goreType = Mod.Find<ModGore>("PotGold" + g).Type;
			Gore.NewGore(source, position, Vector2.Zero, goreType);
		}
	}

	public LootTable AddLoot(int objectStyle)
	{
		var loot = new LootTable();
		loot.Add(ItemDropRule.Common(ItemID.GoldDust, 1, 2, 6));
		loot.Add(ItemDropRule.Common(ItemID.LuckPotion, 2, 1, 2));
		loot.Add(ItemDropRule.Common(ItemID.HealingPotion, 1, 1, 3));
		return loot;
	}

	public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
	{
		if (TileObjectData.IsTopLeft(i, j))
			GlowTileHandler.AddGlowPoint(new Rectangle(i, j, 32, 32), Color.Goldenrod * .5f, 200);

		return true;
	}
}