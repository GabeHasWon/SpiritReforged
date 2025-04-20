using SpiritReforged.Common.Particle;
using SpiritReforged.Content.Ocean.Items.Reefhunter.Particles;

namespace SpiritReforged.Content.Underground.Tiles.Potion;

public class BuffAura : ModProjectile
{
	public int PotionType
	{
		get => (int)Projectile.ai[0];
		set => Projectile.ai[0] = value;
	}

	public override string Texture => "Terraria/Images/Projectile_0";

	public override void SetDefaults()
	{
		Projectile.Size = new Vector2(200);
		Projectile.penetrate = -1;
		Projectile.aiStyle = 0;
		Projectile.tileCollide = false;
		Projectile.timeLeft = 500;
	}

	public override bool? CanDamage() => false;
	public override bool? CanCutTiles() => false;

	public override void AI()
	{
		var item = new Item(PotionType);

		int buffType = item.buffType;
		int buffTime = item.buffTime * 3;

		if (buffType == 0) //Invalid buff
		{
			Projectile.Kill();
			return;
		}

		foreach (var p in Main.ActivePlayers)
		{
			if (p.getRect().Intersects(Projectile.getRect()) && !p.HasBuff(buffType))
				p.AddBuff(buffType, buffTime);
		}

		if (Main.rand.NextBool(5))
		{
			var spawn = Projectile.Center + Main.rand.NextVector2Unit() * Main.rand.NextFloat(Projectile.width / 2);
			ParticleHandler.SpawnParticle(new BubbleParticle(spawn, Vector2.UnitY * -.3f, Main.rand.NextFloat(.35f), 60) { Color = VatSlot.GetColorFromPotion(PotionType) });
		}
	}

	public override bool PreDraw(ref Color lightColor) => false;
}