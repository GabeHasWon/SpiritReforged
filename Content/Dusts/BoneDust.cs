namespace SpiritReforged.Content.Dusts;

public class BoneDust : ModDust
{
	public override void OnSpawn(Dust dust)
	{
		dust.noGravity = false;
		dust.noLight = false;
		dust.frame = Texture2D.Frame(1, 6, 0, Main.rand.Next(6));
	}

	public override bool Update(Dust dust)
	{
		dust.position += dust.velocity;
		dust.velocity.Y += 0.2f;

		if (Collision.SolidCollision(dust.position, 4, 4))
			dust.velocity *= -0.5f;

		dust.rotation = MathHelper.Lerp(dust.rotation, dust.velocity.ToRotation(), 0.05f);
		dust.scale *= 0.99f;
		
		if (dust.scale < 0.2f)
			dust.active = false;
		
		return false;
	}
}
