namespace SpiritReforged.Content.Dusts;

public class OganessonMossDust : ModDust
{
	public override void OnSpawn(Dust dust)
	{
		dust.noGravity = true;
		dust.noLight = false;
		dust.color.R = 220;
		dust.color.G = 220;
		dust.color.B = 220;
	}

	public override Color? GetAlpha(Dust dust, Color lightColor) => new Color(220, 220, 220, 60);
	public override bool Update(Dust dust)
	{
		dust.position += dust.velocity;
		dust.velocity *= 0.88f;
		dust.velocity.Y -= 0.2f;

		dust.rotation = dust.velocity.ToRotation();

		if ((dust.scale *= 0.95f) < 0.2f)
			dust.active = false;

		return false;
	}
}
