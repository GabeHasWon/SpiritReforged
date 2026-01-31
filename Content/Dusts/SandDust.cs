namespace SpiritReforged.Content.Dusts;

public class SandDust : ModDust
{
	public override bool Update(Dust dust)
	{
		dust.position += dust.velocity;
		dust.velocity *= 0.97f;
		dust.scale -= 0.02f;

		if (dust.scale > 1)
			dust.scale *= 0.9f;

		if (!dust.noGravity)
			dust.velocity.Y -= 0.05f;

		if (dust.scale <= 0f)
			dust.active = false;

		return false;
	}
}
