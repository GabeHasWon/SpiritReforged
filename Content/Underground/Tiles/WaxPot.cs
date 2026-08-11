using SpiritReforged.Common.TileCommon.PresetTiles;
using SpiritReforged.Common.UI.PotCatalogue;
using static SpiritReforged.Common.TileCommon.NamedStyles;
using Terraria.DataStructures;
using SpiritReforged.Common.WorldGeneration;
using SpiritReforged.Common.Misc;
using SpiritReforged.Content.Glyphs;
using SpiritReforged.Common.Particle;
using SpiritReforged.Content.Particles;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Content.Underground.Pottery;
using Terraria.Audio;
using SpiritReforged.Common.ItemCommon;

namespace SpiritReforged.Content.Underground.Tiles;

public class WaxPot : PotTile, ILootable
{
	public override void AddItemRecipes(ModItem modItem, NamedStyles.StyleGroup group, Condition condition) => modItem.CreateRecipe().AddIngredient(ItemID.ClayBlock, 5)
		.AddIngredient(AutoContent.ItemType<WaxBlock>(), 3).AddTile(ModContent.TileType<PotteryWheel>()).AddCondition(condition).Register();

	public override Dictionary<string, int[]> TileStyles => new() { { string.Empty, [0] } };

	public override TileRecord AddRecord(int type, StyleGroup group)
	{
		var record = new TileRecord(group.name, type, group.styles);
		return record.AddRating(3).AddDescription(Language.GetText(TileRecord.DescKey + ".Wax"));
	}

	public override void AddObjectData()
	{
		const int row = 1;

		Main.tileOreFinderPriority[Type] = 575;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
		TileObjectData.newTile.Origin = new(0, 1);
		TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop, TileObjectData.newTile.Width, 0);
		TileObjectData.newTile.StyleWrapLimit = row;
		TileObjectData.newTile.RandomStyleRange = row;
		TileObjectData.newTile.StyleHorizontal = true;
		TileObjectData.newTile.UsesCustomCanPlace = true;
		TileObjectData.newTile.DrawYOffset = 2;
		TileObjectData.addTile(Type);

		DustType = IsRubble ? -1 : 58;
	}

	public override void AddMapData() => AddMapEntry(new Color(146, 76, 77), Language.GetText("Mods.SpiritReforged.Items.WaxPotItem.DisplayName"));
	public void AddLoot(ILoot loot) => loot.AddCommon(ModContent.ItemType<ChromaticWax>(), 1, 3, 6);

	public override void EmitParticles(int i, int j, Tile tile, short tileFrameX, short tileFrameY, Color tileLight, bool visible)
	{
		if (visible && TileObjectData.IsTopLeft(i, j) && Main.rand.NextBool(8) && Lighting.Brightness(i, j) > 0.5f)
		{
			Rectangle area = new(i * 16, j * 16, 32, 32);
			ParticleHandler.SpawnParticle(new SharpStarParticle(Main.rand.NextVector2FromRectangle(area), Vector2.Zero, ChromaticWax.SpecialColor, 0.2f, 50, 0, AddLight: false));
		}
	}

	public override void KillMultiTile(int i, int j, int frameX, int frameY)
	{
		if (WorldMethods.Generating || IsRubble)
			return;

		var position = new Vector2(i, j).ToWorldCoordinates(16, 16);

		ItemMethods.SplitCoins(Main.rand.Next(30000, 50000), delegate (int type, int stack)
		{
			Item.NewItem(new EntitySource_TileBreak(i, j), position, new Item(type, stack), noGrabDelay: true);
		});

		if (Main.netMode != NetmodeID.MultiplayerClient)
		{
			SoundEngine.PlaySound(new SoundStyle("SpiritReforged/Assets/SFX/Tile/PotBreak") with { Volume = 0.5f }, position);
			SoundEngine.PlaySound(SoundID.DD2_WitherBeastAuraPulse, position);

			for (int x = 0; x < 20; x++)
				ParticleHandler.SpawnParticle(new EmberParticle(position + Main.rand.NextVector2Circular(15, 15), Vector2.UnitY * -Main.rand.NextFloat(0.1f, 1f), ChromaticWax.SpecialColor, 1, 30, 2));
		}

		base.KillMultiTile(i, j, frameX, frameY);
	}
}
