using SpiritReforged.Common.Particle;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.TileCommon.DrawPreviewHook;
using SpiritReforged.Common.Visuals.Glowmasks;
using SpiritReforged.Content.Particles;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Forest.Misc.Bonsai;

[AutoloadGlowmask("255,255,255")]
public class BonsaiTrees : ModTile, IDrawPreview
{
	public const int FrameWidth = 60;
	public const int FrameHeight = 72;

	public override void SetStaticDefaults()
	{
		Main.tileFrameImportant[Type] = true;
		Main.tileNoAttach[Type] = true;
		Main.tileLavaDeath[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style3x4);
		TileObjectData.newTile.CoordinateWidth = 18;
		TileObjectData.newTile.Origin = new(1, 3);
		TileObjectData.newTile.Direction = TileObjectDirection.PlaceRight;
		TileObjectData.newTile.StyleWrapLimit = 2; 
		TileObjectData.newTile.StyleMultiplier = 2; 
		TileObjectData.newTile.StyleHorizontal = true;
		TileObjectData.newTile.StyleMultiplier = 2;
		TileObjectData.newTile.StyleWrapLimit = 2;
		TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile);
		TileObjectData.newAlternate.Direction = TileObjectDirection.PlaceLeft; 
		TileObjectData.addAlternate(1); 
		TileObjectData.addTile(Type);

		DustType = -1;
		AddMapEntry(new Color(140, 140, 140), CreateMapEntryName());
	}

	public override void NearbyEffects(int i, int j, bool closer)
	{
		const int fluff = 8;

		if (closer && !Main.gamePaused && Main.tile[i, j].TileFrameY is short frameY && frameY > FrameHeight * 2 && TileObjectData.IsTopLeft(i, j) && Main.rand.NextBool(8))
		{
			Color color = (frameY / FrameHeight) switch
			{
				3 => Color.Red,
				4 => Color.White,
				5 => Color.Green,
				6 => Color.Blue,
				7 => Color.Blue,
				8 => Color.Purple,
				_ => Color.Goldenrod
			};

			int width = 48 - fluff * 2;
			int height = 38;
			var position = new Vector2(i, j) * 16 + new Vector2(fluff);

			float scale = Main.rand.NextFloat(0.2f, 0.7f);
			int timeLeft = Main.rand.Next(15, 30);

			var rectangle = Main.rand.NextVector2FromRectangle(new((int)position.X, (int)position.Y, width, height));

			ParticleHandler.SpawnParticle(new EmberParticle(rectangle, Vector2.Zero, color, scale, timeLeft, 3));
		}
	}

	public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
	{
		if (!TileExtensions.GetVisualInfo(i, j, out var color, out var texture))
			return false;

		var t = Main.tile[i, j];
		var frame = new Point(t.TileFrameX, t.TileFrameY);

		int offsetX = (frame.X % FrameWidth == 0) ? -2 : ((frame.X % FrameWidth == 40) ? 2 : 0);

		var source = new Rectangle(frame.X, frame.Y, 18, 16);
		var position = new Vector2(i, j) * 16 - Main.screenPosition + TileExtensions.TileOffset + new Vector2(offsetX, 2);

		spriteBatch.Draw(texture, position, source, color, 0, Vector2.Zero, 1, SpriteEffects.None, 0);
		spriteBatch.Draw(GlowmaskTile.TileIdToGlowmask[Type].Glowmask.Value, position, source, color * 3, 0, Vector2.Zero, 1, SpriteEffects.None, 0);
		return false;
	}

	public void DrawPreview(SpriteBatch spriteBatch, TileObjectPreviewData op, Vector2 position)
	{
		const int wrap = 2;

		var texture = TextureAssets.Tile[op.Type].Value;
		var data = TileObjectData.GetTileData(op.Type, op.Style, op.Alternate);
		var color = ((op[0, 0] == 1) ? Color.White : Color.Red * .7f) * .5f;

		int style = data.CalculatePlacementStyle(op.Style, op.Alternate, op.Random);

		for (int frameX = 0; frameX < 3; frameX++)
		{
			for (int frameY = 0; frameY < 4; frameY++)
			{
				(int x, int y) = (op.Coordinates.X + frameX, op.Coordinates.Y + frameY);

				var source = new Rectangle(frameX * 20 + style % wrap * data.CoordinateFullWidth, frameY * 18 + style / wrap * data.CoordinateFullHeight, 18, 16);
				int offsetX = (frameX == 0) ? -2 : ((frameX == 2) ? 2 : 0);
				var drawPos = new Vector2(x, y) * 16 - Main.screenPosition + TileExtensions.TileOffset + new Vector2(offsetX, 0);

				spriteBatch.Draw(texture, drawPos, source, color, 0, Vector2.Zero, 1, SpriteEffects.None, 0f);
			}
		}
	}
}