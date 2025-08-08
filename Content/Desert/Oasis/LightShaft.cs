using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.SimpleEntity;
using SpiritReforged.Content.Particles;

namespace SpiritReforged.Content.Desert.Oasis;

public class LightShaft : SimpleEntity
{
	public override string TexturePath => AssetLoader.EmptyTexture;

	public virtual bool ValidPosition()
	{
		Point coords = Center.ToTileCoordinates();
		return WorldGen.SolidTile(coords);
	}

	public override void Load() => saveMe = true;

	public override void Update()
	{
		if (!ValidPosition())
		{
			Kill();
			return;
		}

		Lighting.AddLight(Center + new Vector2(0, 8), new Vector3(0.5f, 0.5f, 0.1f));

		if (Main.rand.NextBool(50))
			ParticleHandler.SpawnParticle(new GlowParticle(new Vector2(Center.X + Main.rand.Next(-20, 20), Center.Y), Vector2.UnitY * Main.rand.NextFloat(), Color.Goldenrod, Main.rand.NextFloat(0.2f, 0.8f), 180));
	}

	public override void Draw(SpriteBatch spriteBatch)
	{
		var texture = AssetLoader.LoadedTextures["Ray"].Value;

		for (int i = 0; i < 3; i++)
		{
			float lerp = (float)Math.Sin((Main.timeForVisualEffects + i * 200f) / 50f);
			var position = Center + new Vector2(6f * lerp, 0) - Main.screenPosition;

			spriteBatch.Draw(texture, position, null, Color.Goldenrod.Additive() * (0.1f + 0.05f * lerp), 0, new Vector2(texture.Width / 2, 0), 2, default, 0);
			spriteBatch.Draw(texture, position, null, Color.White.Additive() * (0.05f + 0.025f * lerp), 0, new Vector2(texture.Width / 2, 0), 1, default, 0);
		}
	}
}