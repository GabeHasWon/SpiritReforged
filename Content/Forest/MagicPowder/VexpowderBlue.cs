namespace SpiritReforged.Content.Forest.MagicPowder;

public class VexpowderBlue : Flarepowder
{
	public override void SetDefaults()
	{
		base.SetDefaults();
		Item.shoot = ModContent.ProjectileType<VexpowderBlueDust>();
	}
}

internal class VexpowderBlueDust : FlarepowderDust
{
	public override Color[] Colors => [Color.Violet, Color.BlueViolet, Color.Goldenrod];
}