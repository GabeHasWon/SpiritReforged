using RubbleAutoloader;
using SpiritReforged.Content.Crossmod.Spooky.Items.ShovelClub;
using SpiritReforged.Content.Crossmod.Spooky.SpookyForest.PumpkinClub;
using TileHelper.Common;

namespace SpiritReforged.Content.Crossmod.Spooky.Tiles;

public class SmackOLanternTile : ModTile, IAutoloadRubble
{
	public IAutoloadRubble.RubbleData Data => new IAutoloadRubble.RubbleData(ModContent.ItemType<PumpkinClub>(), IAutoloadRubble.RubbleSize.Medium);

	public override void SetStaticDefaults()
	{
		Main.tileFrameImportant[Type] = true;
		Main.tileNoAttach[Type] = true;
		Main.tileLavaDeath[Type] = true;
		Main.tileNoFail[Type] = true;
		Main.tileSpelunker[Type] = true;
		Main.tileOreFinderPriority[Type] = 285;

		TileHelperSets.TileGlowmask[Type] = Helpers.RequestGlowmask(this);

		TileID.Sets.DoesntGetReplacedWithTileReplacement[Type] = false;
		TileID.Sets.CanDropFromRightClick[Type] = true;

		ref TileObjectData tile = ref TileObjectData.newTile;
		tile.CopyFrom(TileObjectData.Style2x2);
		tile.Height = 3;
		tile.CoordinateHeights = [16, 16, 16];
		TileObjectData.addTile(Type);

		RegisterItemDrop(ModContent.ItemType<PumpkinClub>());
		AddMapEntry(new Color(191, 138, 67));
		DustType = DustID.Gold;
	}

	public override void MouseOver(int i, int j)
	{
		Player player = Main.LocalPlayer;
		player.noThrow = 2;
		player.cursorItemIconEnabled = true;
		player.cursorItemIconID = ModContent.ItemType<PumpkinClub>();
	}
}

public class ShovelTile : ModTile, IAutoloadRubble
{
	public IAutoloadRubble.RubbleData Data => new(ModContent.ItemType<ShovelClub>(), IAutoloadRubble.RubbleSize.Medium);

	public override void SetStaticDefaults()
	{
		Main.tileFrameImportant[Type] = true;
		Main.tileNoAttach[Type] = true;
		Main.tileLavaDeath[Type] = true;
		Main.tileNoFail[Type] = true;
		Main.tileSpelunker[Type] = true;
		Main.tileOreFinderPriority[Type] = 285;

		TileID.Sets.DoesntGetReplacedWithTileReplacement[Type] = false;
		TileID.Sets.CanDropFromRightClick[Type] = true;

		ref TileObjectData tile = ref TileObjectData.newTile;
		tile.CopyFrom(TileObjectData.Style2x2);
		tile.Height = 3;
		tile.CoordinateHeights = [16, 16, 18];
		TileObjectData.addTile(Type);

		RegisterItemDrop(ModContent.ItemType<ShovelClub>());
		AddMapEntry(new Color(112, 38, 38));
		DustType = DustID.Gold;
	}

	public override void MouseOver(int i, int j)
	{
		Player player = Main.LocalPlayer;
		player.noThrow = 2;
		player.cursorItemIconEnabled = true;
		player.cursorItemIconID = ModContent.ItemType<ShovelClub>();
	}
}
