using SpiritReforged.Common;
using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.ModCompat;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.PlayerCommon;
using SpiritReforged.Common.ProjectileCommon.Abstract;
using SpiritReforged.Content.Particles;
using Terraria.Audio;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Forest.Rapiers;

public class Jian : ModItem
{
	public class JianSwing : RapierProjectile, FreeDodgePlayer.IFreeDodge
	{
		public enum MoveType { Lunge, Stance, Flurry }

		public MoveType Move { get => (MoveType)Projectile.ai[0]; set => Projectile.ai[0] = (int)value; }

		public override float SwingTime => Move switch
		{
			MoveType.Flurry => base.SwingTime * 0.5f,
			MoveType.Stance => FreeDodgeTime,
			_ => base.SwingTime
		};

		public override string Texture => ModContent.GetInstance<Jian>().Texture;
		public override LocalizedText DisplayName => ModContent.GetInstance<Jian>().DisplayName;

		private BasicNoiseCone _motionCone;

		public override IConfiguration SetConfiguration() => new RapierConfiguration(EaseFunction.EaseCubicOut, 84, 12, 12, 15);

		public override void AI()
		{
			base.AI();

			if (Move == MoveType.Flurry && Counter == SwingTime - 2)
			{
				if (Main.player[Projectile.owner].HeldItem.ModItem is Jian jian && (jian.combo = Math.Max(jian.combo - 0.1f, 0)) > 0)
					StartFlurryJab();
			}

			if (!Main.dedServ && Move != MoveType.Stance && Counter == 1)
			{
				Vector2 position = Projectile.Center + Projectile.velocity * 12;
				ParticleHandler.SpawnParticle(_motionCone = (BasicNoiseCone)new BasicNoiseCone(position, Projectile.velocity, 14, new(90, 150)).SetColors(Color.White.Additive(100), Color.Goldenrod).SetIntensity(2).AttachTo(Projectile));
			}
		}

		private void StartFlurryJab()
		{
			Counter = 0;
			Projectile.timeLeft++;
			Projectile.ResetLocalNPCHitImmunity();

			if (Projectile.owner == Main.myPlayer)
			{
				Projectile.velocity = Projectile.DirectionTo(Main.MouseWorld).RotatedByRandom(0.5f);
				Projectile.netUpdate = true;
			}

			SoundEngine.PlaySound(SoundID.DD2_WyvernDiveDown with { Pitch = 0.5f }, Projectile.Center);
		}

		public bool FreeDodge(Player.HurtInfo info)
		{
			if (Move != MoveType.Stance)
				return false;

			Move = MoveType.Flurry; //Initiate a flurry

			Player owner = Main.player[Projectile.owner];
			owner.velocity -= Projectile.velocity * 8;
			owner.SetImmuneTimeForAllTypes(30);

			StartFlurryJab();

			return true;
		}

		public override float GetRotation(out float armRotation, out Player.CompositeArmStretchAmount stretch)
		{
			if (Move == MoveType.Stance)
			{
				float value = GetAbsoluteAngle();
				armRotation = value - MathHelper.PiOver2;
				stretch = Player.CompositeArmStretchAmount.Full;

				return value + ((Projectile.direction == -1) ? MathHelper.Pi + MathHelper.PiOver2 : MathHelper.Pi);
			}
			else
			{
				return base.GetRotation(out armRotation, out stretch) + MathHelper.PiOver4;
			}
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			Player owner = Main.player[Projectile.owner];
			if (hitSweetSpot)
			{
				for (int i = 0; i < 5; i++)
				{
					float magnitude = Main.rand.NextFloat();
					ParticleHandler.SpawnParticle(new EmberParticle(GetEndPosition(), Projectile.velocity.RotatedByRandom(0.5f) * magnitude * -5f, Color.PaleVioletRed, 0.4f * (1f - magnitude), 30, 3));
				}

				_motionCone?.SetColors(Color.White.Additive(100), Color.PaleVioletRed);

				if (Move != MoveType.Flurry && owner.HeldItem.ModItem is Jian jian)
				{
					SoundEngine.PlaySound(SoundID.DD2_WitherBeastCrystalImpact with { Pitch = jian.combo });
					jian.combo = Math.Min(jian.combo + 0.1f, 1);
				}
			}

			if (Move == MoveType.Flurry)
				DuelistRose.ApplyEffect(owner, target, hit);
		}

		public override bool PreDraw(ref Color lightColor)
		{
			float offset = (Move != MoveType.Stance) ? Math.Max(30 * (0.5f - Progress * 2), -2) : 0;
			float mult = 1f - Counter / 5f;

			DrawHeld(Projectile.GetAlpha(lightColor), new Vector2(0, TextureAssets.Projectile[Type].Value.Height) + new Vector2(-offset, offset), Projectile.rotation);

			if (mult > 0)
				DrawStar(lightColor, 0.8f, mult);

			return false;
		}

		public override bool? CanDamage() => (Move == MoveType.Stance || Counter > 5) ? false : null;
	}

	public float combo;

	public override void SetStaticDefaults() => SpiritSets.IsSword[Type] = true;

	public override void SetDefaults()
	{
		Item.DefaultToSpear(ModContent.ProjectileType<JianSwing>(), 1f, 18);
		Item.SetShopValues(ItemRarityColor.Blue1, Item.sellPrice(silver: 50));
		Item.damage = 14;
		Item.knockBack = 3;
		Item.UseSound = RapierProjectile.DefaultSwing;
		Item.autoReuse = true;
		MoRHelper.SetSlashBonus(Item);
	}

	public override bool AltFunctionUse(Player player) => true;

	public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
	{
		JianSwing.MoveType moveType = (player.altFunctionUse == 2) ? JianSwing.MoveType.Stance : JianSwing.MoveType.Lunge;
		SwungProjectile.Spawn(position, velocity, type, damage, knockback, player, Main.rand.NextFloat(-0.1f, 0.1f), source, (int)moveType);

		return false;
	}

	public override void ModifyTooltips(List<TooltipLine> tooltips) => tooltips.RemoveAll(static x => x.Mod == "Terraria" && x.Name == "CritChance"); //Remove the line indicating crit chance
}