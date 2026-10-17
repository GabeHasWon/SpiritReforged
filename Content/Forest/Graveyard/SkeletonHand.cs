using RubbleAutoloader;
using SpiritReforged.Common.Particle;
using Terraria.DataStructures;
using Terraria.GameContent.Drawing;
using TileHelper.Common;

namespace SpiritReforged.Content.Forest.Graveyard;

public class SkeletonHand : ModTile, IAutoloadRubble
{
	private static readonly Point[] glowPoints = [new Point(9, 11), new Point(13, 5), new Point(7, 11)]; //Corresponds to different styles

	public IAutoloadRubble.RubbleData Data => new(ModContent.ItemType<SafekeeperRing>(), IAutoloadRubble.RubbleSize.Small);

	public override void SetStaticDefaults()
	{
		Main.tileNoFail[Type] = true;
		Main.tileSolid[Type] = false;
		Main.tileMergeDirt[Type] = false;
		Main.tileBlockLight[Type] = false;
		Main.tileFrameImportant[Type] = true;

		TileID.Sets.CanDropFromRightClick[Type] = true;
		TileHelperSets.TileGlowmask[Type] = Helpers.RequestGlowmask(this, GetGlowColor);

		const int height = 30;
		TileObjectData.newTile.CopyFrom(TileObjectData.Style1x1);
		TileObjectData.newTile.CoordinateWidth = 26;
		TileObjectData.newTile.CoordinateHeights = [height];
		TileObjectData.newTile.DrawYOffset = -(height - 18);
		TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile, 1, 0);
		TileObjectData.newTile.StyleHorizontal = true;
		TileObjectData.newTile.RandomStyleRange = 3;

		TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile);
		TileObjectData.newAlternate.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.AlternateTile, 1, 0);
		TileObjectData.newAlternate.AnchorAlternateTiles = [ModContent.TileType<BonePile>()];
		TileObjectData.newAlternate.DrawYOffset = -(height - 18) + 4; //Offset not working

		TileObjectData.addAlternate(0);
		TileObjectData.addTile(Type);

		AddMapEntry(new Color(165, 165, 150));
		RegisterItemDrop(ModContent.ItemType<SafekeeperRing>());
	}

	private static Color GetGlowColor(int i, int j)
	{
		float mult = MathHelper.Clamp(1f - Main.LocalPlayer.Distance(new Vector2(i, j) * 16) / 150f, 0, 1);
		float lerp = (float)Math.Sin(Main.timeForVisualEffects / 50f) * 0.25f;

		return Color.White * (mult + lerp);
	}

	public override void NearbyEffects(int i, int j, bool closer)
	{
		if (closer && !Main.gamePaused && Main.rand.NextBool(5))
		{
			Tile tile = Main.tile[i, j];
			float mult = MathHelper.Clamp(1f - Main.LocalPlayer.Distance(new Vector2(i, j) * 16) / 150f, 0, 1);

			if (!TileDrawing.IsVisible(tile) || TileObjectData.GetTileData(tile) is not TileObjectData objectData || mult < 0.15f)
				return;

			Rectangle source = new(tile.TileFrameX, tile.TileFrameY, objectData.CoordinateWidth, objectData.CoordinateFullHeight);
			Vector2 position = new Vector2(i, j) * 16 - new Vector2((source.Width - 16) / 2, source.Height - 16 - 4);

			int style = TileObjectData.GetTileStyle(Main.tile[i, j]);
			var dustPos = position + glowPoints[style].ToVector2() + Main.rand.NextVector2Unit() * Main.rand.NextFloat(3f);

			ParticleHandler.SpawnParticle(new Particles.GlowParticle(dustPos,
				Vector2.UnitY * -Main.rand.NextFloat(0.5f), Color.White, Color.Orange, 0.15f, 30, 5));
		}
	}

	public override void MouseOver(int i, int j)
	{
		Player player = Main.LocalPlayer;
		player.noThrow = 2;
		player.cursorItemIconEnabled = true;
		player.cursorItemIconID = ModContent.ItemType<SafekeeperRing>();
	}
}