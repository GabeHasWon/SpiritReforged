using SpiritReforged.Common.TileCommon;
using Terraria.GameContent.Drawing;

namespace SpiritReforged.Content.Underground.Tiles;

public class GlowTileHandler : ILoadable
{
	private static readonly Dictionary<Rectangle, Color> GlowPoints = [];
	/// <summary> Adds a fancy glow effect at the given tile region. Normally called in <see cref="ModBlockType.PreDraw"/>. </summary>
	/// <param name="r"> The tile coordinate area to draw at. </param>
	public static void AddGlowPoint(Rectangle r, Color color = default) => GlowPoints.Add(r, color);
	/// <inheritdoc cref="AddGlowPoint(Rectangle, Color)"/>
	public static void AddGlowPoint(Rectangle r, Color color = default, float proximity = 200)
	{
		var world = r.Location.ToWorldCoordinates(r.Width / 2, r.Height / 2);
		float value = Main.LocalPlayer.DistanceSQ(world) / (proximity * proximity);

		if (value > 1)
			return;

		float lighting = Math.Min(Lighting.Brightness(r.X, r.Y) * 3f, 1);
		color *= (1f - value) * lighting;

		GlowPoints.TryAdd(r, color);
	}

	public void Load(Mod mod)
	{
		DrawOrderSystem.DrawTilesNonSolidEvent += DrawGlow;
		On_TileDrawing.PreDrawTiles += ClearAll;
	}

	private static void DrawGlow()
	{
		foreach (var p in GlowPoints.Keys)
		{
			var world = p.Location.ToWorldCoordinates(p.Width / 2, p.Height / 2 + 2);
			DrawGlow(world - Main.screenPosition, GlowPoints[p], p.Width, p.Height);
		}
	}

	private static void ClearAll(On_TileDrawing.orig_PreDrawTiles orig, TileDrawing self, bool solidLayer, bool forRenderTargets, bool intoRenderTargets)
	{
		orig(self, solidLayer, forRenderTargets, intoRenderTargets);

		bool flag = intoRenderTargets || Lighting.UpdateEveryFrame;

		if (!solidLayer && flag)
			GlowPoints.Clear();
	}

	/// <summary> Draws a fancy glow effect at the given tile region. <para/>
	/// If a point doesn't need to be queued using <see cref="AddGlowPoint"/> due to batching, call this directly.</summary>
	public static void DrawGlow(Vector2 drawPosition, Color color, int width, int height)
	{
		const int spread = 10;

		var region = new Rectangle((int)drawPosition.X - width / 2, (int)drawPosition.Y - height / 2, width, height);
		var c = Color.White;

		short[] indices = [0, 1, 2, 1, 3, 2];

		//Note that corner positions are reversed to flip the effect
		VertexPositionColorTexture[] vertices =
		[
			new(new Vector3(region.BottomRight(), 0), c, new Vector2(0, 0)),
			new(new Vector3(region.BottomLeft(), 0), c, new Vector2(1, 0)),
			new(new Vector3(region.TopRight() + new Vector2(spread, 0), 0), c, new Vector2(0, 1)),
			new(new Vector3(region.TopLeft() - new Vector2(spread, 0), 0), c, new Vector2(1, 1)),
		];

		var projection = Matrix.CreateOrthographicOffCenter(0, Main.screenWidth, Main.screenHeight, 0, -1, 1);
		Matrix view = Main.GameViewMatrix.TransformationMatrix;
		Effect effect = AssetLoader.LoadedShaders["ShadowFade"];

		foreach (EffectPass pass in effect.CurrentTechnique.Passes)
		{
			effect.Parameters["baseShadowColor"].SetValue(color.ToVector4());
			effect.Parameters["adjustColor"].SetValue(Color.White.ToVector4());
			effect.Parameters["noiseScroll"].SetValue(Main.GameUpdateCount * 0.0015f);
			effect.Parameters["noiseStretch"].SetValue(.5f);
			effect.Parameters["uWorldViewProjection"].SetValue(view * projection);
			effect.Parameters["noiseTexture"].SetValue(AssetLoader.LoadedTextures["vnoise"].Value);
			pass.Apply();

			Main.instance.GraphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, vertices, 0, 4, indices, 0, 2);
		}
	}

	public void Unload() { }
}