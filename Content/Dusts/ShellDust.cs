namespace SpiritReforged.Content.Dusts;

public class ShellDust : ModDust
{
    private readonly Point dustDims = new(8, 12);

    public override void OnSpawn(Dust dust) => dust.frame = new Rectangle(0, 0, dustDims.X - 2, dustDims.Y - 2);

    public override bool Update(Dust dust)
    {
		dust.velocity.X *= 0.9f;
		if (Collision.SolidCollision(dust.position - new Vector2(2), 4, 4))
		{
			dust.velocity.Y *= -0.5f;
		}
		else
		{
			dust.velocity.Y += 0.75f;
			if (Math.Abs(dust.velocity.Y) > 1f)
			{
				//Use fadeIn to increment our draw frames because it's functionless otherwise
				dust.frame = new Rectangle(0, dustDims.Y * ((int)(++dust.fadeIn / 4) % 9), dustDims.X - 2, dustDims.Y - 2);
			}
		}

		dust.position += dust.velocity;
        dust.rotation += dust.velocity.X / 8;

		if (Math.Abs(dust.velocity.X) < 0.5f && ++dust.alpha >= 255)
			dust.active = false;

		return false;
    }
}