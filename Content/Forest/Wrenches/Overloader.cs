using SpiritReforged.Common.Subclasses.Wrenches;
using SpiritReforged.Common.Visuals;

namespace SpiritReforged.Content.Forest.Wrenches;

public class Overloader : CopperSpanner
{
	public sealed class OverloaderExplosion : ModProjectile
	{
		public override string Texture => AssetLoader.EmptyTexture;
		public ref float Power => ref Projectile.ai[0];

		public int WindupTime { get; private set; }

		public override void SetDefaults()
		{
			Projectile.ignoreWater = true;
			Projectile.tileCollide = false;
			Projectile.friendly = true;
		}

		public override void AI()
		{
			if (WindupTime == 0)
			{
				Projectile.timeLeft = WindupTime = 30 + Math.Min((int)Power, 60); //90 ticks max
			} //Initialize times
		}

		public override void OnKill(int timeLeft)
		{
			//Explode and die
		}

		public override bool PreDraw(ref Color lightColor) => base.PreDraw(ref lightColor);
	}

	public sealed class OverloaderSwing : CopperSpannerSwing, IDrawPixelated, IHitSentry
	{
		public override LocalizedText DisplayName => ModContent.GetInstance<Overloader>().DisplayName;
		public override string Texture => ModContent.GetInstance<Overloader>().Texture;

		void IHitSentry.OnHitSentry(Player player, Projectile sentry, ref int cooldown)
		{
			IHitSentry.ClientHitEffects(sentry);

			if (player.TryGetModPlayer(out WrenchPlayer wrenchPlayer))
			{
				int totalScrap = wrenchPlayer.StoredScrap;
				wrenchPlayer.StoredScrap = 0;

				if (player.whoAmI == Main.myPlayer) //EXPLODE
					Projectile.NewProjectile(sentry.GetSource_Misc("WrenchHit"), sentry.Center, Vector2.Zero, ModContent.ProjectileType<OverloaderExplosion>(), 999, 9, Projectile.owner, totalScrap);
			}

			SetRecoil();
		}

		void IDrawPixelated.DrawPixelated(SpriteBatch spriteBatch) => DrawPixelatedSmear(spriteBatch, new Color(187, 165, 124));
	}

	public override void SetDefaults()
	{
		base.SetDefaults();

		Item.damage = 20;
		Item.useTime = Item.useAnimation = 22;
		Item.shoot = ModContent.ProjectileType<OverloaderSwing>();
	}
}