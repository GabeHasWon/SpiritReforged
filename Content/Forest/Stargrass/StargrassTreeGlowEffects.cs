using SpiritReforged.Common.Misc;
using SpiritReforged.Common.ModCompat;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.TileCommon.PostDrawTreeHookSystem;
using SpiritReforged.Common.WorldGeneration.Noise;
using SpiritReforged.Content.Crossmod.Spooky.SpookyForest.Plants;
using SpiritReforged.Content.Forest.Stargrass.Tiles;
using Terraria.DataStructures;
using TileHelper.Common;

namespace SpiritReforged.Content.Forest.Stargrass;

internal class StargrassTreeGlowEffects : GlobalTile, IPostDrawTree
{
	internal class StarscareGlowEffects : ILoadable, IPostDrawTree
	{
		private static Asset<Texture2D> _topTex;
		private static Asset<Texture2D> _branchTex;

		public void Load(Mod mod)
		{
			_topTex = ModContent.Request<Texture2D>($"{StarscareTree.Path}TopsGlow");
			_branchTex = ModContent.Request<Texture2D>($"{StarscareTree.Path}BranchesGlow");
		}

		public void Unload() { }
		void IPostDrawTree.PostDrawTree(int i, int j) => DrawGlow(i, j, Main.spriteBatch, null, _topTex.Value, _branchTex.Value);
	}

	internal class StarscareGlowGreenEffects : ILoadable, IPostDrawTree
	{
		private static Asset<Texture2D> _topTex;
		private static Asset<Texture2D> _branchTex;

		public void Load(Mod mod)
		{
			_topTex = ModContent.Request<Texture2D>($"{StarscareTree.Path}GreenTopsGlow");
			_branchTex = ModContent.Request<Texture2D>($"{StarscareTree.Path}GreenBranchesGlow");
		}

		public void Unload() { }
		void IPostDrawTree.PostDrawTree(int i, int j) => DrawGlow(i, j, Main.spriteBatch, null, _topTex.Value, _branchTex.Value);
	}

	public enum GlowTreeType
	{
		Stargrass,
		Spooky_Starscare,
		Spooky_StarscareGreen
	}

	private static Asset<Texture2D> _baseTexture;
	private static Asset<Texture2D> _topTexture;
	private static Asset<Texture2D> _branchTexture;

	public override void Load()
	{
		_baseTexture = ModContent.Request<Texture2D>(StargrassTree.TexturePath + "_Glow");
		_topTexture = ModContent.Request<Texture2D>($"{StargrassTree.TexturePath}_Tops_Glow");
		_branchTexture = ModContent.Request<Texture2D>($"{StargrassTree.TexturePath}_Branches_Glow");
	}

	public override void NearbyEffects(int i, int j, int type, bool closer)
	{
		if (IsStargrassTree(i, j, type, out GlowTreeType treeType))
		{
			Tile tile = Main.tile[i, j];
			Vector3 glow = treeType switch
			{
				GlowTreeType.Spooky_Starscare => new Vector3(0.45f, 0.2f, 0.2f),
				GlowTreeType.Spooky_StarscareGreen => new Vector3(0.15f, 0.48f, 0.15f),
				_ => new Vector3(0.2f, 0.2f, 0.5f),
			};

			if (tile.TileFrameX is 44 or 66 && treeType != GlowTreeType.Stargrass)
				glow *= 0.4f;

			Lighting.AddLight(new Vector2(i, j).ToWorldCoordinates(), glow);
		}
	}

	public override void DrawEffects(int i, int j, int type, SpriteBatch spriteBatch, ref TileDrawInfo drawData)
	{
		if (IsStargrassTree(i, j, type, out GlowTreeType treeType))
		{
			PostDrawTreeHook.AddPoint(new Point(i, j), GetTreeDrawer(treeType));
			CheckBranch(i - 1, j, type);
			CheckBranch(i + 1, j, type);
		}
	}

	private static void CheckBranch(int i, int j, int type)
	{
		Tile tile = Main.tile[i, j];

		if (tile.TileFrameY >= 198)
		{
			if (tile.TileFrameX == 44 && IsStargrassTree(i + 1, j, type, out GlowTreeType treeType))
				PostDrawTreeHook.AddPoint(new Point(i, j), GetTreeDrawer(treeType));
			else if (tile.TileFrameX == 66 && IsStargrassTree(i - 1, j, type, out GlowTreeType treeType2))
				PostDrawTreeHook.AddPoint(new Point(i, j), GetTreeDrawer(treeType2));
		}
	}

	public static IPostDrawTree GetTreeDrawer(GlowTreeType type) => type switch
	{
		GlowTreeType.Stargrass => ModContent.GetInstance<StargrassTreeGlowEffects>(),
		GlowTreeType.Spooky_Starscare => new StarscareGlowEffects(),
		GlowTreeType.Spooky_StarscareGreen => new StarscareGlowGreenEffects(),
		_ => throw new Exception("How did you get here?")
	};

	private static void DrawGlow(int i, int j, SpriteBatch spriteBatch, Texture2D trunkTexture, Texture2D topsTexture, Texture2D branchTexture)
	{
		Tile tile = Main.tile[i, j];
		var frame = new Rectangle(tile.TileFrameX, tile.TileFrameY, 16, 16);

		double lerp = Math.Sin(NoiseSystem.Perlin(i * 1.2f, j * 0.2f) * 5f + Main.GlobalTimeWrappedHourly) * 0.25f;
		Color color = (Color.White * (0.3f - (float)lerp)).Additive();

		Vector2 baseDrawPos = new Vector2(i + 1, j + 2) * 16f - Main.screenPosition;
		spriteBatch.Draw(_baseTexture.Value, baseDrawPos, frame, color);

		if (tile.TileFrameY < 198)
			return;

		int treeFrame = WorldGen.GetTreeFrame(tile);

		if (tile.TileFrameX == 22)
		{
			int _ = 0;

			if (!WorldGen.GetCommonTreeFoliageData(i, j, 0, ref treeFrame, ref _, out _, out int topTextureFrameWidth3, out int topTextureFrameHeight3))
				return;

			Texture2D treeTopTexture = _topTexture.Value;
			Vector2 drawPos = baseDrawPos - new Vector2(8, 16);
			float rotation = 0f;

			if (tile.WallType <= WallID.None)
				rotation = Main.instance.TilesRenderer.GetWindCycle(i + 1, j + 2, WindTileRenderer.TreeWindCounter - MathHelper.PiOver4);

			drawPos.X += rotation * 2f;
			drawPos.Y += Math.Abs(rotation) * 2f;

			var source = new Rectangle(treeFrame * (topTextureFrameWidth3 + 2), 0, topTextureFrameWidth3, topTextureFrameHeight3);
			var origin = new Vector2(topTextureFrameWidth3 / 2, topTextureFrameHeight3);

			Main.spriteBatch.Draw(treeTopTexture, drawPos, source, color, rotation * 0.08f, origin, 1f, SpriteEffects.None, 0f);
		}
		else
		{
			int _ = 0;

			if (!WorldGen.GetCommonTreeFoliageData(i, j, -1, ref treeFrame, ref _, out _, out int _, out int _))
				return;

			Texture2D treeBranchTexture = _branchTexture.Value;
			Vector2 position = baseDrawPos;
			float rotation = 0f;

			if (tile.WallType <= WallID.None)
				rotation = Main.instance.TilesRenderer.GetWindCycle(i, j, WindTileRenderer.TreeWindCounter);

			if (rotation < 0f)
				position.X += rotation;

			if (tile.TileFrameX == 44)
			{
				position.X += Math.Abs(rotation) * 2f;
				position += new Vector2(16f, 12f);

				var origin = new Vector2(40f, 24f);
				var source = new Rectangle(0, treeFrame * 42, 40, 40);

				Main.spriteBatch.Draw(treeBranchTexture, position, source, color, rotation * 0.06f, origin, 1f, SpriteEffects.None, 0f);
			}
			else if (tile.TileFrameX == 66)
			{
				position.X -= Math.Abs(rotation) * 2f;
				position += new Vector2(0, 18);

				var origin = new Vector2(0f, 30f);
				var source = new Rectangle(42, treeFrame * 42, 40, 40);

				Main.spriteBatch.Draw(treeBranchTexture, position, source, color, rotation * 0.06f, origin, 1f, SpriteEffects.None, 0f);
			}
		}
	}

	private static bool IsStargrassTree(int i, int j, int type, out GlowTreeType treeType)
	{
		treeType = GlowTreeType.Stargrass;

		if (type == TileID.Trees)
		{
			while (Main.tile[i, j].TileType == TileID.Trees)
				j++;

			if (Main.tile[i, j].TileType == ModContent.TileType<StargrassTile>())
				return true;

			if (CrossMod.Spooky.Enabled)
			{
				if (Main.tile[i, j].TileType == ModContent.TileType<OrangeSpookyStargrass>())
				{
					treeType = GlowTreeType.Spooky_Starscare;
					return true;
				}

				if (Main.tile[i, j].TileType == ModContent.TileType<GreenSpookyStargrass>())
				{
					treeType = GlowTreeType.Spooky_StarscareGreen;
					return true;
				}
			}
		}

		return false;
	}

	void IPostDrawTree.PostDrawTree(int i, int j) => DrawGlow(i, j, Main.spriteBatch, _baseTexture.Value, _topTexture.Value, _branchTexture.Value);
}
