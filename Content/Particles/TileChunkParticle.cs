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
	private readonly int _bigModeTopLeftVariant;
	private readonly int _bigModeTopRightVariant;
	private readonly int _bigModeBottomLeftVariant;
	private readonly int _bigModeBottomRightVariant;

	public TileChunkParticle(Point tilePosition, Vector2 worldPosition, Vector2 velocity, int lifetime, bool bigMode = false) : base()
	{
		tilePosition.X = Math.Clamp(tilePosition.X, 0, Main.maxTilesX - 1);
		tilePosition.Y = Math.Clamp(tilePosition.Y, 0, Main.maxTilesY - 1);
		_tileCache = Main.tile[tilePosition];

		if (!_tileCache.HasTile || Main.tileFrameImportant[_tileCache.TileType])
		{
			killMe = true;
			return;
		}

		Position = worldPosition;
		Velocity = velocity;
		Scale = 1;
		_angularMomentum = Velocity.Length() * 0.02f * (velocity.X < 0 ? -1 : 1);

		//Pick the frame coordinates of the 1x1 tile chunks
		_xFrame = (short)(162 + Main.rand.Next(3) * 18);
		_yFrame = 54;
		_bigMode = bigMode;

		Main.instance.TilesRenderer.GetTileDrawData(tilePosition.X, tilePosition.Y, _tileCache, _tileCache.TileType, ref _xFrame, ref _yFrame, out _, out _, out _, out _, out int frameXExtra, out int frameYExtra, out _, out _, out _, out _);
		_xFrame += (short)frameXExtra;
		_yFrame += (short)frameYExtra;

		if (_bigMode)
		{
			_bigModeTopLeftVariant = Main.rand.Next(3);
			_bigModeTopRightVariant = Main.rand.Next(3);
			_bigModeBottomLeftVariant = Main.rand.Next(3);
			_bigModeBottomRightVariant = Main.rand.Next(3);
		}

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
			Vector2 unitX = Vector2.UnitX.RotatedBy(Rotation) * Scale * 8f;
			Vector2 unitY = Vector2.UnitY.RotatedBy(Rotation) * Scale * 8f;

			//Draw a chunky 2x2 cube of tiles
			spriteBatch.Draw(texture, position - unitX - unitY - Main.screenPosition, new Rectangle(_bigModeTopLeftVariant * 36, 54, 16, 16), color, Rotation, Vector2.One * 8, Scale, SpriteEffects.None, 0);
			spriteBatch.Draw(texture, position + unitX - unitY - Main.screenPosition, new Rectangle(18 + _bigModeTopRightVariant * 36, 54, 16, 16), color, Rotation, Vector2.One * 8, Scale, SpriteEffects.None, 0); 
			spriteBatch.Draw(texture, position - unitX + unitY - Main.screenPosition, new Rectangle(_bigModeBottomLeftVariant * 36, 72, 16, 16), color, Rotation, Vector2.One * 8, Scale, SpriteEffects.None, 0);
			spriteBatch.Draw(texture, position + unitX + unitY - Main.screenPosition, new Rectangle(18 + _bigModeBottomRightVariant * 36, 72, 16, 16), color, Rotation, Vector2.One * 8, Scale, SpriteEffects.None, 0);
		}
	}
}