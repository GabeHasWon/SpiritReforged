using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.TileCommon.PresetTiles;
using SpiritReforged.Common.WorldGeneration;
using SpiritReforged.Content.Underground.Pottery;
using SpiritReforged.Content.Underground.Tiles;
using Terraria.Audio;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Desert.Tiles;

public class BronzePots : PotTile, ILootable
{
	public override Dictionary<string, int[]> TileStyles => new() { { string.Empty, [0, 1, 2] } };

	public override void AddItemRecipes(ModItem modItem, NamedStyles.StyleGroup group, Condition condition) => modItem.CreateRecipe()
		.AddRecipeGroup("ClayAndMud", 3).AddIngredient(AutoContent.ItemType<BronzePlating>(), 2).AddTile(ModContent.TileType<PotteryWheel>()).AddCondition(condition).Register();

	public override void SetStaticDefaults()
	{
		base.SetStaticDefaults();

		Main.tileOreFinderPriority[Type] = 100;
		DustType = IsRubble ? -1 : DustID.Copper;
	}

	public override void AddMapData() => AddMapEntry(new Color(112, 60, 70), Language.GetText("MapObject.Pot"));

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
			var center = new Vector2(i, j).ToWorldCoordinates(16, 16);
			var source = new EntitySource_TileBreak(i, j);
			var p = Main.player[Player.FindClosest(center, 0, 0)];

			TileLootSystem.Resolve(i, j, Type, frameX, frameY);
			ItemMethods.SplitCoins((int)CalculateCoinValue(), delegate (int type, int stack)
			{
				Item.NewItem(source, center, new Item(type, stack), noGrabDelay: true);
			});

			if (p.statLife < p.statLifeMax2)
			{
				int stack = Main.rand.Next(3, 6);

				for (int h = 0; h < stack; h++)
					Item.NewItem(source, center, ItemID.Heart);
			}

			if (Main.rand.NextBool(100))
				Projectile.NewProjectile(source, center, Vector2.UnitY * -4f, ProjectileID.CoinPortal, 0, 0);
		}

		base.KillMultiTile(i, j, frameX, frameY);
	}

	public override void DeathEffects(int i, int j, int frameX, int frameY)
	{
		var source = new EntitySource_TileBreak(i, j);
		var position = new Vector2(i, j) * 16;

		for (int g = 1; g < 4; g++)
		{
			int goreType = Mod.Find<ModGore>("PotBronze" + g).Type;
			Gore.NewGore(source, position, Vector2.Zero, goreType);
		}
	}

	public void AddLoot(ILoot loot)
	{
		if (TileLootSystem.TryGetLootPool(ModContent.TileType<Pots>(), out var dele))
			dele.Invoke(loot);
	}
}