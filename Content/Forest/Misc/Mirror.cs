using SpiritReforged.Common.Easing;
using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Common.Visuals.RenderTargets;
using Terraria.DataStructures;
using Terraria.GameContent.Drawing;
using TileHelper.Common;

namespace SpiritReforged.Content.Forest.Misc;

public sealed class Mirror : ModTile, ILoadItem
{
	#region special drawing
	private static readonly EasyTarget MirrorTarget = new(), FilterTarget = new();
	private static readonly HashSet<Point16> SpecialPositions = [];

	public override void Load()
	{
		TargetSetup.DrawIntoRendertargets += DrawContent;
		On_TileDrawing.PostDrawTiles += DrawReflection;
		On_TileDrawing.ClearCachedTileDraws += ClearSpecialDrawPoints;
	}

	private static void DrawContent()
	{
		GraphicsDevice graphics = Main.graphics.GraphicsDevice;
		SpriteBatch spriteBatch = Main.spriteBatch;

		#region mirror drawing
		Vector2 storedZoom = Main.GameViewMatrix.Zoom;
		Main.GameViewMatrix.Zoom = Vector2.One;

		graphics.SetRenderTarget(MirrorTarget.Value);
		graphics.Clear(Color.Transparent);

		Reflections.DrawPlayers_BehindNPCs(Main.instance);
		Reflections.DrawPlayers_AfterProjectiles(Main.instance);
		Main.GameViewMatrix.Zoom = storedZoom;

		spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.Default, Main.Rasterizer, null);

		foreach (Point16 position in SpecialPositions)
			DrawNoise(spriteBatch, position);

		spriteBatch.End();
		graphics.SetRenderTarget(null);
		#endregion

		#region filter drawing
		graphics.SetRenderTarget(FilterTarget.Value);
		graphics.Clear(Color.Transparent);

		spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.Default, Main.Rasterizer, null);
		Texture2D tileTexture = TextureAssets.Tile[ModContent.TileType<Mirror>()].Value;

		foreach (Point16 position in SpecialPositions)
		{
			Rectangle tileSource = tileTexture.Frame(2, 2, 1, Framing.GetTileSafely(position).TileFrameY / 74); //74 is the tile frame height
			spriteBatch.Draw(tileTexture, position.ToVector2() * 16 - Main.screenPosition + new Vector2(10, 5), tileSource, Color.White * 0.7f, 0, Vector2.Zero, 1, 0, 0);
		}

		spriteBatch.End();
		graphics.SetRenderTarget(null);
		#endregion

		static void DrawNoise(SpriteBatch spriteBatch, Point16 coordinates)
		{
			const float scale = 1;

			float scroll = EaseFunction.EaseCubicInOut.Ease((float)(Main.timeForVisualEffects + coordinates.X * coordinates.Y) / 150f % 1);
			Texture2D noise = AssetLoader.LoadedTextures["MirrorShine"].Value;
			Color color = Lighting.GetColor(coordinates.X, coordinates.Y).Additive();
			Rectangle source = new(0, 0, 36, noise.Height);

			for (int y = 0; y < 2; y++)
			{
				Vector2 position = coordinates.ToWorldCoordinates() - Main.screenPosition + new Vector2(0, noise.Height * scale * (y - scroll));

				spriteBatch.Draw(noise, position, source, color * 0.7f, 0, Vector2.Zero, scale, default, 0);
				spriteBatch.Draw(noise, position + new Vector2(0, 10), source, color * 0.3f, 0, Vector2.Zero, scale, default, 0);
				spriteBatch.Draw(noise, position - new Vector2(0, 8), source, color * 0.2f, 0, Vector2.Zero, scale, default, 0);
			}
		}
	}

	private static void DrawReflection(On_TileDrawing.orig_PostDrawTiles orig, TileDrawing self, bool solidLayer, bool forRenderTargets, bool intoRenderTargets)
	{
		orig(self, solidLayer, forRenderTargets, intoRenderTargets);

		if (!solidLayer && !intoRenderTargets && MirrorTarget?.Value != null)
		{
			SpriteBatch spriteBatch = Main.spriteBatch;
			Effect effect = AssetLoader.LoadedShaders["SimpleMultiply"].Value;

			effect.Parameters["tileTexture"].SetValue(FilterTarget.Value);
			effect.Parameters["lightness"].SetValue(10);

			spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, effect, Main.Transform);
			spriteBatch.Draw(MirrorTarget.Value, -new Vector2(10, 5), null, Color.White, 0, Vector2.Zero, 1, 0, 0);
			spriteBatch.End();
		}
	}

	private static void ClearSpecialDrawPoints(On_TileDrawing.orig_ClearCachedTileDraws orig, TileDrawing self, bool solidLayer)
	{
		orig(self, solidLayer);

		if (!solidLayer)
			SpecialPositions.Clear();
	}
	#endregion

	void ILoadItem.AddItemRecipes(ModItem modItem) => modItem.CreateRecipe().AddRecipeGroup(RecipeGroupID.Wood, 10).AddIngredient(ItemID.Glass, 5).AddTile(TileID.Sawmill).Register();

	public override void SetStaticDefaults()
	{
		Main.tileTable[Type] = true;
		Main.tileFrameImportant[Type] = true;
		Main.tileNoAttach[Type] = true;
		Main.tileLavaDeath[Type] = true;
		Main.tileLighted[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
		TileObjectData.newTile.Height = 4;
		TileObjectData.newTile.CoordinateHeights = [16, 16, 16, 18];
		TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop, TileObjectData.newTile.Width, 0);
		TileObjectData.newTile.Origin = new(0, 3);

		TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile);
		TileObjectData.newAlternate.AnchorWall = true;
		TileObjectData.newAlternate.AnchorBottom = AnchorData.Empty;
		TileObjectData.addAlternate(1);
		TileObjectData.addTile(Type);

		AddMapEntry(FurnitureTile.MapColor, this.AutoModItem().DisplayName);
		DustType = -1;
	}

	public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
	{
		if (TileObjectData.IsTopLeft(i, j))
			SpecialPositions.Add(new(i, j));

		return true;
	}
}