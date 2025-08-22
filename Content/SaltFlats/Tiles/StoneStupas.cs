using SpiritReforged.Common.Misc;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.TileCommon.PresetTiles;
using SpiritReforged.Content.Underground.Pottery;
using SpiritReforged.Content.Underground.Tiles;
using Terraria.Audio;
using Terraria.DataStructures;

namespace SpiritReforged.Content.SaltFlats.Tiles;

public class StoneStupas : PotTile, ILootable
{
	public override Dictionary<string, int[]> TileStyles => new() { { string.Empty, [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11] } };

	public static readonly SoundStyle Break = new("SpiritReforged/Assets/SFX/Tile/StoneStupaShatter", 2)
	{
		PitchVariance = 0.2f
	};

	public override void AddItemRecipes(ModItem modItem, StyleDatabase.StyleGroup group, Condition condition) => modItem.CreateRecipe()
		.AddRecipeGroup("ClayAndMud", 3).AddIngredient(ItemID.StoneBlock, 3).AddTile(ModContent.TileType<PotteryWheel>()).AddCondition(condition).Register();

	public override void AddObjectData()
	{
		DustType = IsRubble ? -1 : DustID.Stone;
		HitSound = IsRubble ? SoundID.Dig : Break;

		base.AddObjectData();
	}

	public override void KillTile(int i, int j, ref bool fail, ref bool effectOnly, ref bool noItem)
	{
		if (effectOnly || fail || IsRubble)
			return;

		if (TileObjectData.IsTopLeft(i, j))
		{
			if (Main.netMode != NetmodeID.MultiplayerClient)
				Pots.SpawnThingsFromPot(default, i, j, i, j, 0);

			for (int g = 1; g < 7; g++)
			{
				int goreType = Mod.Find<ModGore>("Stupa" + g).Type;
				var position = Main.rand.NextVector2FromRectangle(new(i * 16, j * 16, 32, 32));

				Gore.NewGore(new EntitySource_TileBreak(i, j), position, Vector2.Zero, goreType);
			}
		}
	}

	public void AddLoot(ILoot loot)
	{
		if (TileLootHandler.TryGetLootPool(ModContent.TileType<Pots>(), out var dele))
			dele.Invoke(loot);
	}
}