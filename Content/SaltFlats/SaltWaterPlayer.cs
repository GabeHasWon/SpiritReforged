using SpiritReforged.Content.SaltFlats.Biome;

namespace SpiritReforged.Content.SaltFlats;

internal class SaltWaterPlayer : ModPlayer
{
	public override void PreUpdateMovement()
	{
		if (Player.wet && Player.InModBiome<SaltBiome>())
		{
			const float FloatSpeedMax = -1f;

			bool jump = false;

			if (Collision.WetCollision(Player.position + new Vector2(0, 20), Player.width, 8))
			{
				if (Player.velocity.Y > FloatSpeedMax)
				{
					float speedIncrease = Math.Max(0, 0.3f - Math.Abs(Player.velocity.X) * 0.1f);
					Player.velocity.Y = MathHelper.Max(Player.velocity.Y - speedIncrease, FloatSpeedMax);
				}
			}
			else if (Collision.WetCollision(Player.position + new Vector2(0, 28), Player.width, 8))
			{
				Player.velocity.Y -= Player.gravity * 0.6f;
				jump = true;
			}

			if (jump && Player.controlJump && Player.velocity.Y > -4)
				Player.velocity.Y = -4;
		}
	}
}
