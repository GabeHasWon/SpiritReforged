using SpiritReforged.Content.SaltFlats.Biome;

namespace SpiritReforged.Content.SaltFlats;

internal class SaltWaterPlayer : ModPlayer
{
	public override void PreUpdateMovement()
	{
		if (Player.wet && Player.InModBiome<SaltBiome>())
		{
			const float FloatSpeed = -1f;

			if (Collision.WetCollision(Player.position + new Vector2(0, 20), Player.width, 8))
			{
				if (Player.velocity.Y > FloatSpeed)
				{
					float floatSpeed = Math.Max(0, 0.3f - Math.Abs(Player.velocity.X) * 0.1f);
					Player.velocity.Y = MathHelper.Max(Player.velocity.Y - floatSpeed, FloatSpeed);
				}
			}
			else if (Collision.WetCollision(Player.position + new Vector2(0, 28), Player.width, 8))
			{
				Player.velocity.Y -= Player.gravity * 0.6f;
			}
		}
	}
}
