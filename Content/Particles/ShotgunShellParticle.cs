using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.Subclasses.Shotguns;
using SpiritReforged.Content.Forest.Ammo;

namespace SpiritReforged.Content.Particles;

public class ShotgunShellParticle : Particle
{
	internal ShotgunAmmoItem _ammo;
	internal bool HitTile;

	public ShotgunShellParticle(Vector2 position, Vector2 velocity, float scale, int maxTime, ShotgunAmmoItem ammo = null)
	{
		if (ammo is null)
			_ammo = ModContent.GetInstance<Shot>(); // default to shot shell texture
		else
			_ammo = ammo;

		Position = position;
		Color = Color.White;
		Scale = scale;
		MaxTime = maxTime;
		Velocity = velocity;
		HitTile = false;
		Rotation = Main.rand.NextFloat(6.28f);
	}

	public override void Update()
	{
		Tile tile = Framing.GetTileSafely((int)Position.X / 16, (int)Position.Y / 16);

		if (HitTile)
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

		Velocity.Y += 0.1f;
		Velocity *= 0.99f;

		Rotation += Velocity.Length() * 0.03f;

		if (tile.HasTile && tile.BlockType == BlockType.Solid && Main.tileSolid[tile.TileType] && !HitTile)
		{
			Velocity.X *= 0.75f;
			Velocity.Y *= -0.75f;
			HitTile = true;
		}
	}

	public override void CustomDraw(SpriteBatch spriteBatch)
	{
		var texture = ModContent.Request<Texture2D>(_ammo.Texture + "_Shell").Value;
		var bloom = AssetLoader.LoadedTextures["BloomNonPremult"].Value;

		float rotation = Rotation;

		float fade;

		if (Progress < 0.1f)
			fade = Progress / 0.1f;
		else
			fade = 1f - (Progress - 0.1f) / 0.9f;

		spriteBatch.End();
		spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, Main.DefaultSamplerState, default, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

		spriteBatch.Draw(bloom, Position - Main.screenPosition, null, Color.Black * 0.5f * fade, 0f, bloom.Size() / 2, Scale * 0.35f, 0, 0);

		spriteBatch.End();
		spriteBatch.BeginDefault();

		spriteBatch.Draw(texture, Position - Main.screenPosition, null, Lighting.GetColor((int)Position.X / 16, (int)Position.Y / 16, Color) * fade, rotation, texture.Size() / 2, Scale, 0, 0);
	}

	public override ParticleLayer DrawLayer => ParticleLayer.AbovePlayer;

	public override ParticleDrawType DrawType => ParticleDrawType.Custom;
}

