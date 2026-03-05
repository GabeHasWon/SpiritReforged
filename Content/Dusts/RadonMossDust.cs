namespace SpiritReforged.Content.Dusts;

public class RadonMossDust : ModDust
{
	public override void OnSpawn(Dust dust)
	{
		dust.noGravity = true;
		dust.noLight = false;
		dust.color.R = 252;
		dust.color.G = 248;
		dust.color.B = 3;
	}

	public override Color? GetAlpha(Dust dust, Color lightColor) => new Color(161, 161, 157, 60);
	public override bool Update(Dust dust)
	{
		dust.position += dust.velocity;
		dust.velocity *= 0.88f;

		dust.rotation = dust.velocity.ToRotation();

		if ((dust.scale *= 0.99f) < 0.2f)
			dust.active = false;

		return false;
	}
}
