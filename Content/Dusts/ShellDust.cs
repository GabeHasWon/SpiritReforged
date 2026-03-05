namespace SpiritReforged.Content.Dusts;

public class ShellDust : ModDust
{
    private readonly Vector2 dustDims = new(8, 12);

    public override void OnSpawn(Dust dust) => dust.frame = new Rectangle(0, 0, (int)dustDims.X - 2, (int)dustDims.Y - 2);

    public override bool Update(Dust dust)
    {
		//Use fadeIn to increment our draw frames because it's functionless otherwise
		dust.frame = new Rectangle(0, (int)dustDims.Y * ((int)(++dust.fadeIn / 4) % 9), (int)dustDims.X - 2, (int)dustDims.Y - 2);
		
		dust.velocity.X *= .9f;
		dust.velocity.Y += .75f;

		dust.position += dust.velocity;
        dust.rotation += dust.velocity.X / 8;

		if (Math.Abs(dust.velocity.X) < .5f && ++dust.alpha >= 255 || WorldGen.SolidOrSlopedTile(Framing.GetTileSafely(dust.position)))
			dust.active = false;

		return false;
    }
}