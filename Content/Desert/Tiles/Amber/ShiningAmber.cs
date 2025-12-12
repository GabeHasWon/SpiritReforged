using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.TileCommon.TileMerging;
using SpiritReforged.Common.Visuals.RenderTargets;
using SpiritReforged.Content.Particles;

namespace SpiritReforged.Content.Desert.Tiles.Amber;

public abstract class ShiningAmber : ModTile
{
	public sealed class AmberGridOverlay : TileGridOverlay
	{
		public override void RenderTileTarget(SpriteBatch spriteBatch) => PrepareDefault.Invoke(spriteBatch, tileTarget, () =>
		{
			foreach (var pt in _grid)
			{
				TileExtensions.DrawSingleTile(pt.X, pt.Y, true, Vector2.Zero);
				TileMerger.DrawMerge(spriteBatch, pt.X, pt.Y, Color.Black, Vector2.Zero, TileID.Sand);
			}
		});

		public override void RenderOverlayTarget(SpriteBatch spriteBatch) => PrepareDefault.Invoke(spriteBatch, overlayTarget, () =>
		{
			const float scale = 4;

			var noise = TextureAssets.Extra[ExtrasID.MagicMissileTrailErosion].Value;
			float scroll = (float)Main.timeForVisualEffects / 4000f % 1;
			float opacity = (0.5f + (float)Math.Sin(Main.timeForVisualEffects / 100f) * 0.1f) * 0.25f;

			for (int x = 0; x < Main.screenWidth / (noise.Width * scale) + 1; x++)
			{
				for (int y = 0; y < Main.screenHeight / (noise.Height * scale) + 1; y++)
				{
					var position = new Vector2(noise.Width * scale * (x - scroll), noise.Height * scale * (y - scroll));
					spriteBatch.Draw(noise, position, null, (Color.Goldenrod * opacity).Additive(), 0, Vector2.Zero, scale, default, 0);
				}
			}
		});

		protected override void DrawContents(SpriteBatch spriteBatch)
		{
			var s = AssetLoader.LoadedShaders["SimpleMultiply"].Value;
			s.Parameters["tileTexture"].SetValue(tileTarget);
			s.Parameters["lightness"].SetValue(100);

			spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.Default, Main.Rasterizer, s, Main.GameViewMatrix.TransformationMatrix);

			spriteBatch.Draw(overlayTarget, Vector2.Zero, null, Color.White, 0, Vector2.Zero, 1, default, 0);
			spriteBatch.End();
		}
	}

	private static AmberGridOverlay Overlay;

	public override void Load()
	{
		if (Overlay == null)
		{
			Overlay = new();
			DrawOverHandler.PostDrawTilesSolid += Overlay.Draw;
		}
	}

	public override void SetStaticDefaults()
	{
		Main.tileSolid[Type] = true;
		Main.tileMergeDirt[Type] = true;
		Main.tileBlockLight[Type] = false;
		Main.tileLighted[Type] = true;
		TileID.Sets.CanBeClearedDuringOreRunner[Type] = false;

		AddMapEntry(Color.Orange);
		this.Merge(ModContent.TileType<PolishedAmber>(), ModContent.TileType<AmberFossil>(), ModContent.TileType<AmberFossilSafe>(), TileID.Sand);

		DustType = DustID.GemAmber;
		MineResist = 0.5f;
	}

	public override bool TileFrame(int i, int j, ref bool resetFrame, ref bool noBreak) => TileFraming.Gemspark(i, j, resetFrame);
	public override void NearbyEffects(int i, int j, bool closer)
	{
		if (closer && !Main.gamePaused && Main.rand.NextBool(3000))
		{
			Vector2 coords = new Vector2(i, j) * 16;
			Vector2 position = Main.rand.NextVector2FromRectangle(new((int)coords.X, (int)coords.Y, 16, 16));

			float scale = Main.rand.NextFloat(0.2f, 0.5f);
			ParticleHandler.SpawnParticle(new GlowParticle(position, Vector2.UnitY * -0.3f, Color.Goldenrod * 0.5f, scale, 200));
		}
	}

	public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
	{
		if (!TileExtensions.GetVisualInfo(i, j, out Color color, out _))
			return false;

		Overlay.AddToGrid(i, j);

		TileExtensions.DrawSingleTile(i, j, true, TileExtensions.TileOffset);
		TileMerger.DrawMerge(spriteBatch, i, j, color, TileExtensions.TileOffset, TileID.Sand);
		return false;
	}
}