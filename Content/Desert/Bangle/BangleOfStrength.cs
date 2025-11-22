using SpiritReforged.Common.ItemCommon.Abstract;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.ModCompat.Classic;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.PlayerCommon;
using SpiritReforged.Content.Particles;
using Terraria.Audio;

namespace SpiritReforged.Content.Desert.Bangle;

[FromClassic("CleftHorn")]
public class BangleOfStrength : EquippableItem
{
	public sealed class BanglePlayer : ModPlayer
	{
		private int _cooldown;

		public override void ResetEffects()
		{
			if (_cooldown > 0)
				_cooldown--;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (Player.HasEquip<BangleOfStrength>() && hit.DamageType.CountsAsClass(DamageClass.Melee) && _cooldown == 0)
			{
				Projectile.NewProjectile(Player.GetSource_OnHit(target), target.Center, Vector2.Zero, ModContent.ProjectileType<BangleImpact>(), 50, 0, Player.whoAmI, target.whoAmI);
				_cooldown = 200;
			}
		}
	}

	public sealed class BangleImpact : ModProjectile
	{
		public const int TimeLeftMax = 12;

		public bool Activated => Projectile.timeLeft <= TimeLeftMax;
		public int TargetIndex
		{
			get => (int)Projectile.ai[0];
			set => Projectile.ai[0] = value;
		}

		private bool _setup;

		public override void SetStaticDefaults() => Main.projFrames[Type] = 6;
		public override void SetDefaults()
		{
			Projectile.friendly = true;
			Projectile.penetrate = -1;
			Projectile.tileCollide = false;
			Projectile.timeLeft = TimeLeftMax + 6;
			Projectile.Opacity = 0;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = -1;
		}

		public override void AI()
		{
			if (!Activated && Main.npc[TargetIndex] is NPC npc && npc.active)
				Projectile.Center = npc.Center;

			if (Activated && !_setup)
			{
				Projectile.rotation = Main.rand.NextFloat();
				Projectile.Opacity = 1;

				if (!Main.dedServ)
				{
					Vector2 velocity = Vector2.UnitX.RotatedBy(Projectile.rotation) * 0.1f;
					int timeLeft = Projectile.timeLeft;

					ParticleHandler.SpawnParticle(new ImpactLine(Projectile.Center, velocity, Color.PaleVioletRed.Additive(), Vector2.One * 1.2f, timeLeft));
					ParticleHandler.SpawnParticle(new ImpactLine(Projectile.Center, velocity, Color.White.Additive(), Vector2.One, timeLeft));

					SoundEngine.PlaySound(SoundID.DD2_BetsyFireballImpact, Projectile.Center);
					SoundEngine.PlaySound(SoundID.DD2_WitherBeastCrystalImpact with { Pitch = -0.5f }, Projectile.Center);
				}

				_setup = true;
			}

			Projectile.frame = (int)((1f - (float)Projectile.timeLeft / TimeLeftMax) * Main.projFrames[Type]);
		}

		public override bool PreDraw(ref Color lightColor)
		{
			Texture2D texture = TextureAssets.Projectile[Type].Value;
			Rectangle source = texture.Frame(1, Main.projFrames[Type], 0, Projectile.frame, 0, -2);
			Vector2 position = Projectile.Center - Main.screenPosition + new Vector2(0, Projectile.gfxOffY);

			Main.EntitySpriteDraw(texture, position, source, Projectile.GetAlpha(Color.White), Projectile.rotation, source.Size() / 2, Projectile.scale, default);
			Main.EntitySpriteDraw(texture, position, source, Projectile.GetAlpha(Color.White) * 0.2f, Projectile.rotation, source.Size() / 2, Projectile.scale * 1.1f, default);

			int impactType = ProjectileID.DD2ExplosiveTrapT1Explosion;
			Texture2D impactTexture = TextureAssets.Projectile[impactType].Value;
			source = impactTexture.Frame(1, Main.projFrames[impactType], 0, Projectile.frame);

			Main.EntitySpriteDraw(impactTexture, position, source, Projectile.GetAlpha(Color.PaleVioletRed).Additive() * 0.2f, Projectile.rotation + MathHelper.PiOver2, source.Size() / 2, Projectile.scale * 0.6f, default);
			Main.EntitySpriteDraw(impactTexture, position, source, Projectile.GetAlpha(Color.White).Additive() * 0.2f, Projectile.rotation + MathHelper.PiOver2, source.Size() / 2, Projectile.scale * 0.5f, default);

			return false;
		}

		public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) => modifiers.DamageVariationScale *= 0;
		public override bool? CanDamage() => Activated ? null : false;
		public override bool? CanHitNPC(NPC target) => target.whoAmI == TargetIndex ? null : false;
	}

	public override void SetDefaults()
	{
		Item.DefaultToAccessory();
		Item.rare = ItemRarityID.Blue;
		Item.value = Item.sellPrice(gold: 1, silver: 75);
	}
}