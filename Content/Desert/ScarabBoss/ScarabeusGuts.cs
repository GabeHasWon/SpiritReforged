using SpiritReforged.Common.MathHelpers;
using SpiritReforged.Common.Visuals;
using Terraria.DataStructures;
using Terraria.Graphics.Renderers;

namespace SpiritReforged.Content.Desert.ScarabBoss;

public class ScarabeusGuts : Particle
{
	private SpriteFrame _source;
	private Vector2 _offset;
	private float _opacity;

	public ScarabeusGuts(Vector2 position, Vector2 velocity = default)
	{
		Position = position;
		Velocity = velocity;
		MaxTime = Gore.goreTime * 3;

		_source = new(2, 3, 0, (byte)Main.rand.Next(3));
		_opacity = 1;
	}

	public override void Update(ref ParticleRendererSettings settings)
	{
		const float fadeout_speed = 0.01f;

		base.Update(ref settings);

		Velocity.X *= 0.98f;
		Velocity.Y = Math.Min(Velocity.Y + 0.2f, 8);
		Rotation += Velocity.X * 0.1f;

		if (CollisionChecks.Tiles(new((int)(Position.X - 8), (int)(Position.Y - 8), 16, 16), CollisionChecks.SolidOrPlatform))
		{
			Position.Y = Position.ToTileCoordinates().ToWorldCoordinates().Y;

			_source = new(2, 3, 1, _source.CurrentRow);
			_offset.Y = 8;
			Velocity = Vector2.Zero;
			Rotation = 0;
		}

		if (TimeActive >= MaxTime - Math.Ceiling(1f / fadeout_speed))
			_opacity -= fadeout_speed;
	}

	public override void Draw(ref ParticleRendererSettings settings, SpriteBatch spriteBatch)
	{
		Rectangle source = _source.GetSourceRectangle(Texture);
		spriteBatch.Draw(Texture, Position - Main.screenPosition + _offset, source, Lighting.GetColor(Position.ToTileCoordinates()) * _opacity, Rotation, source.Size() / 2, 1, 0, 0);
	}
}