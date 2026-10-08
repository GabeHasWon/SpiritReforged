namespace SpiritReforged.Content.Dusts;
public class YellowElectricDust : ModDust
{       
	// All code below is adapted from vanilla code for DustID.Electric
	public override bool Update(Dust dust)
	{
		float scale = dust.scale;
		if (scale > 1f)
			scale = 1f;

		if (!dust.noLight)
			Lighting.AddLight((int)(dust.position.X / 16f), (int)(dust.position.Y / 16f), scale * 0.2f, scale * 1f, scale * 1f);

		if (dust.noGravity)
		{
			dust.velocity *= 0.93f;
			if (dust.fadeIn == 0f)
				dust.scale += 0.0025f;
		}

		dust.velocity *= new Vector2(0.97f, 0.99f);
		dust.scale -= 0.01f;

		dust.position += dust.velocity;

		return false;
	}

	public override Color? GetAlpha(Dust dust, Color lightColor)
	{
		return Color.Lerp(lightColor, Color.White, 0.8f) with { A = 25 };
	}

	public override bool PreDraw(Dust dust)
	{
		Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;

		float length = Math.Abs(dust.velocity.X) + Math.Abs(dust.velocity.Y) * 3f;
		if (length > 10f)
			length = 10f;

		for (int i = 0; i < (int)length; i++)
		{
			Vector2 pos = dust.position - dust.velocity * i;
			float scale = dust.scale * (1f - i / 10f);

			Color color = Lighting.GetColor((int)((dust.position.X + 4.0) / 16), (int)((dust.position.Y + 4.0) / 16));
			color = dust.GetAlpha(color);

			Main.spriteBatch.Draw(texture, pos - Main.screenPosition, dust.frame, color, dust.rotation, new Vector2(4f), scale, 0f, 0f);
		}

		return false;
	}
}
