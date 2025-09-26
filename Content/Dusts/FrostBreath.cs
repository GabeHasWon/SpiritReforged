namespace SpiritReforged.Content.Dusts;

public class FrostBreath : ModDust
{
	public override void OnSpawn(Dust dust)
	{
		dust.noGravity = true;
		dust.frame = new Rectangle(0, 38 * Main.rand.Next(3), 36, 38);
		dust.noLight = true;

		dust.position -= dust.frame.Size() / 2; //Center the dust on spawn
	}

	public override Color? GetAlpha(Dust dust, Color lightColor) => dust.color;
	public override bool Update(Dust dust)
	{
		dust.color = Lighting.GetColor((int)(dust.position.X / 16), (int)(dust.position.Y / 16)).MultiplyRGB(new Color(156, 217, 255)) * 0.36f;
		dust.scale *= 0.992f;
		dust.velocity *= 0.97f;
		dust.rotation += 0.05f;
		dust.alpha += 15;

		Vector2 rotationOffset = (new Vector2(dust.scale) / MathHelper.PiOver2).RotatedBy(-dust.rotation);

		dust.position += dust.velocity * 0.1f + new Vector2(rotationOffset.X, -rotationOffset.Y);

		if (dust.scale <= 0.12f)
			dust.active = false;

		return false;
	}
}