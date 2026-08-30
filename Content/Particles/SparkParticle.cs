using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;

namespace SpiritReforged.Content.Particles;
public class SparkParticle : Particle
{
	private Color lineColor;
	private Color bloomColor;

	internal bool HitTile;

	private bool canCollide;
	private bool addLight = true;

	private readonly Action<Particle> _action;
	public ParticleLayer Layer { get; set; } = ParticleLayer.BelowProjectile;
	public override ParticleLayer DrawLayer => Layer;

	public override ParticleDrawType DrawType => ParticleDrawType.Custom;

	public SparkParticle(Vector2 position, Vector2 velocity, Color LineColor, Color BloomColor, float scale, int maxTime, Action<Particle> extraUpdateAction = null, bool AddLight = true, bool tileCollide = true)
	{
		Position = position;
		Velocity = velocity;
		lineColor = LineColor.Additive();
		bloomColor = BloomColor.Additive();
		Scale = scale;
		MaxTime = maxTime;
		_action = extraUpdateAction;
		addLight = AddLight;

		canCollide = tileCollide;
	}

	public SparkParticle(Vector2 position, Vector2 velocity, Color color, float scale, int maxTime, Action<Particle> extraUpdateAction = null, bool AddLight = true, bool tileCollide = true) : this(position, velocity, color, color, scale, maxTime, extraUpdateAction, AddLight, tileCollide) { }

	public override void Update()
	{
		if (canCollide)
		{
			Tile tile = Framing.GetTileSafely((int)Position.X / 16, (int)Position.Y / 16); // we really need collision for particles I think

			if (tile.HasTile && tile.BlockType == BlockType.Solid && Main.tileSolid[tile.TileType] && !HitTile)
			{
				Velocity *= 0f;
				HitTile = true;
			}
		}
		
		if (addLight)
			Lighting.AddLight(Position, lineColor.ToVector3() * (1f - Progress));

		if (!HitTile)
		{
			Rotation = Velocity.ToRotation();
			_action?.Invoke(this);
		}
		else
			TimeActive++;	
	}

	public override void CustomDraw(SpriteBatch spriteBatch)
	{
		Main.instance.LoadProjectile(873);

		float progress = 1 - EaseBuilder.EaseCubicOut.Ease(Progress);

		var glowLine = TextureAssets.Projectile[873].Value;
		Texture2D bloomtexture = AssetLoader.LoadedTextures["Bloom"].Value;

		spriteBatch.Draw(bloomtexture, Position - Main.screenPosition, null, bloomColor * 0.1f, Rotation + MathHelper.PiOver2, bloomtexture.Size() / 2, Scale * 0.5f * progress, SpriteEffects.None, 0);

		spriteBatch.Draw(glowLine, Position - Main.screenPosition, null, lineColor, Rotation + MathHelper.PiOver2, glowLine.Size() / 2, new Vector2(0.2f, 1f) * Scale * progress, SpriteEffects.None, 0);

		spriteBatch.Draw(glowLine, Position - Main.screenPosition, null, Color.White.Additive(), Rotation + MathHelper.PiOver2, glowLine.Size() / 2, new Vector2(0.2f, 1f) * Scale * progress * 0.5f, SpriteEffects.None, 0);
	}
}
