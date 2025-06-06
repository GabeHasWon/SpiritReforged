namespace SpiritReforged.Content.Forest.MagicPowder;

public class VexpowderRed : Flarepowder
{
	public override void SetDefaults()
	{
		base.SetDefaults();
		Item.shoot = ModContent.ProjectileType<VexpowderRedDust>();
	}
}

internal class VexpowderRedDust : FlarepowderDust
{
	public override Color[] Colors => [Color.Red, Color.OrangeRed, Color.Goldenrod];
}