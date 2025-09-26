namespace SpiritReforged.Content.Dusts;

public class BubbleDust : ModDust
{
	public override void OnSpawn(Dust dust)
	{
		dust.frame = new Rectangle(0, 0, 14, 14);
		dust.scale = 1f;
		dust.alpha = 75;
		dust.velocity *= 0.1f;
	}

	public override bool Update(Dust dust)
	{
		dust.noGravity = true;
		dust.position += dust.velocity;

		if ((dust.scale *= 0.99f) < 0.5f || (dust.alpha += 8) >= 255)
			dust.active = false;

		return false;
	}
}
