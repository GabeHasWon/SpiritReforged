using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Common.Visuals.RenderTargets;
using System.Runtime.CompilerServices;
using Terraria.DataStructures;

namespace SpiritReforged.Content.SaltFlats.Tiles;

public class SaltBlock : ModTile
{
	public override void SetStaticDefaults()
	{
		Main.tileSolid[Type] = true;
		Main.tileMergeDirt[Type] = true;
		Main.tileBlockLight[Type] = false;

		TileID.Sets.ChecksForMerge[Type] = true;

		AddMapEntry(new Color(230, 220, 220));
		this.Merge(TileID.IceBlock, TileID.SnowBlock, TileID.Sand);

		DustType = DustID.Pearlsand;
		MineResist = 0.5f;
	}

	public override void PostSetDefaults() => Main.tileNoSunLight[Type] = false;

	public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
	{
		SaltReflection.ReflectionPoints.Add(new(i, j));
		return true;
	}

	public override void ModifyFrameMerge(int i, int j, ref int up, ref int down, ref int left, ref int right, ref int upLeft, ref int upRight, ref int downLeft, ref int downRight)
		=> WorldGen.TileMergeAttempt(-2, TileID.SnowBlock, ref up, ref down, ref left, ref right, ref upLeft, ref upRight, ref downLeft, ref downRight);
}

public class SaltReflection : ILoadable
{
	public static readonly Asset<Texture2D> GradientMap = ModContent.Request<Texture2D>(DrawHelpers.RequestLocal(typeof(SaltReflection), "GradientMap"));

	public static bool Drawing { get; private set; }
	public static readonly HashSet<Point16> ReflectionPoints = [];

	public static ModTarget2D MapTarget { get; } = new(CanDraw, DrawMapTarget);
	public static ModTarget2D TileTarget { get; } = new(CanDraw, DrawTileTarget);
	public static ModTarget2D ReflectionTarget { get; } = new(CanDraw, DrawAndHandleReflectionTarget, false);

	[UnsafeAccessor(UnsafeAccessorKind.Method, Name = "DrawPlayers_AfterProjectiles")]
	private static extern void DrawPlayers_AfterProjectiles(Main main);

	[UnsafeAccessor(UnsafeAccessorKind.Method, Name = "DrawNPCs")]
	private static extern void DrawNPCs(Main main, bool behindTiles = false);

	[UnsafeAccessor(UnsafeAccessorKind.Method, Name = "DrawBG")]
	private static extern void DrawBG(Main main);

	public void Load(Mod mod)
	{
		DrawOverHandler.PostDrawTilesSolid += DrawFullReflection;
		TileEvents.AddPreDrawAction(true, ReflectionPoints.Clear);
	}

	private static bool CanDraw()
	{
		if (ReflectionPoints.Count > 0)
		{
			Drawing = true;
			return true;
		}

		return false;
	}

	private static void DrawMapTarget(SpriteBatch spriteBatch)
	{
		var texture = GradientMap.Value;

		foreach (var pt in ReflectionPoints)
		{
			int i = pt.X;
			int j = pt.Y;

			if (WorldGen.SolidOrSlopedTile(i, j - 1))
				continue;

			var t = Main.tile[i, j];
			if (t.IsHalfBlock)
			{
				spriteBatch.Draw(texture, new Vector2(i, j) * 16 - Main.screenPosition + new Vector2(0, 8), null, Color.White, 0, Vector2.Zero, 1, default, 0);
				continue;
			}
			else if (t.Slope != SlopeType.Solid)
			{
				for (int x = 0; x < 8; x++)
				{
					//Does not account for upside-down slopes
					Vector2 position = (t.Slope == SlopeType.SlopeDownLeft) ? new(2 * x, 2 * x) : new(2 * (7 - x), 2 * x);
					spriteBatch.Draw(texture, new Vector2(i, j) * 16 - Main.screenPosition + position, new Rectangle(0, 0, 2, texture.Height), Color.White, 0, Vector2.Zero, 1, default, 0);
				}

				continue;
			}

			spriteBatch.Draw(texture, new Vector2(i, j) * 16 - Main.screenPosition, null, Color.White, 0, Vector2.Zero, 1, default, 0);
		}
	}

	private static void DrawTileTarget(SpriteBatch spriteBatch)
	{
		var texture = TextureAssets.Tile[ModContent.TileType<SaltBlock>()].Value;

		foreach (var pt in ReflectionPoints)
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
	}

	private static void DrawAndHandleReflectionTarget(SpriteBatch spriteBatch)
	{
		var gd = Main.graphics.GraphicsDevice;

		var storedZoom = Main.GameViewMatrix.Zoom;
		Main.GameViewMatrix.Zoom = Vector2.One;

		gd.SetRenderTarget(ReflectionTarget.Target);
		gd.Clear(Color.Transparent);

		//Draw the actual contents
		spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.Transform);

		//spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, Rasterizer, null, transformationMatrix);
		DrawBG(Main.instance);
		DrawNPCs(Main.instance);

		spriteBatch.End();

		DrawPlayers_AfterProjectiles(Main.instance);

		gd.SetRenderTarget(null);

		Main.GameViewMatrix.Zoom = storedZoom;
	}

	private static void DrawFullReflection()
	{
		if (!Drawing || ReflectionTarget.Target is null || MapTarget.Target is null || TileTarget.Target is null)
			return;

		var s = AssetLoader.LoadedShaders["Reflection"];
		s.Parameters["mapTexture"].SetValue(MapTarget);
		s.Parameters["distortionTexture"].SetValue(AssetLoader.LoadedTextures["supPerlin"].Value);
		s.Parameters["tileTexture"].SetValue(TileTarget);
		s.Parameters["reflectionHeight"].SetValue(ReflectionTarget.Target.Height / 4f);
		s.Parameters["fade"].SetValue(3f);
		
		s.Parameters["noiseMult"].SetValue(new Vector2(1));
		s.Parameters["noiseStrength"].SetValue(new Vector2(0.5f, 0));

		Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, Main.Rasterizer, s, Main.Transform);

		Main.spriteBatch.Draw(ReflectionTarget, Vector2.Zero, null, new Color(255, 190, 200, 220) * 0.8f, 0, Vector2.Zero, 1, SpriteEffects.None, 0);
		Main.spriteBatch.End();

		Drawing = false;
	}

	public void Unload() { }
}