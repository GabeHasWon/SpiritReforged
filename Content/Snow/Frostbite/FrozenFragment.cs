namespace SpiritReforged.Content.Snow.Frostbite;

public class FrozenFragment : ModProjectile
{
	private const int timeLeftMax = 120;

	public override LocalizedText DisplayName => Language.GetText("Mods.SpiritReforged.Items.Frostbite.DisplayName");
	private int TargetWhoAmI
	{
		get => (int)Projectile.ai[0];
		set => Projectile.ai[0] = value;
	}

	private Vector2 relativeOffset;

	public override void SetStaticDefaults() =>	Main.projFrames[Type] = 3;

	public override void SetDefaults()
	{
		Projectile.Size = new Vector2(10);
		Projectile.aiStyle = -1;
		Projectile.ignoreWater = true;
		Projectile.tileCollide = false;
		Projectile.penetrate = -1;
		Projectile.timeLeft = timeLeftMax;
		Projectile.scale = 0;
	}

	public override void AI()
	{
		int fadeoutTime = 12;

		Projectile.rotation = Projectile.velocity.ToRotation();

		if (Projectile.timeLeft == timeLeftMax)
		{
			relativeOffset = Projectile.position - Main.npc[TargetWhoAmI].position;
			Projectile.frame = Main.rand.Next(Main.projFrames[Type]);
		}

		if (Projectile.timeLeft < fadeoutTime)
			Projectile.scale -= 1f / fadeoutTime;
		else
			Projectile.scale = Math.Min(1, Projectile.scale + .1f);

		var npc = Main.npc[TargetWhoAmI];
		Projectile.position = npc.position + relativeOffset;

		if (!npc.active)
			Projectile.Kill();
	}

	public override bool? CanCutTiles() => false;
	public override bool? CanDamage() => false;

	public override bool PreDraw(ref Color lightColor)
	{
		Texture2D texture = TextureAssets.Projectile[Type].Value;
		Rectangle rect = texture.Frame(1, Main.projFrames[Type], 0, Projectile.frame, 0, -2);
		Vector2 position = Projectile.Center - Main.screenPosition + new Vector2(0, Projectile.gfxOffY);

		Main.EntitySpriteDraw(texture, position, rect, Projectile.GetAlpha(Color.White), Projectile.rotation, rect.Size() / 2, Projectile.scale, SpriteEffects.None, 0);
		return false;
	}
}