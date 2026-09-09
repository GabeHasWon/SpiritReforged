using SpiritReforged.Common;
using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.ModCompat;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.PlayerCommon;
using SpiritReforged.Common.ProjectileCommon.Abstract;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Particles;
using Terraria.Audio;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Forest.Rapiers;

public class Bladesong : ModItem
{
	public class BladesongMimic : ModProjectile
	{
		public int TargetWhoAmI
		{
			get => (int)Projectile.ai[0];
			set => Projectile.ai[0] = value;
		}

		public ref float Counter => ref Projectile.ai[1];

		public override string Texture => ModContent.GetInstance<Bladesong>().Texture;

		public override LocalizedText DisplayName => ModContent.GetInstance<Bladesong>().DisplayName;

		public override void SetDefaults()
		{
			Projectile.friendly = true;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.DamageType = DamageClass.MeleeNoSpeed;

			Projectile.scale = 0;
			Projectile.Opacity = 0;
		}

		public override void AI()
		{
			if (Main.npc[TargetWhoAmI] is NPC target && target.active)
			{
				Projectile.rotation = Projectile.AngleTo(target.Center);
				if (++Counter > 30)
				{
					Projectile.velocity = Vector2.Lerp(Projectile.velocity, Projectile.DirectionTo(target.Center) * 10, 0.1f);

					if (!Main.dedServ && Counter == 31)
					{
						Vector2 position = Projectile.Center - Projectile.velocity * 30;
						ParticleHandler.SpawnParticle(new BasicNoiseCone(position, Projectile.velocity, 20, new(80, 150)).SetColors(Color.White.Additive(50), Color.Cyan.Additive()).SetIntensity(3).AttachTo(Projectile));
					}

					if (Main.rand.NextBool(3))
						ParticleHandler.SpawnParticle(new SharpStarParticle(Projectile.Center + Main.rand.NextVector2Circular(20, 20), Projectile.velocity * 0.5f, Color.Cyan, 0.2f, 20, 0.1f));
				}
				else
				{
					Projectile.Center = Vector2.Lerp(Projectile.Center, target.Center - Projectile.velocity * 90, 0.1f);
				}
			}

			Projectile.Opacity = Math.Min(Projectile.Opacity + 0.05f, 1);
			Projectile.scale = Math.Min(Projectile.scale + 0.03f, 0.75f);
		}

		public override bool? CanDamage() => (Counter > 30) ? null : false;

		public override bool PreDraw(ref Color lightColor)
		{
			Texture2D texture = TextureAssets.Projectile[Type].Value;
			Vector2 origin = new(texture.Width - Projectile.width / 2, 0 - Projectile.height / 2);

			DrawHelpers.DrawOutline(default, default, default, default, (offset) =>
				Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition + offset, null, Projectile.GetAlpha(Color.Cyan).Additive(100), Projectile.rotation + MathHelper.PiOver4, origin, Projectile.scale, 0));

			Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition - Projectile.velocity * 2, null, Projectile.GetAlpha(Color.Cyan).Additive(), Projectile.rotation + MathHelper.PiOver4, origin, Projectile.scale, 0);
			Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, Projectile.GetAlpha(Color.White), Projectile.rotation + MathHelper.PiOver4, origin, Projectile.scale, 0);

			return false;
		}
	}

	public class BladesongSwing : RapierProjectile, FreeDodgePlayer.IImmuneTo
	{
		public enum MoveType { Swing, Stance, Vanish }

		public MoveType Move { get => (MoveType)Projectile.ai[0]; set => Projectile.ai[0] = (int)value; }

		public override float SwingTime => (Move is MoveType.Stance or MoveType.Vanish) ? FreeDodgeTime : base.SwingTime * 1.5f;

		public override string Texture => ModContent.GetInstance<Bladesong>().Texture;

		public override LocalizedText DisplayName => ModContent.GetInstance<Bladesong>().DisplayName;

		public override IConfiguration SetConfiguration() => new RapierConfiguration(EaseFunction.EaseCubicInOut, 78, 12, 18, 15);

		public override void AI()
		{
			const int reach = 150;
			const float start_swing = 0.1f;
			const float end_swing = 0.7f;

			base.AI();

			if (Move == MoveType.Swing)
			{
				if (Counter == 10)
					SoundEngine.PlaySound(SoundID.DD2_WyvernDiveDown, Projectile.Center);

				if (Progress >= end_swing)
					Projectile.scale *= 0.97f;

				if (Progress is > start_swing and < end_swing)
				{
					Player owner = Main.player[Projectile.owner];
					float progress = (Progress - start_swing) / (end_swing - start_swing);
					Projectile.Center = owner.Center + Projectile.velocity.RotatedBy((progress - 0.5f) * SwingDirection) * reach * EaseFunction.EaseCircularOut.Ease(EaseFunction.EaseSine.Ease(progress));

					owner.SetCompositeArmFront(false, 0, 0);

					if (!Main.dedServ)
					{
						Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Electric, Scale: 0.5f);
						dust.noGravity = true;
						dust.velocity = Projectile.velocity * 3;

						if (Main.rand.NextBool())
							ParticleHandler.SpawnParticle(new CompositeSmoke(Projectile.Center, Projectile.velocity * Main.rand.NextFloat(3f), Color.Cyan, 20));

						if (Main.rand.NextBool(3))
							ParticleHandler.SpawnParticle(new SmallCompositeSmoke(Projectile.Center, Projectile.velocity * Main.rand.NextFloat(3f), Color.White, 25));
					}
				}
			}
		}

		public bool ImmuneTo(PlayerDeathReason damageSource, int cooldownCounter, bool dodgeable)
		{
			if (Move != MoveType.Stance)
				return false;

			if (!Main.dedServ)
			{
				Vector2 position = Projectile.Center + Projectile.velocity * (GetConfig<RapierConfiguration>().Reach - 12);

				if (damageSource.TryGetCausingEntity(out Entity entity))
					position = entity.Center;

				float rotation = Projectile.AngleTo(position) + Main.rand.NextFloat(-1f, 1f);

				ParticleHandler.SpawnParticle(new ImpactLinePrim(position, Vector2.Zero, Color.PaleVioletRed.Additive() * 0.5f, new Vector2(0.5f, 1) * 2.5f, 5, 0) { Rotation = rotation, NoLight = true });
				ParticleHandler.SpawnParticle(new ImpactLinePrim(position, Vector2.Zero, Color.SteelBlue.Additive(), new Vector2(0.3f, 1) * 2, 5, 0) { Rotation = rotation, NoLight = true });
				ParticleHandler.SpawnParticle(new ImpactLinePrim(position, Vector2.Zero, Color.White.Additive(), new Vector2(0.3f, 1) * 1.5f, 5, 0) { Rotation = rotation, NoLight = true });
				ParticleHandler.SpawnParticle(new LightBurst(position, 0, Color.PaleVioletRed.Additive(), 0.4f, 10) { noLight = true });

				SoundEngine.PlaySound(SoundID.Research with { Pitch = 0.9f }, Projectile.Center);
				SoundEngine.PlaySound(SoundID.Item35, Projectile.Center);
			}

			SwingArc = 3; //Initiate a swing
			Counter = 0;

			Projectile.timeLeft++;
			Move = MoveType.Vanish;

			Player owner = Main.player[Projectile.owner];
			owner.velocity -= Projectile.velocity * 8;
			owner.SetImmuneTimeForAllTypes(30);

			if (Projectile.owner == Main.myPlayer)
			{
				Projectile.velocity = Projectile.DirectionTo(Main.MouseWorld);
				Projectile.netUpdate = true;
			}

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
				return base.GetRotation(out armRotation, out stretch) + MathHelper.PiOver4 + Math.Max((Progress - 0.7f) / 0.3f, 0) * SwingDirection;
			}
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (hitSweetSpot)
			{
				for (int i = 0; i < 5; i++)
				{
					float magnitude = Main.rand.NextFloat();
					ParticleHandler.SpawnParticle(new EmberParticle(GetEndPosition(), Projectile.velocity.RotatedByRandom(0.5f) * magnitude * -5f, Color.PaleVioletRed, 0.4f * (1f - magnitude), 30, 3));
				}

				Projectile.NewProjectile(Projectile.GetSource_OnHit(target), target.Center, Main.rand.NextVector2CircularEdge(1, 1), ModContent.ProjectileType<BladesongMimic>(), Projectile.damage, Projectile.knockBack, Projectile.owner, target.whoAmI);
			}

			if (Move == MoveType.Swing)
				DuelistRose.ApplyEffect(Main.player[Projectile.owner], target, hit);
		}

		public override bool PreDraw(ref Color lightColor)
		{
			int direction = Projectile.spriteDirection * Math.Sign(SwingArc);
			float progress = Progress - 0.2f;
			float rotation = Projectile.rotation - MathHelper.PiOver4 - 0.5f * direction + progress * SwingDirection * 2;
			SpriteEffects effects = (direction == -1) ? SpriteEffects.FlipVertically : default;

			if (Move == MoveType.Swing)
			{
				DrawCustomSmear(Projectile.GetAlpha(lightColor.MultiplyRGB(Color.Cyan)).Additive() * 0.5f, (int)(progress * 20f), rotation, effects: effects);
				DrawCustomSmear(Projectile.GetAlpha(lightColor.MultiplyRGB(Color.LightBlue)).Additive() * 0.7f * (1f - progress), (int)(progress * 30f), rotation, effects: effects);
			}

			DrawHeld(Projectile.GetAlpha(Color.LightBlue).Additive() * 0.5f, new Vector2(0, TextureAssets.Projectile[Type].Value.Height) - new Vector2(-5, 5), Projectile.rotation);
			DrawHeld(Projectile.GetAlpha(lightColor), new Vector2(0, TextureAssets.Projectile[Type].Value.Height), Projectile.rotation);

			if (Move == MoveType.Swing)
			{
				DrawCustomSmear(Projectile.GetAlpha(lightColor.MultiplyRGB(Color.White)).Additive() * 0.8f * progress, Math.Max((int)(progress * 30f), 2), rotation, effects: effects);
			}

			float mult = 1f - Progress;
			if (mult > 0)
				DrawStar(lightColor, 0.8f, mult, Color.Cyan);

			return false;
		}

		private void DrawCustomSmear(Color color, int frame, float rotation, SpriteEffects effects)
		{
			Main.instance.LoadProjectile(985);
			Texture2D smear = TextureAssets.Projectile[985].Value;

			float distance = ScaledReach + 10;
			Rectangle source = smear.Frame(1, 4, 0, frame);
			Vector2 position = Projectile.Center + (Vector2.UnitX * distance).RotatedBy(rotation) - Main.screenPosition;

			Main.EntitySpriteDraw(smear, position, source, color, rotation, new Vector2(source.Width, source.Height / 2), 0.75f, effects, 0);
		}

		public override bool? CanDamage() => (Move == MoveType.Stance) ? false : null;
	}

	private int _swingDirection = 1;

	public override void SetStaticDefaults() => SpiritSets.IsSword[Type] = true;

	public override void SetDefaults()
	{
		Item.DefaultToSpear(ModContent.ProjectileType<BladesongSwing>(), 1f, 25);
		Item.SetShopValues(ItemRarityColor.LightRed4, Item.sellPrice(gold: 3));
		Item.damage = 50;
		Item.knockBack = 3;
		Item.UseSound = null;
		Item.autoReuse = true;
		MoRHelper.SetSlashBonus(Item);
	}

	public override bool AltFunctionUse(Player player) => true;

	public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
	{
		BladesongSwing.MoveType moveType = (player.altFunctionUse == 2) ? BladesongSwing.MoveType.Stance : BladesongSwing.MoveType.Swing;
		SwungProjectile.Spawn(position, velocity, type, damage, knockback, player, (player.altFunctionUse == 2) ? 0 : 5 * _swingDirection, source, (int)moveType);

		_swingDirection = -_swingDirection;
		return false;
	}

	public override void ModifyTooltips(List<TooltipLine> tooltips) => tooltips.RemoveAll(static x => x.Mod == "Terraria" && x.Name == "CritChance"); //Remove the line indicating crit chance

	public override void AddRecipes() => CreateRecipe().AddRecipeGroup("Tier2HMBars", 5).AddIngredient(ItemID.SoulofFlight, 10).AddIngredient(ItemID.Feather, 5).AddTile(TileID.MythrilAnvil).Register();
}