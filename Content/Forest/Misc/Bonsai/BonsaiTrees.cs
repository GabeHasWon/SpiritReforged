using SpiritReforged.Common.Particle;
using SpiritReforged.Common.TileCommon.TileSway;
using SpiritReforged.Content.Particles;
using Terraria.GameContent.Drawing;
using TileHelper.Common;

namespace SpiritReforged.Content.Forest.Misc.Bonsai;

public class BonsaiTrees : ModTile
{
	public const int FrameWidth = 60;
	public const int FrameHeight = 72;

	public override void SetStaticDefaults()
	{
		Main.tileFrameImportant[Type] = true;
		Main.tileNoAttach[Type] = true;
		Main.tileLavaDeath[Type] = true;
		TileHelperSets.TileGlowmask[Type] = Helpers.RequestGlowmask(this);

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
			Vector2 position = new Vector2(i, j) * 16 + new Vector2(fluff);

			float scale = Main.rand.NextFloat(0.2f, 0.7f);
			int timeLeft = Main.rand.Next(15, 30);

			Vector2 rectangle = Main.rand.NextVector2FromRectangle(new((int)position.X, (int)position.Y, width, height));
			ParticleHandler.SpawnParticle(new EmberParticle(rectangle, Vector2.Zero, color, scale, timeLeft, 2));
		}
	}

	public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
	{
		Tile tile = Main.tile[i, j];

		if (!TileDrawing.IsVisible(tile))
			return false;

		Rectangle source = new(tile.TileFrameX, tile.TileFrameY, 18, 16);
		Vector2 position = Helpers.GetTilePosition(i, j) + GetSpecialOffset(tile.TileFrameX, tile.TileFrameY, out _, out _);

		spriteBatch.Draw(Helpers.GetTileTextureValue(tile), position, source, Lighting.GetColor(i, j), 0, Vector2.Zero, 1, 0, 0);

		if (TileObjectData.IsTopLeft(i, j))
			Main.instance.TilesRenderer.AddSpecialPoint(i, j, TileDrawing.TileCounterType.CustomNonSolid);

		return false;
	}

	public override void SpecialDraw(int i, int j, SpriteBatch spriteBatch) //Windy drawing
	{
		if (TileObjectData.GetTileData(Main.tile[i, j]) is not TileObjectData tileObjectData)
			return;

		(int width, int height) = (tileObjectData.Width, tileObjectData.Height);
		float physics = Physics(i, j, tileObjectData);

		for (int x = i; x < i + width; x++)
		{
			for (int y = j; y < j + height; y++)
			{
				Tile tile = Main.tile[x, y];
				Vector2 specialOffset = GetSpecialOffset(tile.TileFrameX, tile.TileFrameY, out int gridX, out int gridY);
				float rotation = (1.5f - (float)gridY / tileObjectData.Origin.Y) * physics * 0.1f;

				Vector2 position = new Vector2(x, y) * 16 - Main.screenPosition + new Vector2(0, Math.Abs(rotation) * 20f) + specialOffset;
				Vector2 origin = (tileObjectData.Origin.ToVector2() + Vector2.One * 0.5f - new Vector2(gridX, gridY)) * 16;
				Rectangle source = new(tile.TileFrameX + 20 * 6, tile.TileFrameY, 18, 16);
				Color lightColor = Lighting.GetColor(x, y);

				spriteBatch.Draw(Helpers.GetTileTextureValue(tile), position + origin, source, lightColor, rotation, origin, 1, 0, 0);
				spriteBatch.Draw(TileHelperSets.TileGlowmask[Type].Texture.Value, position + origin, source, lightColor * 3, rotation, origin, 1, 0, 0);
			}
		}
	}

	private static Vector2 GetSpecialOffset(int frameX, int frameY, out int gridX, out int gridY)
	{
		(gridX, gridY) = (frameX / 20 % 3, frameY / 18 % 4);
		return new Vector2(2 + gridX * 2 - 6, 2);
	}

	public static float Physics(int i, int j, TileObjectData tileObjectData)
	{
		(int width, int height) = (1, tileObjectData.Height);
		float rotation = Main.instance.TilesRenderer.GetWindCycle(++i, j, TileSwaySystem.TreeWindCounter);

		if (!WorldGen.InAPlaceWithWind(i, j, width, height))
			rotation = 0f;

		return (rotation + TileSwayHelper.GetHighestWindGridPushComplex(i, j, width, height, 30, 3f, 2, true)) * 0.3f;
	}

	public override bool PreDrawPlacementPreview(int i, int j, SpriteBatch spriteBatch, ref Rectangle frame, ref Vector2 position, ref Color color, bool validPlacement, ref SpriteEffects spriteEffects)
	{
		Texture2D texture = TextureAssets.Tile[Type].Value;

		position += GetSpecialOffset(frame.X, frame.Y, out _, out _) + Vector2.UnitX; //Mystery X offset
		spriteBatch.Draw(texture, position, frame with { X = frame.X + 20 * 6 }, color, 0, Vector2.Zero, 1, spriteEffects, 0);

		return true;
	}
}