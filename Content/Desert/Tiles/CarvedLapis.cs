using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.TileCommon.TileMerging;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Common.Visuals.RenderTargets;

namespace SpiritReforged.Content.Desert.Tiles;

public class CarvedLapis : ModTile, IAutoloadTileItem
{
	public class LapisGridOverlay : TileGridOverlay
	{
		public readonly ModTarget2D normalTarget;
		protected Texture2D _distanceMap;

		public LapisGridOverlay() => normalTarget = new(() => CanDraw, RenderNormalTarget);

		public Texture2D CreateTilemap(int width, int height)
		{
			if (_distanceMap != null && _distanceMap.Width == width && _distanceMap.Height == height)
				return _distanceMap;

			return _distanceMap = Reflections.CreateTilemap(width, height);
		}

		public override void RenderOverlayTarget(SpriteBatch spriteBatch)
		{
			GraphicsDevice gd = Main.graphics.GraphicsDevice;
			Vector2 storedZoom = Main.GameViewMatrix.Zoom;
			Main.GameViewMatrix.Zoom = Vector2.One;

			gd.SetRenderTarget(overlayTarget);
			gd.Clear(Color.Transparent);

			spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.Default, Main.Rasterizer, null);

			Reflections.DrawNPCs(Main.instance, true);
			Reflections.DrawNPCs(Main.instance, false);

			spriteBatch.End();

			Reflections.DrawPlayers_BehindNPCs(Main.instance);
			Reflections.DrawPlayers_AfterProjectiles(Main.instance);

			gd.SetRenderTarget(null);
			Main.GameViewMatrix.Zoom = storedZoom;
		}

		public virtual void RenderNormalTarget(SpriteBatch spriteBatch)
		{
			const float scale = 1;
			var gradient = CreateTilemap(16, 16 * 3);

			foreach (var pt in _grid)
			{
				int i = pt.X;
				int j = pt.Y;

				if (!_grid.Contains(new(i, j - 1)))
				{
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
							var position = t.Slope == SlopeType.SlopeDownLeft ? new Vector2(x, x) * 2 : new Vector2(6 - x, x) * 2;
							spriteBatch.Draw(gradient, new Vector2(i, j) * 16 - Main.screenPosition + position, source with { Width = 2 }, Color.White, 0, Vector2.Zero, scale, default, 0);
						}

						continue;
					}

					spriteBatch.Draw(gradient, new Vector2(i, j) * 16 - Main.screenPosition, source, Color.White, 0, Vector2.Zero, scale, default, 0);
				}

				TileMerger.DrawMerge(spriteBatch, i, j, Color.Black, Vector2.Zero, ModContent.TileType<RedSandstoneBrick>(), ModContent.TileType<RedSandstoneBrickCracked>(), ModContent.TileType<RedSandstoneSlab>());
			}
		}

		protected override void DrawContents(SpriteBatch spriteBatch)
		{
			if (normalTarget.Target != null && tileTarget.Target != null && overlayTarget.Target != null)
			{
				Effect s = AssetLoader.LoadedShaders["Reflection"].Value;
				Color tint = Color.SlateBlue.Additive(230) * 0.6f;

				s.Parameters["normalTexture"].SetValue(normalTarget);
				s.Parameters["tileTexture"].SetValue(tileTarget);
				s.Parameters["totalHeight"].SetValue(overlayTarget.Target.Height / 255f / 6f);

				spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, s, Main.Transform);
				spriteBatch.Draw(overlayTarget, Vector2.Zero, null, tint, 0, Vector2.Zero, 1, SpriteEffects.None, 0);
				spriteBatch.End();
			}
		}
	}

	private static LapisGridOverlay Overlay;

	public override void Load()
	{
		if (Overlay == null)
		{
			Overlay = new();
			DrawOverHandler.PostDrawTilesSolid += Overlay.Draw;
		}
	}

	public void AddItemRecipes(ModItem item) => item.CreateRecipe(25).AddIngredient(ItemID.Sapphire).AddTile(TileID.WorkBenches).Register();
	public override void SetStaticDefaults()
	{
		Main.tileSolid[Type] = true;
		Main.tileBlockLight[Type] = true;

		TileID.Sets.GeneralPlacementTiles[Type] = false;
		TileID.Sets.CanBeClearedDuringOreRunner[Type] = false;

		this.Merge(ModContent.TileType<RedSandstoneBrick>(), ModContent.TileType<RedSandstoneBrickCracked>(), ModContent.TileType<RedSandstoneSlab>());
		AddMapEntry(new Color(28, 67, 194));

		DustType = DustID.Cobalt;
		this.AutoItem().ResearchUnlockCount = 100;
	}

	public override void PostDraw(int i, int j, SpriteBatch spriteBatch) => TileMerger.DrawMerge(spriteBatch, i, j, ModContent.TileType<RedSandstoneBrick>(), ModContent.TileType<RedSandstoneBrickCracked>(), ModContent.TileType<RedSandstoneSlab>());
	public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
	{
		if (Reflections.Enabled)
			Overlay.AddToGrid(i, j);

		return true;
	}
}