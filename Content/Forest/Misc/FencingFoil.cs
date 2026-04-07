using SpiritReforged.Common;
using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.ModCompat;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.ProjectileCommon.Abstract;
using SpiritReforged.Content.Particles;
using Terraria.Audio;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Forest.Misc;

public class FencingFoil : ModItem
{
	public class FencingFoilSwing : SwungProjectile
	{
		public int FlourishDirection => (int)Projectile.ai[0];

		public override string Texture => ModContent.GetInstance<FencingFoil>().Texture;
		public override LocalizedText DisplayName => ModContent.GetInstance<FencingFoil>().DisplayName;

		public static readonly SoundStyle Slash = new("SpiritReforged/Assets/SFX/Projectile/SwordSlash1");

		public override Configuration SetConfiguration() => new(EaseFunction.EaseCubicOut, 58, 12);

		public override void AI()
		{
			base.AI();

			if (!Main.dedServ && Counter == 1)
				ParticleHandler.SpawnParticle(new BasicNoiseCone(Projectile.Center - Projectile.velocity * 5, Projectile.velocity, 20, new(50, 150)).SetColors(Color.White, Color.Gray).SetIntensity(2).AttachTo(Projectile));

			Player.CompositeArmStretchAmount amount = (int)(Progress * 4f) switch
			{
				1 => Player.CompositeArmStretchAmount.ThreeQuarters,
				2 => Player.CompositeArmStretchAmount.Quarter,
				3 => Player.CompositeArmStretchAmount.None,
				_ => Player.CompositeArmStretchAmount.Full
			};

			GetRotation(out float armRotation);

			Owner.SetCompositeArmFront(true, amount, armRotation);
			Projectile.Center = Owner.GetFrontHandPosition(amount, armRotation);
		}

		public override float GetRotation(out float armRotation)
		{
			float flourishRotation = Counter * 0.01f * FlourishDirection;
			int direction = Projectile.spriteDirection * Math.Sign(SwingArc);
			float value = base.GetRotation(out armRotation) + flourishRotation + direction * Progress * 2;

			armRotation += flourishRotation;
			return value + MathHelper.PiOver4;
		}

		public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
		{
			int reach = config.Reach - 4;
			if (Projectile.Center.DistanceSQ(target.Hitbox.ClosestPointInRect(Projectile.Center)) > reach * reach)
			{
				modifiers.SetCrit(); //Sweet spot crit
				SoundEngine.PlaySound(SoundID.DD2_CrystalCartImpact, target.Center);
			}
			else
			{
				modifiers.DisableCrit();
			}
		}

		public override bool PreDraw(ref Color lightColor)
		{
			float offset = Math.Max(20 * (0.5f - Progress * 2), -10);
			DrawHeld(lightColor, new Vector2(0, TextureAssets.Projectile[Type].Value.Height) + new Vector2(-offset, offset), Projectile.rotation);
			float mult = 1f - Counter / 10f;

			if (mult > 0)
			{
				const float starScale = 0.3f;

				Main.instance.LoadProjectile(ProjectileID.RainbowRodBullet);
				Texture2D star = TextureAssets.Projectile[ProjectileID.RainbowRodBullet].Value;
				Texture2D glow = AssetLoader.LoadedTextures["Bloom"].Value;

				Vector2 position = GetEndPosition() - Main.screenPosition;

				Main.EntitySpriteDraw(glow, position, null, lightColor.MultiplyRGB(Color.SteelBlue).Additive() * mult * 0.4f, 0, glow.Size() / 2, new Vector2(3, 0.3f) * Projectile.scale * 0.5f * starScale, default);
				Main.EntitySpriteDraw(glow, position, null, lightColor.MultiplyRGB(Color.LightSteelBlue).Additive() * mult * 0.5f, 0, glow.Size() / 2, Projectile.scale * 0.5f * starScale, default);
				Main.EntitySpriteDraw(star, position, null, lightColor.MultiplyRGB(Color.SteelBlue).Additive() * mult, 0, star.Size() / 2, Projectile.scale * starScale, default);
				Main.EntitySpriteDraw(star, position, null, lightColor.MultiplyRGB(Color.White).Additive() * mult, 0, star.Size() / 2, Projectile.scale * 0.8f * starScale, default);
			}

			return false;
		}

		public Rectangle GetSweetSpot(int size)
		{
			var end = GetEndPosition().ToPoint();
			return new(end.X - size / 2, end.Y - size / 2, size, size);
		}
	}

	public override void SetStaticDefaults() => SpiritSets.IsSword[Type] = true;

	public override void SetDefaults()
	{
		Item.damage = 14;
		Item.knockBack = 3;
		Item.useTime = Item.useAnimation = 25;
		Item.DamageType = DamageClass.Melee;
		Item.width = Item.height = 46;
		Item.useStyle = ItemUseStyleID.Swing;
		Item.value = Item.sellPrice(gold: 1);
		Item.rare = ItemRarityID.Blue;
		Item.UseSound = SoundID.Item1;
		Item.shoot = ModContent.ProjectileType<FencingFoilSwing>();
		Item.shootSpeed = 1f;
		Item.autoReuse = true;
		Item.useTurn = true;
		Item.noUseGraphic = true;
		Item.noMelee = true;
		MoRHelper.SetSlashBonus(Item);
	}

	public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
	{
		SoundEngine.PlaySound(FencingFoilSwing.Slash with { Pitch = 1f, PitchVariance = 0.15f });
		SwungProjectile.Spawn(position, velocity, type, damage, knockback, player, 0, source, Main.rand.NextFromList(-1, 0, 1));
		return false;
	}
}