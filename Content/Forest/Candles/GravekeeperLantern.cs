using SpiritReforged.Common.Easing;
using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.ModCompat;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.PlayerCommon;
using SpiritReforged.Content.Particles;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Forest.Candles;

public class GravekeeperLantern : ModItem, IDrawHeld
{
	public sealed class GhostlyFlame : ModProjectile
	{
		private bool _didSpawnEffects;

		public ref float Angle => ref Projectile.ai[0];

		public override void SetStaticDefaults()
		{
			ProjectileID.Sets.TrailCacheLength[Type] = 8;
			ProjectileID.Sets.TrailingMode[Type] = 0;
		}

		public override void SetDefaults()
		{
			Projectile.Size = new(12);
			Projectile.friendly = true;
			Projectile.tileCollide = false;
			Projectile.timeLeft = 80;
		}

		public override void AI()
		{
			if (!_didSpawnEffects)
			{
				Color[] colors = [Color.Goldenrod.Additive(100), Color.PaleVioletRed.Additive(100), Color.Red.Additive(100)];
				
				for (int i = 0; i < 2; i++)
					ParticleHandler.SpawnParticle(new FireParticle(Projectile.Center, Vector2.UnitY * -3, colors, 0.8f, 0.05f, EaseFunction.EaseQuarticOut, 18)
					{ PixelDivisor = 2 });

				_didSpawnEffects = true;

				if (Projectile.owner == Main.myPlayer)
				{
					Player owner = Main.player[Projectile.owner];
					float angle = owner.Center.Y - Projectile.Center.Y;
					Angle = angle / 1800f * owner.direction;
				}
			}

			Projectile.velocity *= 0.97f;
			Projectile.velocity = Projectile.velocity.RotatedBy(Angle);
			Projectile.rotation = Projectile.velocity.ToRotation();

			if (Main.rand.NextBool(5))
			{
				var dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.WhiteTorch, newColor: Color.White.Additive());
				dust.velocity = Projectile.velocity * 0.8f;
				dust.noGravity = true;
				dust.fadeIn = 1.1f * Projectile.scale;
			}

			Projectile.scale = Math.Min(Projectile.timeLeft / 20f, 1);
		}

		public override void OnKill(int timeLeft)
		{
			if (timeLeft <= 0)
				return;

			ParticleHandler.SpawnParticle(new TexturedPulseCircle(Projectile.Center, Color.PaleVioletRed, 0.5f, 100, 18, "supPerlin", Vector2.One * 3, EaseFunction.EaseCubicOut).WithSkew(0.5f, Main.rand.NextFloat(MathHelper.PiOver2)));
			ParticleHandler.SpawnParticle(new TexturedPulseCircle(Projectile.Center, Color.PaleVioletRed, 0.5f, 100, 18, "supPerlin", Vector2.One * 3, EaseFunction.EaseCubicOut).WithSkew(0.5f, Main.rand.NextFloat(MathHelper.PiOver2)));
			ParticleHandler.SpawnParticle(new LightBurst(Projectile.Center, 0, Color.Goldenrod, 0.5f, 16));

			for (int i = 0; i < 4; i++)
				ParticleHandler.SpawnParticle(new EmberParticle(Projectile.Center, Main.rand.NextVector2Circular(2, 2), Color.Goldenrod, Color.Red, 0.5f, 30, 3));
		}

		public override bool PreDraw(ref Color lightColor)
		{
			Texture2D texture = TextureAssets.Projectile[Type].Value;
			int length = ProjectileID.Sets.TrailCacheLength[Type];

			for (int i = 0; i < length; i++)
			{
				Vector2 drawPosition = Projectile.oldPos[i] + Projectile.Size / 2 - Main.screenPosition;
				float progress = i / (float)length;
				var color = Color.Lerp(Color.White, Color.PaleVioletRed, progress).Additive() * (1f - progress);

				Main.EntitySpriteDraw(texture, drawPosition, null, color, Projectile.rotation, texture.Size() / 2, Projectile.scale * (1f - progress), 0);
			}

			Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, Projectile.GetAlpha(Color.White).Additive(200), Projectile.rotation, texture.Size() / 2, Projectile.scale, 0);
			return false;
		}
	}

	public const int MANA_LIMIT = 100;

	public static float GetManaStrength(Player player) => Math.Min(player.GetManaConsumed() / (float)MANA_LIMIT, 1);

	public override void SetStaticDefaults()
	{
		Main.RegisterItemAnimation(Type, new NightlightLead.DrawGrid(3, 2, 1));
		MoRHelper.AddElement(Item, MoRHelper.Arcane, true);
	}

	public override void SetDefaults()
	{
		Item.DefaultToMagicWeapon(ModContent.ProjectileType<GhostlyFlame>(), 20, 10, true);
		Item.damage = 11;
		Item.mana = 8;
		Item.useStyle = ItemUseStyleID.HoldUp;
		Item.noUseGraphic = true;
		Item.UseSound = SoundID.Item1;
		Item.maxStack = 1;
		Item.value = Item.sellPrice(silver: 40);
	}

	public override void HoldItem(Player player) => player.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.ThreeQuarters, -MathHelper.PiOver2 * player.direction);

	void IDrawHeld.DrawHeld(ref PlayerDrawSet drawinfo)
	{
		Player player = drawinfo.drawPlayer;
		Texture2D texture = TextureAssets.Item[Type].Value;
		Rectangle source = Main.itemAnimations[Type].GetFrame(texture, 3);

		Vector2 bobOffset = Main.OffsetsPlayerHeadgear[player.bodyFrame.Y / player.bodyFrame.Height] * player.gravDir;
		Vector2 center = player.MountedCenter + bobOffset + new Vector2(15 * player.direction, 6 * player.gravDir);
		Vector2 drawPosition = new((int)(center.X - Main.screenPosition.X), (int)(center.Y - Main.screenPosition.Y + player.gfxOffY));

		float rotation = 0; //player.itemRotation
		float strength = Math.Min(GetManaStrength(player) * 1.1f, 1);
		Color color = Lighting.GetColor((int)center.X / 16, (int)center.Y / 16);

		if (strength > 0)
			source = Main.itemAnimations[Type].GetFrame(texture, 4);

		drawinfo.DrawDataCache.Add(new DrawData(texture, drawPosition, source, color, rotation, source.Size() / 2, 1, drawinfo.playerEffect, 0));

		source = Main.itemAnimations[Type].GetFrame(texture, 5);
		drawinfo.DrawDataCache.Add(new DrawData(texture, drawPosition, source, Color.White.Additive(100) * strength, rotation, source.Size() / 2, 1, drawinfo.itemEffect));
	}
}