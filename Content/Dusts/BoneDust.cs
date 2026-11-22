namespace SpiritReforged.Content.Dusts;

public class BoneDust : ModDust
{
	public override void OnSpawn(Dust dust)
	{
		dust.noGravity = false;
		dust.noLight = false;
	}

	public override bool Update(Dust dust)
	{
		dust.position += dust.velocity;
		dust.velocity.Y += 0.2f;

		if (Collision.SolidCollision(dust.position, 4, 4))
			dust.velocity *= -0.5f;

		dust.rotation = dust.velocity.ToRotation();
		dust.scale *= 0.99f;
		
		if (dust.scale < 0.2f)
			dust.active = false;
		
		return false;
	}
}
