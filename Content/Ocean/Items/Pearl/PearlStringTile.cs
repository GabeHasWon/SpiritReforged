using RubbleAutoloader;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.Visuals.Glowmasks;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Ocean.Items.Pearl;

[AutoloadGlowmask("255,255,255", false)]
public class PearlStringTile : ModTile, IAutoloadRubble
{
	public IAutoloadRubble.RubbleData Data => new(ModContent.ItemType<PearlString>(), IAutoloadRubble.RubbleSize.Small);

	public override void SetStaticDefaults()
	{
		Main.tileSolid[Type] = false;
		Main.tileFrameImportant[Type] = true;
		Main.tileNoFail[Type] = true;

		TileID.Sets.CanDropFromRightClick[Type] = true;
		
		TileObjectData.newTile.CopyFrom(TileObjectData.Style2x1);
		TileObjectData.newTile.CoordinateHeights = [16];
		TileObjectData.newTile.Origin = new(1, 0);
		TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile, 2, 0);
		TileObjectData.newTile.AnchorValidTiles = [TileID.Sand, TileID.Ebonsand, TileID.Crimsand, TileID.Pearlsand];
		TileObjectData.addTile(Type);

		AddMapEntry(new Color(100, 100, 120));
		RegisterItemDrop(ModContent.ItemType<PearlString>());
		SolidBottomTile.TileTypes.Add(Type);

		DustType = DustID.Sand;
	}

	public override void MouseOver(int i, int j)
	{
		if (Autoloader.IsRubble(Type))
			return;

		Player player = Main.LocalPlayer;
		player.noThrow = 2;
		player.cursorItemIconEnabled = true;
		player.cursorItemIconID = ModContent.ItemType<PearlString>();
	}

	public override bool CreateDust(int i, int j, ref int type)
	{
		var tile = Framing.GetTileSafely(i, j);
		type = (tile.TileFrameY / 18) switch
		{
			1 => DustID.Corruption,
			2 => DustID.Crimson,
			3 => DustID.Pearlsand,
			_ => DustID.Sand,
		};

		return true;
	}

	public override void PostTileFrame(int i, int j, int up, int down, int left, int right, int upLeft, int upRight, int downLeft, int downRight)
	{
		int type = Framing.GetTileSafely(i, j + 1).TileType;

		switch (type)
		{
			case TileID.Sand:
				Framing.GetTileSafely(i, j).TileFrameY = 0;
				break;

			case TileID.Ebonsand:
				Framing.GetTileSafely(i, j).TileFrameY = 18;
				break;

			case TileID.Crimsand:
				Framing.GetTileSafely(i, j).TileFrameY = 36;
				break;

			case TileID.Pearlsand:
				Framing.GetTileSafely(i, j).TileFrameY = 54;
				break;
		}
	}

	public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
	{
		if (!TileExtensions.GetVisualInfo(i, j, out var color, out var texture))
			return false;

		var t = Framing.GetTileSafely(i, j);

		var source = new Rectangle(t.TileFrameX, t.TileFrameY, 16, 16);
		var offset = Lighting.LegacyEngine.Mode > 1 && Main.GameZoomTarget == 1 ? Vector2.Zero : Vector2.One * 12;
		var position = (new Vector2(i, j) + offset) * 16 - Main.screenPosition;

		spriteBatch.Draw(texture, position, source, color, 0, Vector2.Zero, 1, SpriteEffects.None, 0);
		spriteBatch.Draw(GlowmaskTile.TileIdToGlowmask[Type].Glowmask.Value, position, source, color * 2, 0, Vector2.Zero, 1, SpriteEffects.None, 0);

		var rect = new Rectangle(i * 16, j * 16, 16, 16);
		if (!Main.gamePaused && Main.rand.NextBool(50) && Main.LocalPlayer.Distance(rect.Center()) < 100) //Nearby dust effects
		{
			var dustPos = Main.rand.NextVector2FromRectangle(rect);

			var dust = Dust.NewDustPerfect(dustPos, DustID.SilverCoin, Scale: .2f);
			dust.noGravity = true;
			dust.velocity = Vector2.Zero;
			dust.noLightEmittence = true;

			ParticleHandler.SpawnParticle(new Particles.GlowParticle(dustPos, Vector2.Zero, Main.DiscoColor * .8f, Color.Black, .75f, 50));
		}

		return false;
	}
}