using SpiritReforged.Common.Misc;
using SpiritReforged.Common.PrimitiveRendering;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Common.Visuals.RenderTargets;
using Terraria.DataStructures;
using Terraria.Graphics;

namespace SpiritReforged.Content.SaltFlats.Tiles.Salt;

public class SaltBlockReflective : SaltBlock
{
	public sealed class SaltGridOverlay : TileGridOverlay
	{
		private static Vector2 Origin;

		public readonly ModTarget2D normalTarget;
		private Texture2D _distanceMap;

		public SaltGridOverlay() => normalTarget = new(() => CanDraw, RenderNormalTarget);

		/// <summary> Gets a gradient texture for shader mapping. </summary>
		/// <param name="width"> The pre-upscaled width of the texture. </param>
		/// <param name="height"> The pre-upscaled height of the texture.</param>
		public Texture2D CreateTilemap(int width, int height)
		{
			if (_distanceMap != null && _distanceMap.Width == width && _distanceMap.Height == height)
				return _distanceMap;

			return _distanceMap = Reflections.CreateTilemap(width, height);
		}

		public override void RenderTileTarget(SpriteBatch spriteBatch) => PrepareDefault.Invoke(spriteBatch, tileTarget, () =>
		{
			var texture = TileMap.Value;

			foreach (var pt in _grid)
			{
				int i = pt.X;
				int j = pt.Y;

				var t = Main.tile[i, j];
				var source = new Rectangle(t.TileFrameX, t.TileFrameY, 16, 16);

				if (t.Slope != SlopeType.Solid || t.IsHalfBlock)
				{
					TileExtensions.DrawSloped(i, j, texture, Color.White, Vector2.Zero);
					continue;
				}

				spriteBatch.Draw(texture, new Vector2(i, j) * 16 - Main.screenPosition, source, Color.White, 0, Vector2.Zero, 1, default, 0);
			}
		});

		public override void RenderOverlayTarget(SpriteBatch spriteBatch)
		{
			GraphicsDevice gd = Main.graphics.GraphicsDevice;
			Vector2 storedZoom = Main.GameViewMatrix.Zoom;
			Main.GameViewMatrix.Zoom = Vector2.One;

			gd.SetRenderTarget(overlayTarget);
			gd.Clear(Color.Transparent);
			spriteBatch.BeginDefault();

			Main.tileBatch.Begin();
			DrawSimpleGradient(new Color(129, 118, 225) * 0.8f, new(147, 160, 255), new(210, 190, 220));
			Main.tileBatch.End();

			foreach (Cloud c in Main.cloud)
			{
				if (c.active)
				{
					float offset = c.position.Y * (Main.screenHeight / 600f) + 200f;
					DrawForegroudCloud(c, c.cloudColor(Main.ColorOfTheSkies) * 0.3f, offset);
				}
			}

			if (Reflections.Detail > 1)
			{
				if (!Reflections.HighResolution)
					spriteBatch.Draw(Main.instance.wallTarget, Main.sceneWallPos - Main.screenPosition, Color.White);

				Reflections.DrawNPCs(Main.instance, true);

				if (!Reflections.HighResolution)
				{
					spriteBatch.Draw(Main.instance.tileTarget, Main.sceneTilePos - Main.screenPosition, Color.White);
					spriteBatch.Draw(Main.instance.tile2Target, Main.sceneTile2Pos - Main.screenPosition, Color.White);

					if (Reflections.Detail > 1)
					{
						spriteBatch.End();
						Main.instance.TilesRenderer.PostDrawTiles(false, false, false);
						spriteBatch.BeginDefault();
					}
				}
			}

			Reflections.DrawNPCs(Main.instance, false);

			if (Reflections.Detail > 2)
			{
				Reflections.DrawGore(Main.instance);
				Main.instance.DrawItems();
			}

			spriteBatch.End();

			if (Reflections.Detail > 2)
			{
				Reflections.DrawDust(Main.instance);
				Reflections.DrawProjectiles(Main.instance);
			}

			Reflections.DrawPlayers_BehindNPCs(Main.instance);
			Reflections.DrawPlayers_AfterProjectiles(Main.instance);

			gd.SetRenderTarget(null);
			Main.GameViewMatrix.Zoom = storedZoom;
		}

		public void RenderNormalTarget(SpriteBatch spriteBatch)
		{
			Vector2 scale = Vector2.One;
			var gradient = CreateTilemap(16, 255 * 3);

			foreach (var pt in _grid)
			{
				int i = pt.X;
				int j = pt.Y;

				if (_grid.Contains(new(i, j - 1)))
					continue;

				Rectangle source = new(0, 0, gradient.Width, gradient.Height);
				Tile t = Main.tile[i, j];

				if (t.IsHalfBlock)
				{
					spriteBatch.Draw(gradient, new Vector2(i, j) * 16 - Main.screenPosition + new Vector2(0, 8), source, Color.White, 0, Vector2.Zero, scale, default, 0);
					continue;
				}
				else if (t.Slope is SlopeType.SlopeDownLeft or SlopeType.SlopeDownRight)
				{
					for (int x = 0; x < 8; x++)
					{
						Vector2 position = (t.Slope == SlopeType.SlopeDownLeft) ? new Vector2(x, x) * 2 : new Vector2(7 - x, x) * 2;
						spriteBatch.Draw(gradient, new Vector2(i, j) * 16 - Main.screenPosition + position, source with { Width = 2 }, Color.White, 0, Vector2.Zero, scale, default, 0);
					}

					continue;
				}

				spriteBatch.Draw(gradient, new Vector2(i, j) * 16 - Main.screenPosition, source, Color.White, 0, Vector2.Zero, scale, default, 0);
			}
		}

		protected override void DrawContents(SpriteBatch spriteBatch)
		{
			var s = AssetLoader.LoadedShaders["Reflection"].Value;

			s.Parameters["normalTexture"].SetValue(normalTarget);
			s.Parameters["tileTexture"].SetValue(tileTarget);
			s.Parameters["totalHeight"].SetValue(overlayTarget.Target.Height / 255f / 6f);
			ShaderHelpers.SetEffectMatrices(ref s);

			spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, s, Main.Transform);

			Color tint = Color.White * 0.9f;
			spriteBatch.Draw(overlayTarget, Vector2.Zero, null, tint, 0, Vector2.Zero, 1, SpriteEffects.None, 0);
			spriteBatch.End();
		}

		/// <summary> Adapted from <see cref="Main.DrawSimpleSurfaceBackground"/>. </summary>
		private static void DrawSimpleGradient(params Color[] colors)
		{
			int samples = colors.Length - 1;

			float areaWidth = Main.screenWidth;
			float areaHeight = Main.screenHeight;
			float divHeight = areaHeight / samples;

			var skyColor = Main.ColorOfTheSkies.ToVector4();

			for (int i = 0; i < samples; i++)
			{
				Color startColor = colors[i];
				Color endColor = colors[i + 1];

				Color topColor = new(startColor.ToVector4() * skyColor);
				Color bottomColor = new(endColor.ToVector4() * skyColor);

				VertexColors vertexColors = new()
				{
					TopLeftColor = topColor,
					TopRightColor = topColor,
					BottomLeftColor = bottomColor,
					BottomRightColor = bottomColor
				};

				Main.tileBatch.Draw(TextureAssets.BlackTile.Value, new Vector4(0f, divHeight * i, areaWidth, divHeight), vertexColors);
			}
		}

		/// <summary> Draws Cloud. </summary>
		/// <param name="cloud">The cloud to draw.</param>
		/// <param name="color">The color to draw the cloud in.</param>
		/// <param name="yOffset">The vertical offset of the cloud.</param>
		/// <param name="index">The index of the cloud in <see cref="Main.cloud"/> if applicable.</param>
		public static void DrawForegroudCloud(Cloud cloud, Color color, float yOffset, SpriteEffects effects = default, int index = -1)
		{
			Texture2D texture = TextureAssets.Cloud[cloud.type].Value;
			Vector2 position = new(cloud.position.X + texture.Width * 0.5f, yOffset + texture.Height * 0.5f);
			Rectangle sourceRectangle = new(0, 0, texture.Width, texture.Height);
			float rotation = cloud.rotation;
			Vector2 origin = texture.Size() / 2;
			float scale = cloud.scale;
			DrawData drawData = new(texture, position, sourceRectangle, color, rotation, origin, scale, effects);

			ModCloud modCloud = cloud.ModCloud;
			if (modCloud == null || index == -1 || modCloud.Draw(Main.spriteBatch, cloud, index, ref drawData))
			{
				drawData.Draw(Main.spriteBatch);
			}
		}
	}

	public static readonly Asset<Texture2D> TileMap = DrawHelpers.RequestLocal(typeof(SaltBlockReflective), "SaltBlockReflectiveMap", false);
	private static SaltGridOverlay Overlay;

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
		base.SetStaticDefaults();
		AddMapEntry(new Color(230, 220, 220));
	}

	public override void PostSetDefaults() => Main.tileNoSunLight[Type] = false;
	public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
	{
		if (Reflections.Enabled)
			Overlay.AddToGrid(i, j);

		return true;
	}

	public override void ModifyFrameMerge(int i, int j, ref int up, ref int down, ref int left, ref int right, ref int upLeft, ref int upRight, ref int downLeft, ref int downRight)
		=> WorldGen.TileMergeAttempt(-2, ModContent.TileType<SaltBlockDull>(), ref up, ref down, ref left, ref right, ref upLeft, ref upRight, ref downLeft, ref downRight);
}