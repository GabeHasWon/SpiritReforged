using SpiritReforged.Common.Visuals.Glowmasks;
using SpiritReforged.Content.Underground.Items.BigBombs;
using Terraria.Audio;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Underground.Tiles;

[AutoloadGlowmask("255,255,255")]
public class ObsidianShroom : ModTile
{
	public static readonly SoundStyle Break = new("SpiritReforged/Assets/SFX/Tile/StoneCrack2")
	{
		PitchRange = (.2f, .8f)
	};

	public override void SetStaticDefaults()
	{
		Main.tileFrameImportant[Type] = true;
		Main.tileOreFinderPriority[Type] = 600;
		Main.tileSpelunker[Type] = true;
		Main.tileNoFail[Type] = true;

		TileID.Sets.PreventsTileReplaceIfOnTopOfIt[Type] = true;
		TileID.Sets.PreventsTileRemovalIfOnTopOfIt[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
		TileObjectData.newTile.Origin = new(0, 1);
		TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop, TileObjectData.newTile.Width, 0);
		TileObjectData.newTile.DrawYOffset = 2;
		TileObjectData.addTile(Type);

		DustType = DustID.Obsidian;
		MinPick = 55;

		AddMapEntry(new Color(50, 25, 55), CreateMapEntryName());
		RegisterItemDrop(ModContent.ItemType<BoomShroom>());
	}

	public override void NumDust(int i, int j, bool fail, ref int num) => num = fail ? 1 : 10;
	public override bool KillSound(int i, int j, bool fail)
	{
		if (!fail)
			SoundEngine.PlaySound(Break, new Vector2(i, j).ToWorldCoordinates(16, 16));

		return true;
	}

	public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
	{
		Lighting.AddLight(new Vector2(i, j) * 16, Color.Orange.ToVector3() * .2f);
		return true;
	}
}

/// <summary> Handles natural <see cref="ObsidianShroom"/> gen. </summary>
internal class ShroomGlobalTile : GlobalTile
{
	public override void RandomUpdate(int i, int j, int type)
	{
		if (type is TileID.Stone or TileID.Obsidian && i > Main.rockLayer && Main.rand.NextBool(3000) && WorldGen.InWorld(i, j, 20) && !Framing.GetTileSafely(i, j - 1).HasTile)
		{
			int toPlace = ModContent.TileType<ObsidianShroom>();

			if (WorldGen.CountNearBlocksTypes(i, j, 100, 4, type) == 0)
				WorldGen.PlaceTile(i, j - 1, toPlace, true);
		}
	}
}