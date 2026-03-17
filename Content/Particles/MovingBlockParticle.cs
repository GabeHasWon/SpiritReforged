using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Particle;
using Terraria.GameContent.Drawing;

namespace SpiritReforged.Content.Particles;

public class MovingBlockParticle : Particle
{
	private readonly Point _tilePosition;
	private readonly float _bounceHeight;
	private readonly Tile _tile;

	private readonly short _xFrame = 0;
	private readonly short _yFrame = 0;

	public MovingBlockParticle(Vector2 worldPosition, int bounceTime, float bounceHeight) : base()
	{
		Point tilePosition = worldPosition.ToTileCoordinates();
		Position = tilePosition.ToWorldCoordinates(0, 0);
		Velocity = Vector2.Zero;
		_tilePosition = tilePosition;
		_bounceHeight = bounceHeight;

		_tile = Main.tile[tilePosition];

		_xFrame = _tile.TileFrameX;
		_yFrame = _tile.TileFrameY;

		Main.instance.TilesRenderer.GetTileDrawData(tilePosition.X, tilePosition.Y, _tile, _tile.TileType, ref _xFrame, ref _yFrame, out _, out _, out _, out _, out int frameXExtra, out int frameYExtra, out _, out _, out _, out _);

		_xFrame += (short)frameXExtra;
		_yFrame += (short)frameYExtra;

		MaxTime = bounceTime;
	}

	public override void Update()
	{
		if (Main.tile[_tilePosition] != _tile)
			Kill();
	}

	public override ParticleLayer DrawLayer => ParticleLayer.AboveSolid;

	public override ParticleDrawType DrawType => ParticleDrawType.Custom;

	public override void CustomDraw(SpriteBatch spriteBatch)
	{
		if (!Collision.SolidTiles(Position, 1, 1, false))
			return;
		if (!TileDrawing.IsVisible(_tile))
			return;

		float bounceDisplace = _bounceHeight * EaseFunction.EaseSine.Ease(Progress);

		Texture2D texture = TextureAssets.Tile[_tile.TileType].Value;
		Color color = Lighting.GetColor(_tilePosition);
		if (_tile.TileColor != PaintID.None)
		{
			Texture2D paintedTex = Main.instance.TilePaintSystem.TryGetTileAndRequestIfNotReady(_tile.TileType, 0, _tile.TileColor);
			texture = paintedTex ?? texture;
		}

		if (_tile.IsTileFullbright)
			color = Color.White;

		var frame = new Rectangle(_xFrame, _yFrame, 16, 18);
		Vector2 position = Position - Vector2.UnitY * bounceDisplace;
		spriteBatch.Draw(texture, position - Main.screenPosition, frame, color, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0);

		var bottomFrame = new Rectangle(_xFrame, (_yFrame / 18) * 18 + 14, 16, 2);
		var origin = new Vector2(0, 2);
		var scale = new Vector2(1f, bounceDisplace / 2f);

		spriteBatch.Draw(texture, Position + Vector2.UnitY * 16f - Main.screenPosition, bottomFrame, color * 0.5f, 0f, origin, scale, SpriteEffects.None, 0);
	}
}