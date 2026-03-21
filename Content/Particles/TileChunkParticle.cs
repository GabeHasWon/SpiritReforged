using Microsoft.CodeAnalysis;
using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Particle;
using Terraria.DataStructures;
using Terraria.GameContent.Drawing;

namespace SpiritReforged.Content.Particles;

public class TileChunkParticle : Particle
{
	private readonly Tile _tileCache;
	private readonly short _xFrame = 0;
	private readonly short _yFrame = 0;
	private readonly bool killMe = false;

	private float _angularMomentum;

	private readonly bool _bigMode;
	private readonly TileChunkSegment[] _bigModeSegments;

	public TileChunkParticle(Point tilePosition, Vector2 worldPosition, Vector2 velocity, int lifetime, bool bigMode = false) : base()
	{
		tilePosition.X = Math.Clamp(tilePosition.X, 0, Main.maxTilesX - 1);
		tilePosition.Y = Math.Clamp(tilePosition.Y, 0, Main.maxTilesY - 1);
		_tileCache = Main.tile[tilePosition];

		if (!_tileCache.HasTile || 
			Main.tileFrameImportant[_tileCache.TileType] || 
			TileID.Sets.NotReallySolid[_tileCache.TileType] || 
			!Main.tileSolid[_tileCache.TileType] ||
			TileID.Sets.Platforms[_tileCache.TileType])
		{
			killMe = true;
			return;
		}

		Position = worldPosition;
		Velocity = velocity;
		Scale = 1;
		_angularMomentum = Velocity.Length() * Main.rand.NextFloat(0.015f, 0.022f) * (velocity.X < 0 ? -1 : 1);

		//Pick the frame coordinates of the 1x1 tile chunks
		_xFrame = (short)(162 + Main.rand.Next(3) * 18);
		_yFrame = 54;
		_bigMode = bigMode;

		Main.instance.TilesRenderer.GetTileDrawData(tilePosition.X, tilePosition.Y, _tileCache, _tileCache.TileType, ref _xFrame, ref _yFrame, out _, out _, out _, out _, out int frameXExtra, out int frameYExtra, out _, out _, out _, out _);
		_xFrame += (short)frameXExtra;
		_yFrame += (short)frameYExtra;

		if (_bigMode)
		{
			_bigModeSegments = new TileChunkSegment[4];
			_bigModeSegments[0] = new TileChunkSegment(0, 54, -1, -1);
			_bigModeSegments[1] = new TileChunkSegment(18, 54, 1, -1);
			_bigModeSegments[2] = new TileChunkSegment(0, 72, -1, 1);
			_bigModeSegments[3] = new TileChunkSegment(18, 72, 1, 1);
		}

		Rotation = Main.rand.NextFloat(MathHelper.TwoPi);
		MaxTime = lifetime;
	}

	public override void Update()
	{
		Rotation += _angularMomentum;
		_angularMomentum *= 0.98f;

		float velocityMultiplier = Utils.GetLerpValue(10, 30, TimeActive, true);
		Velocity.Y += 0.54f * velocityMultiplier;
		if (_bigMode)
			Velocity.Y += 0.07f * velocityMultiplier;

		if (killMe)
			Kill();
	}

	public override ParticleLayer DrawLayer => ParticleLayer.BelowSolid;

	public override ParticleDrawType DrawType => ParticleDrawType.Custom;

	public override void CustomDraw(SpriteBatch spriteBatch)
	{
		Texture2D texture = TextureAssets.Tile[_tileCache.TileType].Value;
		if (_tileCache.TileColor != PaintID.None)
		{
			Texture2D paintedTex = Main.instance.TilePaintSystem.TryGetTileAndRequestIfNotReady(_tileCache.TileType, 0, _tileCache.TileColor);
			texture = paintedTex ?? texture;
		}

		var frame = new Rectangle(_xFrame, _yFrame, 16, 16);
		Vector2 position = Position;
		Color color = Lighting.GetColor(Position.ToTileCoordinates());
		if (_tileCache.IsTileFullbright)
			color = Color.White;
		if (!TileDrawing.IsVisible(_tileCache))
			color = Color.Cyan * 0.1f;

		color *= Math.Min(1f, 2 - Progress * 2f);

		if (!_bigMode)
		{
			spriteBatch.Draw(texture, position - Main.screenPosition, frame, color, Rotation, Vector2.One * 8, Scale, SpriteEffects.None, 0);
		}
		else
		{
			Vector2 unitX = Vector2.UnitX.RotatedBy(Rotation) * Scale;
			Vector2 unitY = Vector2.UnitY.RotatedBy(Rotation) * Scale;

			//Draw a chunky 2x2 cube of tiles
			for (int i = 0; i < 4; i++)
				_bigModeSegments[i].Draw(spriteBatch, texture, position - Main.screenPosition, color, Rotation, Scale, unitX, unitY);
		}
	}

	private struct TileChunkSegment
	{
		public int shrinkX;
		public int shrinkY;

		public int dirX;
		public int dirY;

		public Rectangle frame;

		public TileChunkSegment(int frameXStart, int frameY, int directionX, int directionY)
		{
			int frameX = frameXStart + Main.rand.Next(3) * 36;

			shrinkX = 0;
			shrinkY = 0;

			if (Main.rand.NextBool(3))
			{
				shrinkX = Main.rand.Next(4) * 2;
				shrinkY = Main.rand.Next(4) * 2;
			}

			dirX = directionX;
			dirY = directionY;

			frame = new Rectangle(frameX + Math.Max(0, shrinkX * dirX), frameY + Math.Max(0, shrinkY * dirY), 16 - shrinkX, 16 - shrinkY);
		}

		public void Draw(SpriteBatch spriteBatch, Texture2D texture, Vector2 position, Color color, float rotation, float scale, Vector2 unitX, Vector2 unitY)
		{
			position = new Vector2((int)position.X, (int)position.Y);

			int shiftX = Math.Min((16 - shrinkX) * dirX, 0);
			int shiftY = Math.Min((16 - shrinkY) * dirY, 0);

			Vector2 drawPosition = position + unitX * shiftX + unitY * shiftY;
			spriteBatch.Draw(texture, drawPosition, frame, color, rotation, Vector2.Zero, scale, SpriteEffects.None, 0);
		}
	}
}