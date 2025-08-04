using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.ModCompat;
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
		float oldSwingArc = _swingArc;
		while (oldSwingArc == _swingArc) //Never select the same arc twice
			_swingArc = Main.rand.NextFromList(5f, -5f, 0f);

		if (_swingArc == 0)
			velocity = velocity.RotatedByRandom(0.5f);

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
			Projectile.rotation = Counter / 2f * Projectile.direction;

			float armRotation = MathHelper.PiOver2 * Projectile.direction;
			float tossHeight = EaseFunction.EaseCubicOut.Ease(Counter / (float)flourishTime) * 60;
			Projectile.Center = owner.GetFrontHandPosition(Player.CompositeArmStretchAmount.Full, armRotation) - new Vector2(0, tossHeight);

			owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, armRotation);
			owner.heldProj = Projectile.whoAmI;

			owner.itemAnimation = owner.itemTime = Projectile.timeLeft = 2;
			SwingArc = 6;

			if (Counter % 10 == 0)
				SoundEngine.PlaySound(SoundID.Item1 with { MaxInstances = 3 }, Projectile.Center);

			if (++Counter >= flourishTime)
			{
				Counter = 0;
				Projectile.ai[0] = 2;

				SoundEngine.PlaySound(RogueKnifeMinion.BigSwing, Projectile.Center);
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
				target.DelBuff(target.FindBuffIndex(BuffID.Bleeding));
			}
			else
			{
				modifiers.DisableCrit();
			}
		}
	}

	public override bool PreDraw(ref Color lightColor)
	{
		float offset = Math.Max(20 * (0.5f - Progress * 2) * (Stab ? 1 : 0), -10);
		DrawHeld(lightColor, new Vector2(0, TextureAssets.Projectile[Type].Value.Height) + new Vector2(-offset, offset), Projectile.rotation);

		int direction = Projectile.spriteDirection * Math.Sign(SwingArc);
		var effects = (direction == -1) ? SpriteEffects.FlipVertically : default;
		float rotation = Projectile.rotation - MathHelper.PiOver4 - 0.5f * direction;

		if (!Stab && !Flourishing)
		{
			DrawSmear(Projectile.GetAlpha(lightColor.MultiplyRGB(new Color(137, 93, 46)).Additive(100)) * Math.Min(Progress * 3, 1) * 0.5f, rotation, (int)(Progress * 8f), config.Reach + 10, effects: effects);
			DrawSmear(Projectile.GetAlpha(lightColor.MultiplyRGB(new Color(200, 160, 90))) * Math.Min(Progress * 3, 1) * 0.5f, rotation, (int)(Progress * 12f), config.Reach + 10, effects: effects);
		}

		return false;
	}
}