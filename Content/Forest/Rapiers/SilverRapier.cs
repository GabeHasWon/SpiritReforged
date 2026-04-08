using Microsoft.Xna.Framework.Graphics;
using SpiritReforged.Common;
using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.ModCompat;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.ProjectileCommon.Abstract;
using SpiritReforged.Content.Particles;
using Terraria.Audio;
using Terraria.DataStructures;
using static SpiritReforged.Common.PlayerCommon.DoubleTapPlayer;

namespace SpiritReforged.Content.Forest.Rapiers;

public class SilverRapier : ModItem
{
	public class SilverRapierSwing : RapierProjectile
	{
		public int FlourishDirection => (int)Projectile.ai[1];

		public override string Texture => ModContent.GetInstance<SilverRapier>().Texture;
		public override LocalizedText DisplayName => ModContent.GetInstance<SilverRapier>().DisplayName;

		public static readonly SoundStyle Slash = new("SpiritReforged/Assets/SFX/Projectile/SwordSlash1");

		private BasicNoiseCone _motionCone;
		private bool _hitSweetSpot;

		public override Configuration SetConfiguration() => new(EaseFunction.EaseCubicOut, 58, 12, ProgressiveStretch);

		public override void AI()
		{
			base.AI();

			if (Parry)
			{
				Main.player[Projectile.owner].GetModPlayer<ParryPlayer>().parryState = ParryPlayer.ParryState.Active;
			}
			else if (!Main.dedServ && SwingArc == 0 && Counter == 1)
			{
				ParticleHandler.SpawnParticle(_motionCone = (BasicNoiseCone)new BasicNoiseCone(Projectile.Center - Projectile.velocity * 8, Projectile.velocity, 20, new(50, 150)).SetColors(Color.White.Additive(100), Color.SteelBlue).SetIntensity(2).AttachTo(Projectile));
			}
		}

		public override void OnParry(Player.HurtInfo info)
		{
			SwingArc = 2;
			Counter = 0;

			Projectile.timeLeft++;
			Projectile.knockBack *= 3;
			Projectile.ai[0] = 0;

			if (Projectile.owner == Main.myPlayer)
			{
				Projectile.velocity = Projectile.DirectionTo(Main.MouseWorld);
				Projectile.netUpdate = true;
			}
		}

		public override float GetRotation(out float armRotation)
		{
			float flourishRotation = _hitSweetSpot ? 0 : ((Counter > SwingTime / 2) ? (Counter - SwingTime / 2) * 0.06f * FlourishDirection : 0);
			float easedRotation = EaseFunction.EaseCubicIn.Ease(flourishRotation);

			float value = base.GetRotation(out armRotation) + easedRotation;

			armRotation += easedRotation;
			return value;
		}

		public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
		{
			if (InSweetSpot(target, 12))
			{
				modifiers.SetCrit(); //Sweet spot crit
				SoundEngine.PlaySound(SoundID.DD2_CrystalCartImpact, target.Center);

				for (int i = 0; i < 5; i++)
				{
					float magnitude = Main.rand.NextFloat();
					ParticleHandler.SpawnParticle(new EmberParticle(GetEndPosition(), Projectile.velocity.RotatedByRandom(0.5f) * magnitude * -5f, Color.Goldenrod, 0.4f * (1f - magnitude), 30, 3));
				}

				_motionCone?.SetColors(Color.White.Additive(100), Color.Goldenrod);
				_hitSweetSpot = true;
			}
			else
			{
				modifiers.DisableCrit();
			}
		}

		public override bool PreDraw(ref Color lightColor)
		{
			if (Parry)
				return false;

			float offset = Math.Max(30 * (0.5f - Progress * 2), -2);
			DrawHeld(Projectile.GetAlpha(lightColor), new Vector2(0, TextureAssets.Projectile[Type].Value.Height) + new Vector2(-offset, offset), Projectile.rotation);

			if (SwingArc != 0)
			{
				int direction = Projectile.spriteDirection * Math.Sign(SwingArc);
				SpriteEffects effects = (direction == -1) ? SpriteEffects.FlipVertically : default;
				float rotation = Projectile.rotation - MathHelper.PiOver4 - 0.5f * direction;

				DrawSmear(Projectile.GetAlpha(lightColor.MultiplyRGB(Color.SteelBlue)) * Math.Min(Progress * 3, 1) * 0.5f, rotation, (int)(Progress * 8f), Config.Reach + 10, effects: effects);
				DrawSmear(Projectile.GetAlpha(lightColor.MultiplyRGB(new Color(200, 160, 90))) * Math.Min(Progress * 3, 1) * 0.5f, rotation, (int)(Progress * 12f), Config.Reach + 10, effects: effects);
			}
			else
			{
				float mult = 1f - Counter / 5f;
				if (mult > 0)
				{
					const float starScale = 0.8f;

					Main.instance.LoadProjectile(ProjectileID.RainbowRodBullet);
					Texture2D star = TextureAssets.Projectile[ProjectileID.RainbowRodBullet].Value;

					Vector2 position = GetEndPosition() - Main.screenPosition;

					Main.EntitySpriteDraw(star, position, null, lightColor.MultiplyRGB(Color.SteelBlue).Additive() * mult, 0, star.Size() / 2, Projectile.scale * starScale * mult, default);
					Main.EntitySpriteDraw(star, position, null, lightColor.MultiplyRGB(Color.White).Additive() * mult, 0, star.Size() / 2, Projectile.scale * 0.8f * starScale * mult, default);
				}
			}

			return false;
		}
	}

	public override void SetStaticDefaults() => SpiritSets.IsSword[Type] = true;

	public override void SetDefaults()
	{
		Item.damage = 14;
		Item.knockBack = 3;
		Item.useTime = Item.useAnimation = 18;
		Item.DamageType = DamageClass.Melee;
		Item.width = Item.height = 46;
		Item.useStyle = ItemUseStyleID.Swing;
		Item.value = Item.sellPrice(gold: 1);
		Item.rare = ItemRarityID.Blue;
		Item.UseSound = SoundID.Item1;
		Item.shoot = ModContent.ProjectileType<SilverRapierSwing>();
		Item.shootSpeed = 1f;
		Item.autoReuse = true;
		Item.useTurn = true;
		Item.noUseGraphic = true;
		Item.noMelee = true;
		MoRHelper.SetSlashBonus(Item);
	}

	public override bool AltFunctionUse(Player player) => true;

	public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
	{
		if (player.altFunctionUse != 2)
			SoundEngine.PlaySound(SilverRapierSwing.Slash with { Pitch = 1f, PitchVariance = 0.15f });

		SwungProjectile.Spawn(position, velocity, type, damage, knockback, player, 0, source, player.altFunctionUse - 1, Main.rand.NextFromList(-1, 1));
		return false;
	}

	public override void ModifyTooltips(List<TooltipLine> tooltips) => tooltips.RemoveAll(static x => x.Mod == "Terraria" && x.Name == "CritChance"); //Remove the line indicating crit chance
}