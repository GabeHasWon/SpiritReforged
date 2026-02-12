using RubbleAutoloader;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.Visuals.Glowmasks;
using Terraria.DataStructures;
using Terraria.GameContent.Drawing;

namespace SpiritReforged.Content.Forest.Graveyard;

[AutoloadGlowmask("255,255,255", false)]
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

		TileObjectData.newTile.CopyFrom(TileObjectData.Style1x1);
		TileObjectData.newTile.CoordinateWidth = 26;
		TileObjectData.newTile.CoordinateHeights = [38];
		TileObjectData.newTile.DrawYOffset = -16;
		TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.AlternateTile, 1, 0);
		TileObjectData.newTile.AnchorAlternateTiles = [ModContent.TileType<BonePile>()];
		TileObjectData.newTile.StyleHorizontal = true;
		TileObjectData.newTile.RandomStyleRange = 3;
		TileObjectData.addTile(Type);

		AddMapEntry(new Color(165, 165, 150));
		RegisterItemDrop(ModContent.ItemType<SafekeeperRing>());
		DustType = -1;
	}

	public override void MouseOver(int i, int j)
	{
		Player player = Main.LocalPlayer;
		player.noThrow = 2;
		player.cursorItemIconEnabled = true;
		player.cursorItemIconID = ModContent.ItemType<SafekeeperRing>();
	}

	public override void PostDraw(int i, int j, SpriteBatch spriteBatch)
	{
		Tile tile = Main.tile[i, j];
		if (!TileDrawing.IsVisible(tile) || TileObjectData.GetTileData(tile) is not TileObjectData data)
			return;

		Rectangle source = new(tile.TileFrameX, tile.TileFrameY, data.CoordinateWidth, data.CoordinateFullHeight);
		Vector2 position = new Vector2(i, j) * 16 - new Vector2((source.Width - 16) / 2, source.Height - 16 - 4);

		float lerp = (float)Math.Sin(Main.timeForVisualEffects / 50f) * .25f;
		float mult = MathHelper.Clamp(1f - Main.LocalPlayer.Distance(new Vector2(i, j) * 16) / 150f, 0, 1);

		spriteBatch.Draw(GlowmaskTile.TileIdToGlowmask[Type].Glowmask.Value, position - Main.screenPosition + TileExtensions.TileOffset, 
			source, Color.White * (mult + lerp), 0, Vector2.Zero, 1, SpriteEffects.None, 0);

		if (!Main.gamePaused && mult > 0.15f && Main.rand.NextBool(5))
		{
			int style = TileObjectData.GetTileStyle(Main.tile[i, j]);
			var dustPos = position + glowPoints[style].ToVector2() + Main.rand.NextVector2Unit() * Main.rand.NextFloat(3f);

			ParticleHandler.SpawnParticle(new Particles.GlowParticle(dustPos,
				Vector2.UnitY * -Main.rand.NextFloat(0.5f), Color.White, Color.Orange, .15f, 30, 5));
		}
	}
}