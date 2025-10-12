using SpiritReforged.Common.Misc;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.TileCommon.PostDrawTreeHookSystem;
using SpiritReforged.Common.TileCommon.TileSway;
using SpiritReforged.Common.WorldGeneration.Noise;
using SpiritReforged.Content.Forest.Stargrass.Tiles;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Forest.Stargrass;

internal class StargrassTreeGlowEffects : GlobalTile, IPostDrawTree
{
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
		if (IsStargrassTree(i, j, type))
			Lighting.AddLight(new Vector2(i, j).ToWorldCoordinates(), new Vector3(0.2f, 0.2f, 0.5f));
	}

	public override void DrawEffects(int i, int j, int type, SpriteBatch spriteBatch, ref TileDrawInfo drawData)
	{
		if (IsStargrassTree(i, j, type))
		{
			PostDrawTreeHook.AddPoint(new Point(i, j), this);
			CheckBranch(i - 1, j, type);
			CheckBranch(i + 1, j, type);
		}
	}

	private void CheckBranch(int i, int j, int type)
	{
		Tile tile = Main.tile[i, j];

		if (tile.TileFrameY >= 198)
		{
			if (tile.TileFrameX == 44 && IsStargrassTree(i + 1, j, type))
				PostDrawTreeHook.AddPoint(new Point(i, j), this);
			else if (tile.TileFrameX == 66 && IsStargrassTree(i - 1, j, type))
				PostDrawTreeHook.AddPoint(new Point(i, j), this);
		}
	}

	private static void DrawGlow(int i, int j, SpriteBatch spriteBatch)
	{
		Tile tile = Main.tile[i, j];
		var frame = new Rectangle(tile.TileFrameX, tile.TileFrameY, 16, 16);

		double lerp = Math.Sin(NoiseSystem.Perlin(i * 1.2f, j * 0.2f) * 5f + Main.GlobalTimeWrappedHourly) * .25f;
		Color color = (Color.White * (.3f - (float)lerp)).Additive();

		spriteBatch.Draw(_baseTexture.Value, TileExtensions.DrawPosition(i, j, TileExtensions.TileOffset), frame, color);

		if (tile.TileFrameY < 198)
			return;

		int treeFrame = WorldGen.GetTreeFrame(tile);

		if (tile.TileFrameX == 22)
		{
			int _ = 0;

			if (!WorldGen.GetCommonTreeFoliageData(i, j, 0, ref treeFrame, ref _, out _, out int topTextureFrameWidth3, out int topTextureFrameHeight3))
				return;

			Texture2D treeTopTexture = _topTexture.Value;
			Vector2 drawPos = TileExtensions.DrawPosition(i, j, TileExtensions.TileOffset - new Vector2(8, 16));
			float rotation = 0f;

			if (tile.WallType <= 0)
				rotation = Main.instance.TilesRenderer.GetWindCycle(i, j, TileSwaySystem.TreeWindCounter);

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
			var position = TileExtensions.DrawPosition(i, j, TileExtensions.TileOffset);
			float rotation = 0f;

			if (tile.WallType <= 0)
				rotation = Main.instance.TilesRenderer.GetWindCycle(i, j, TileSwaySystem.TreeWindCounter);

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

	private static bool IsStargrassTree(int i, int j, int type)
	{
		if (type == TileID.Trees)
		{
			while (Main.tile[i, j].TileType == TileID.Trees)
				j++;

			if (Main.tile[i, j].TileType == ModContent.TileType<StargrassTile>())
				return true;
		}

		return false;
	}

	void IPostDrawTree.PostDrawTree(int i, int j) => DrawGlow(i, j, Main.spriteBatch);
}
