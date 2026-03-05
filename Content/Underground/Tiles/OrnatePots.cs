using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.TileCommon.PresetTiles;
using SpiritReforged.Common.UI.PotCatalogue;
using SpiritReforged.Common.WorldGeneration;
using SpiritReforged.Content.Underground.Pottery;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.ItemDropRules;

namespace SpiritReforged.Content.Underground.Tiles;

public class OrnatePots : PotTile, ILootable
{
	public override Dictionary<string, int[]> TileStyles => new() { { string.Empty, [0, 1, 2] } };

	public override TileRecord AddRecord(int type, NamedStyles.StyleGroup group)
	{
		var record = new TileRecord(group.name, type, group.styles);
		return record.AddRating(5).AddDescription(Language.GetText(TileRecord.DescKey + ".CoinPortal"));
	}

	public override void AddItemRecipes(ModItem modItem, NamedStyles.StyleGroup group, Condition condition) => modItem.CreateRecipe()
		.AddRecipeGroup("ClayAndMud", 3).AddRecipeGroup("GoldBars", 2).AddTile(ModContent.TileType<PotteryWheel>()).AddCondition(condition).Register();

	public override void SetStaticDefaults()
	{
		base.SetStaticDefaults();

		Main.tileOreFinderPriority[Type] = 575;
		DustType = IsRubble ? -1 : DustID.Gold;
	}

	public override void AddMapData() => AddMapEntry(new Color(180, 180, 77), Language.GetText("Mods.SpiritReforged.Items.OrnatePotsItem.DisplayName"));

	public override bool KillSound(int i, int j, bool fail)
	{
		if (!fail && !IsRubble)
		{
			var pos = new Vector2(i, j).ToWorldCoordinates(16, 16);
			SoundEngine.PlaySound(SoundID.Shatter, pos);

			return false;
		}

		return true;
	}

	public override void KillMultiTile(int i, int j, int frameX, int frameY)
	{
		if (!IsRubble && Main.netMode != NetmodeID.MultiplayerClient && !WorldMethods.Generating)
		{
			var spawn = new Vector2(i, j).ToWorldCoordinates(16, 16);
			var source = new EntitySource_TileBreak(i, j);
			Projectile.NewProjectile(source, new Vector2(i, j).ToWorldCoordinates(16, 16), Vector2.UnitY * -4f, ProjectileID.CoinPortal, 0, 0);

			ItemMethods.SplitCoins(Main.rand.Next(10000, 20000), delegate (int type, int stack)
			{
				Item.NewItem(new EntitySource_TileBreak(i, j), spawn, new Item(type, stack), noGrabDelay: true);
			});
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

	public void AddLoot(ILoot loot)
	{
		loot.Add(ItemDropRule.Common(ItemID.LuckPotion, 2, 1, 2));
		loot.Add(ItemDropRule.Common(ItemID.HealingPotion, 1, 1, 3));
	}

	public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
	{
		if (TileObjectData.IsTopLeft(i, j))
			GlowTileHandler.AddGlowPoint(new Rectangle(i, j, 32, 32), Color.Goldenrod * .5f, 200);

		return true;
	}
}