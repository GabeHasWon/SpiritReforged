using SpiritReforged.Common.Particle;

namespace SpiritReforged.Content.Glyphs.Void;

public class VoidParticle : Particle
{
	private bool _clientInitialized = false;
	private Entity _entity = null;
	private Vector2 _offset;
	private SingularityRenderSystem.ShaderItem _shaderItem;

	public VoidParticle(Vector2 position, Vector2 velocity, Color color, float rotation, float scale, int maxTime, Entity attached = null)
	{
		Position = position;
		Color = color;
		Rotation = rotation;
		Scale = scale;
		MaxTime = maxTime;
		Velocity = velocity;

		_entity = attached;

		if (_entity != null)
			_offset = Position - _entity.Center;
	}

	public override void Update()
	{
		if (!_clientInitialized)
		{
			SingularityRenderSystem.ShaderItems.Add(_shaderItem = new());
			_clientInitialized = true;
		}

		if (_entity != null)
		{
			if (!_entity.active)
			{
				_entity = null;
				return;
			}

			Position = _entity.Center + _offset;
			_offset += Velocity;
		}

		Velocity *= 0.97f;
		Rotation += Velocity.Length() * 0.02f;

		if (_shaderItem != null)
		{
			_shaderItem.Position = Position;
			_shaderItem.Intensity = 0.5f;
			_shaderItem.Progress = Math.Max(Progress, 0.5f);
			_shaderItem.Scale = Scale;
			_shaderItem.timeActive = 2;
		}
	}

	public override void CustomDraw(SpriteBatch spriteBatch)
	{
		Texture2D bloomtexture = AssetLoader.LoadedTextures["Bloom"].Value;
		spriteBatch.Draw(bloomtexture, Position - Main.screenPosition, null, Color * 0.33f, 0, bloomtexture.Size() / 2, Scale * (1f - TimeActive / (float)MaxTime), SpriteEffects.None, 0);
	}

	public override ParticleLayer DrawLayer => ParticleLayer.AbovePlayer;

	public override ParticleDrawType DrawType => ParticleDrawType.Custom;
}