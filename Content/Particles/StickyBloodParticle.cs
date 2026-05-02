using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using Terraria;

namespace SpiritReforged.Content.Particles;

public class StickyBloodParticle : Particle
{
	internal bool HitTile;
	internal int variant;
	internal float FallSpeed;
	public StickyBloodParticle(Vector2 position, Vector2 velocity, float scale, int maxTime, float fallSpeed = 0.15f)
	{
		Position = position;
		Color = Color.White * 0.75f;
		Scale = scale;
		MaxTime = maxTime;
		Velocity = velocity;
		HitTile = false;
		FallSpeed = fallSpeed;
		variant = Main.rand.Next(3);
		Rotation = Main.rand.NextFloat(6.28f);
	}

	public override void Update()
	{
		if (HitTile)
		{
			Velocity *= 0f;
			Velocity.Y += 0.03f;
			return;
		}

		Velocity.Y += FallSpeed;
		Rotation = Velocity.ToRotation();

		Tile tile = Framing.GetTileSafely((int)Position.X / 16, (int)Position.Y / 16);

		if (tile.HasTile && tile.BlockType == BlockType.Solid && Main.tileSolid[tile.TileType] && !HitTile)
		{
			TimeActive++;
			Velocity *= -0.1f;
			HitTile = true;
		}
	}

	public override void CustomDraw(SpriteBatch spriteBatch)
	{
		var texture = Texture;
		var bloom = AssetLoader.LoadedTextures["BloomNonPremult"].Value;

		float rotation = Rotation;

		float fade;

		Rectangle frame = texture.Frame(1, 3, 0, variant);

		if (Progress < 0.25f)
			fade = Progress / 0.25f;
		else
			fade = (1f - (Progress - 0.25f) / 0.75f);

		spriteBatch.End();
		spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, Main.DefaultSamplerState, default, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

		spriteBatch.Draw(bloom, Position - Main.screenPosition, null, Color.DarkRed * 0.5f * fade, 0f, bloom.Size() / 2, Scale * 0.35f, 0, 0);

		spriteBatch.End();
		spriteBatch.BeginDefault();

		spriteBatch.Draw(texture, Position - Main.screenPosition, frame, Color * fade, rotation, frame.Size() / 2, Scale, 0, 0);
	}

	public override ParticleLayer DrawLayer => ParticleLayer.AbovePlayer;

	public override ParticleDrawType DrawType => ParticleDrawType.Custom;
}
