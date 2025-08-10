using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.ModCompat;
using SpiritReforged.Common.NPCCommon;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.ProjectileCommon.Abstract;
using SpiritReforged.Content.Forest.RoguesCrest;
using SpiritReforged.Content.Particles;
using Terraria.Audio;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Desert;

public class CeremonialDagger : ModItem
{
	private float _swingArc;

	public override void SetDefaults()
	{
		Item.damage = 18;
		Item.crit = 6;
		Item.knockBack = 3;
		Item.useTime = Item.useAnimation = 25;
		Item.DamageType = DamageClass.Melee;
		Item.width = Item.height = 46;
		Item.useStyle = ItemUseStyleID.Swing;
		Item.value = Item.sellPrice(silver: 3);
		Item.rare = ItemRarityID.Blue;
		Item.UseSound = SoundID.Item1;
		Item.shoot = ModContent.ProjectileType<CeremonialDaggerSwing>();
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
		const float secondaryArc = 5;

		if (player.altFunctionUse == 0) //Primary function
		{
			if (_swingArc == secondaryArc) //Primary attacks following a secondary are always uppercuts
			{
				_swingArc = -4.2f;
			}
			else
			{
				float oldSwingArc = _swingArc;
				while (oldSwingArc == _swingArc) //Never select the same arc twice
					_swingArc = Main.rand.NextFromList(4.2f, -4.2f, 0f);

				if (_swingArc == 0)
					velocity = velocity.RotatedByRandom(0.5f);
			}
		}
		else //Secondary function
		{
			_swingArc = secondaryArc;
		}

		SwungProjectile.Spawn(position, velocity, type, damage, knockback, player, _swingArc, source, player.altFunctionUse - 1);
		return false;
	}
}

public class CeremonialDaggerSwing : SwungProjectile
{
	public bool Stab => SwingArc == 0;
	public bool AltFunction => Projectile.ai[0] > 0;
	public bool Flourishing => Projectile.ai[0] == 1;

	public override string Texture => ModContent.GetInstance<CeremonialDagger>().Texture;
	public override LocalizedText DisplayName => ModContent.GetInstance<CeremonialDagger>().DisplayName;

	public static readonly SoundStyle Slash = new("SpiritReforged/Assets/SFX/Projectile/SwordSlash1") { Volume = 0.5f, Pitch = 0.5f, PitchVariance = 0.15f };

	public override Configuration SetConfiguration() => new(EaseFunction.EaseCubicOut, 58, 30);

	public override void AI()
	{
		if (Flourishing)
		{
			const int flourishTime = 20;
			var owner = Main.player[Projectile.owner];

			Projectile.spriteDirection = Projectile.direction = owner.direction = (Projectile.velocity.X > 0) ? 1 : -1;
			Projectile.rotation = Counter / 2f * -Projectile.direction;

			float armRotation = MathHelper.PiOver2 * Projectile.direction;
			float tossHeight = EaseFunction.EaseCubicOut.Ease(Counter / (float)flourishTime) * 60;
			Player.CompositeArmStretchAmount stretch = (Counter < 16) ? Player.CompositeArmStretchAmount.ThreeQuarters : Player.CompositeArmStretchAmount.Full;

			Projectile.Center = owner.GetFrontHandPosition(stretch, armRotation) - new Vector2(0, tossHeight);

			owner.SetCompositeArmFront(true, stretch, armRotation);
			owner.heldProj = Projectile.whoAmI;

			owner.itemAnimation = owner.itemTime = Projectile.timeLeft = 2;

			if (Counter % 10 == 0)
				SoundEngine.PlaySound(SoundID.Item1 with { MaxInstances = 3 }, Projectile.Center);

			if (++Counter >= flourishTime)
			{
				Counter = 0;
				Projectile.ai[0] = 2;

				SoundEngine.PlaySound(RogueKnifeMinion.BigSwing with { Volume = 0.7f, Pitch = 0.3f, PitchVariance = 0.1f }, Projectile.Center);

				for (int i = 0; i < 3; i++)
					ParticleHandler.SpawnParticle(new EmberParticle(Projectile.Center + new Vector2(34, -34).RotatedBy(Projectile.rotation), Main.rand.NextVector2Unit(), Color.Yellow, 1, 10));
			}

			return;
		}

		base.AI();

		if (Stab)
		{
			if (Counter == 1)
				ParticleHandler.SpawnParticle(new BasicNoiseCone(Projectile.Center - Projectile.velocity * 30, Projectile.velocity * 3, 20, new(75, 150)).SetColors(Color.SandyBrown, new Color(200, 160, 90)).SetIntensity(2));

			var owner = Main.player[Projectile.owner];
			Player.CompositeArmStretchAmount amount = (int)(Progress * 4f) switch
			{
				1 => Player.CompositeArmStretchAmount.ThreeQuarters,
				2 => Player.CompositeArmStretchAmount.Quarter,
				3 => Player.CompositeArmStretchAmount.None,
				_ => Player.CompositeArmStretchAmount.Full
			};

			GetRotation(out float armRotation);

			owner.SetCompositeArmFront(true, amount, armRotation);
			Projectile.Center = owner.GetFrontHandPosition(amount, armRotation);
		}

		if (AltFunction && Main.rand.NextBool())
		{
			var dust = Dust.NewDustPerfect(Projectile.Center + new Vector2(34, -34).RotatedBy(Projectile.rotation) + Main.rand.NextVector2Unit() * Main.rand.NextFloat(10), DustID.GoldFlame, null, 150, default, 1f - Progress);
			dust.noGravity = true;
			dust.velocity = (Vector2.UnitX * Main.rand.NextFloat(3)).RotatedBy(Projectile.rotation);
			dust.noLightEmittence = true;
		}
	}

	public override float GetRotation(out float armRotation)
	{
		int direction = Projectile.spriteDirection * Math.Sign(SwingArc);
		float value = base.GetRotation(out armRotation) + direction * Progress * 2;

		return value + MathHelper.PiOver4;
	}

	public override bool? CanDamage() => Flourishing ? false : null;
	public override bool? CanHitNPC(NPC target) => target.townNPC ? true : null; //Allow this to strike any town NPC
	public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
	{
		MoRHelper.Decapitation(target, ref damageDone, ref hit.Crit);

		if (Stab)
		{
			SoundEngine.PlaySound(Slash, Projectile.Center);
			SoundEngine.PlaySound(SoundID.NPCHit18 with { Volume = 0.5f, Pitch = 0.1f }, Projectile.Center);

			target.AddBuff(BuffID.Bleeding, 200);

			for (int i = 0; i < 10; i++)
				Dust.NewDustPerfect(target.Hitbox.ClosestPointInRect(Projectile.Center) + Main.rand.NextVector2Unit() * Main.rand.NextFloat(10), DustID.Blood, Main.rand.NextVector2Unit());
		}
	}

	public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
	{
		if (AltFunction)
		{
			if (target.HasBuff(BuffID.Bleeding))
			{
				modifiers.SetCrit();
				target.RemoveBuff(BuffID.Bleeding);
				Vector2 hitPos = target.Hitbox.ClosestPointInRect(Projectile.Center);

				for (int i = 0; i < 10; i++)
					Dust.NewDustPerfect(hitPos + Main.rand.NextVector2Unit() * Main.rand.NextFloat(10), DustID.Blood, Main.rand.NextVector2Unit());

				SoundEngine.PlaySound(SoundID.DD2_WitherBeastDeath with { Volume = 0.7f, Pitch = 0.7f }, Projectile.Center);
			}
			else
			{
				modifiers.Knockback += 1;
				modifiers.DisableCrit();
			}
		}
	}

	public override bool PreDraw(ref Color lightColor)
	{
		float offset = Math.Max(20 * (0.5f - Progress * 2) * (Stab ? 1 : 0), -10);
		DrawHeld(lightColor, new Vector2(2, TextureAssets.Projectile[Type].Value.Height - 2) + new Vector2(-offset, offset), Projectile.rotation);

		int direction = Projectile.spriteDirection * Math.Sign(SwingArc);
		var effects = (direction == -1) ? SpriteEffects.FlipVertically : default;
		float rotation = Projectile.rotation - MathHelper.PiOver4 - 0.5f * direction;

		if (!Stab && !Flourishing)
		{
			DrawSmear(Projectile.GetAlpha(lightColor.MultiplyRGB(new Color(137, 93, 46)).Additive(100)) * Math.Min(Progress * 3, 1) * 0.5f, rotation, (int)(Progress * 8f), config.Reach + 10, effects: effects);
			DrawSmear(Projectile.GetAlpha(lightColor.MultiplyRGB(new Color(200, 160, 90)).Additive((byte)(AltFunction ? 0 : 255))) * Math.Min(Progress * 3, 1) * 0.5f, rotation, (int)(Progress * 12f), config.Reach + 10, effects: effects);

			if (AltFunction && Progress > 0.2f)
				DrawSmear(Projectile.GetAlpha(lightColor.MultiplyRGB(Color.Goldenrod).Additive()) * (1f - Progress * 2), rotation, 3, config.Reach + 10, effects: effects);
		}

		if (AltFunction)
		{
			const float starScale = 0.3f;

			Main.instance.LoadProjectile(ProjectileID.RainbowRodBullet);
			var star = TextureAssets.Projectile[ProjectileID.RainbowRodBullet].Value;
			var glow = AssetLoader.LoadedTextures["Bloom"].Value;

			float mult = Flourishing ? (Counter - 10) / 10f :  1f - Counter / 15f;
			var position = Projectile.Center + new Vector2(34, -34).RotatedBy(Projectile.rotation) - Main.screenPosition;

			Main.EntitySpriteDraw(glow, position, null, lightColor.MultiplyRGB(Color.Goldenrod).Additive() * mult * 0.4f, 0, glow.Size() / 2, new Vector2(3, 0.3f) * Projectile.scale * 0.5f * starScale, default);
			Main.EntitySpriteDraw(glow, position, null, lightColor.MultiplyRGB(Color.Gold).Additive() * mult * 0.5f, 0, glow.Size() / 2, Projectile.scale * 0.5f * starScale, default);
			Main.EntitySpriteDraw(star, position, null, lightColor.MultiplyRGB(Color.Goldenrod).Additive() * mult, 0, star.Size() / 2, Projectile.scale * starScale, default);
			Main.EntitySpriteDraw(star, position, null, lightColor.MultiplyRGB(Color.White).Additive() * mult, 0, star.Size() / 2, Projectile.scale * 0.8f * starScale, default);
		}

		return false;
	}
}