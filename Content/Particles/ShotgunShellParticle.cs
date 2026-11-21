using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Subclasses.Shotguns;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Forest.Ammo;
using Terraria.Graphics.Renderers;

namespace SpiritReforged.Content.Particles;

public class ShotgunShellParticle : Particle
{
	private readonly IShotgunAmmo _ammo;
	private bool _hitTile;

	public ShotgunShellParticle(Vector2 position, Vector2 velocity, float scale, int maxTime, IShotgunAmmo ammo = null)
	{
		_ammo ??= ModContent.GetInstance<Shot>(); //Default to shot if null

		Position = position;
		Color = Color.White;
		Scale = scale;
		MaxTime = maxTime;
		Velocity = velocity;
		Rotation = Main.rand.NextFloat(6.28f);
	}

	public override void Update(ref ParticleRendererSettings settings)
	{
		base.Update(ref settings);

		Tile tile = Framing.GetTileSafely((int)Position.X / 16, (int)Position.Y / 16);

		if (_hitTile)
		{
			if (!tile.HasTile)
				Velocity.Y += 0.08f;
			else
				Velocity.Y *= 0.9f;

			Velocity *= 0.935f;

			Rotation += Velocity.Length() * 0.03f;
			TimeActive++;

			return;
		}

		Velocity.Y += 0.09f;
		if (Velocity.Y > 0)
			Velocity.Y *= 1.04f;

		if (Velocity.Y > 20f)
			Velocity.Y = 20f;

		Velocity *= 0.99f;

		Rotation += Velocity.Length() * 0.03f;

		if (tile.HasTile && tile.BlockType == BlockType.Solid && Main.tileSolid[tile.TileType] && !_hitTile)
		{
			Velocity.X *= 0.75f;
			Velocity.Y *= -0.75f;
			_hitTile = true;
		}
	}

	public override void Draw(ref ParticleRendererSettings settings, SpriteBatch spriteBatch)
	{
		if (_ammo is not ModTexturedType textured)
			return;

		Texture2D texture = ModContent.Request<Texture2D>(textured.Texture + "_Shell").Value;
		Texture2D bloom = AssetLoader.LoadedTextures["BloomNonPremult"].Value;

		float rotation = Rotation;
		float fade;

		if (Progress < 0.1f)
			fade = Progress / 0.1f;
		else
			fade = 1f - (Progress - 0.1f) / 0.9f;

		spriteBatch.End(); //BATCH ME
		spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, Main.DefaultSamplerState, default, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix); //BATCH ME

		spriteBatch.Draw(bloom, Position - Main.screenPosition, null, Color.Black * 0.5f * fade, 0f, bloom.Size() / 2, Scale * 0.35f, 0, 0); //BATCH ME

		spriteBatch.End(); //BATCH ME
		spriteBatch.BeginDefault(); //BATCH ME 

		spriteBatch.Draw(texture, Position - Main.screenPosition, null, Lighting.GetColor((int)Position.X / 16, (int)Position.Y / 16, Color) * fade, rotation, texture.Size() / 2, Scale, 0, 0); //BATCH ME
	}
}