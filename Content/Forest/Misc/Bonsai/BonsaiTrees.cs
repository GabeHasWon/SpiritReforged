using Microsoft.Xna.Framework.Graphics;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Content.Particles;
using Terraria.GameContent.Drawing;
using TileHelper.Common;

namespace SpiritReforged.Content.Forest.Misc.Bonsai;

public class BonsaiTrees : ModTile, WindTileRenderer.IDrawInWind
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

	void WindTileRenderer.IDrawInWind.DrawInWind(SpriteBatch spriteBatch, int i, int j, float rotation, Vector2 position, Vector2 origin)
	{
		Tile tile = Main.tile[i, j];
		if (TileDrawing.IsVisible(tile))
		{
			Vector2 offset = GetSpecialOffset(tile.TileFrameX, tile.TileFrameY, out _, out _);
			Rectangle source = new(tile.TileFrameX + 20 * 6, tile.TileFrameY, 18, 16);
			Color lightColor = Lighting.GetColor(i, j);

			DrawPot(spriteBatch, i, j);

			spriteBatch.Draw(Helpers.GetTileTextureValue(tile), position + offset, source, lightColor, rotation, origin, 1, 0, 0);
			spriteBatch.Draw(TileHelperSets.TileGlowmask[Type].Texture.Value, position + offset, source, lightColor * 3, rotation, origin, 1, 0, 0);
		}
	}

	private static void DrawPot(SpriteBatch spriteBatch, int i, int j)
	{
		Tile tile = Main.tile[i, j];
		Rectangle source = new(tile.TileFrameX, tile.TileFrameY, 18, 16);
		Vector2 position = new Vector2(i, j) * 16 - Main.screenPosition + GetSpecialOffset(tile.TileFrameX, tile.TileFrameY, out _, out _);

		spriteBatch.Draw(Helpers.GetTileTextureValue(tile), position, source, Lighting.GetColor(i, j), 0, Vector2.Zero, 1, 0, 0);
	}

	float WindTileRenderer.IDrawInWind.GetWindStrength(int i, int j)
	{
		const int width = 1;
		const int height = 4;

		float rotation = WorldGen.InAPlaceWithWind(++i, j, width, height) ? Main.instance.TilesRenderer.GetWindCycle(i, j, WindTileRenderer.TreeWindCounter) : 0;
		return (rotation + WindTileRenderer.GetHighestWindGridPushComplex(i, j, width, height, 30, 3f, 2, true)) * 0.3f;
	}

	private static Vector2 GetSpecialOffset(int frameX, int frameY, out int gridX, out int gridY)
	{
		(gridX, gridY) = (frameX / 20 % 3, frameY / 18 % 4);
		return new Vector2(2 + gridX * 2 - 6, 2);
	}

	public override bool PreDrawPlacementPreview(int i, int j, SpriteBatch spriteBatch, ref Rectangle frame, ref Vector2 position, ref Color color, bool validPlacement, ref SpriteEffects spriteEffects)
	{
		Texture2D texture = TextureAssets.Tile[Type].Value;

		position += GetSpecialOffset(frame.X, frame.Y, out _, out _) + Vector2.UnitX; //Mystery X offset
		spriteBatch.Draw(texture, position, frame with { X = frame.X + 20 * 6 }, color, 0, Vector2.Zero, 1, spriteEffects, 0);

		return true;
	}
}