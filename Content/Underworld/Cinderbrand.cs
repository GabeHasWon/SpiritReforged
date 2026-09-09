using SpiritReforged.Common;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.ModCompat;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.PlayerCommon;
using SpiritReforged.Common.ProjectileCommon.Abstract;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Common.Visuals.Glowmasks;
using SpiritReforged.Content.Particles;
using Terraria.Audio;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Underworld;

[AutoloadGlowmask("255,255,255")]
public class Cinderbrand : ModItem
{
	public sealed class	CinderWave : ModProjectile, IDrawPixelated
	{
		public const int TIME_LEFT_MAX = 100;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.DD2SquireSonicBoom;

		public override void SetDefaults()
		{
			Projectile.Size = new(20);
			Projectile.friendly = true;
			Projectile.ignoreWater = true;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
			Projectile.timeLeft = TIME_LEFT_MAX;
			Projectile.extraUpdates = 1;
			Projectile.DamageType = DamageClass.Melee;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 20;
		}

		public override void AI()
		{
			Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
			Projectile.velocity *= 0.97f;

			float length = Projectile.velocity.Length();
			Projectile.scale = length / 10f;

			float intensity = length / 10f;
			if (intensity > 0.1f)
			{
				float progress = 1f - Projectile.timeLeft / (float)TIME_LEFT_MAX;
				Color[] colors = [Color.White, Color.Lerp(Color.Orange, Color.PaleVioletRed, progress), Color.Lerp(Color.Red, Color.Blue, progress)];

				Vector2 position = Projectile.Center + new Vector2(20 * Projectile.scale * Main.rand.NextFloat(-1, 1), -6).RotatedBy(Projectile.rotation);
				ParticleHandler.SpawnParticle(new FireParticle(position, Projectile.velocity, colors, intensity, 0.1f, Common.Easing.EaseFunction.EaseCircularOut, 30));
			}
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => target.AddBuff(BuffID.OnFire3, 300);

		public override bool PreDraw(ref Color lightColor) => false;

		void IDrawPixelated.DrawPixelated(SpriteBatch spriteBatch)
		{
			Texture2D texture = TextureAssets.Projectile[Type].Value;
			Vector2 position = Projectile.Center - Main.screenPosition;

			float progress = 1f - Projectile.timeLeft / (float)TIME_LEFT_MAX;
			float length = Projectile.velocity.Length() * 0.1f;
			Vector2 scale = new Vector2(Math.Max(1 - length, 0.5f), 0.5f + length * 2) * Projectile.scale;

			IDrawPixelated.PixelateDrawPosition(ref position);

			spriteBatch.Draw(texture, position - Projectile.velocity * 2, null, Color.Lerp(Color.OrangeRed, Color.Blue, progress).Additive() * 0.5f, Projectile.rotation, texture.Size() / 2, scale, 0, 0);
			spriteBatch.Draw(texture, position - Projectile.velocity, null, Color.Orange.Additive(50) * 0.8f, Projectile.rotation, texture.Size() / 2, scale, 0, 0);
			spriteBatch.Draw(texture, position, null, Color.White.Additive(50), Projectile.rotation, texture.Size() / 2, scale * 0.9f, 0, 0);
		}
	}

	public sealed class CinderbrandSwing : RapierProjectile, FreeDodgePlayer.IImmuneTo, IDrawPixelated
	{
		public enum MoveType { Lunge, Stance, Wave }

		public MoveType Move { get => (MoveType)Projectile.ai[0]; set => Projectile.ai[0] = (int)value; }
		public int FlourishDirection => (int)Projectile.ai[1];

		public override string Texture => ModContent.GetInstance<Cinderbrand>().Texture;
		public override LocalizedText DisplayName => ModContent.GetInstance<Cinderbrand>().DisplayName;

		private BasicNoiseCone _motionCone;

		public override IConfiguration SetConfiguration() => new RapierConfiguration(null, 80, 12, 12, 15);

		public Player.CompositeArmStretchAmount ParryStretch()
		{
			if (Move == MoveType.Stance)
				return Player.CompositeArmStretchAmount.Full;
			else
				return ProgressiveStretch();
		}

		public override void AI()
		{
			base.AI();

			if (!Main.dedServ && Move is MoveType.Lunge or MoveType.Wave)
			{
				_motionCone ??= (BasicNoiseCone)new BasicNoiseCone(Projectile.Center + Projectile.velocity * 10, Projectile.velocity, 20, new(50, 150)).SetColors(Color.White.Additive(50), Color.Orange).SetIntensity(3).AttachTo(Projectile);

				_motionCone.Position += _motionCone.Velocity; //Update activity
				_motionCone.Update();

				if (++_motionCone.TimeActive > _motionCone.MaxTime && _motionCone.MaxTime > 0)
					_motionCone.Kill();
			}
		}

		public bool ImmuneTo(PlayerDeathReason damageSource, int cooldownCounter, bool dodgeable)
		{
			if (Move != MoveType.Stance || Counter > FreeDodgeTime)
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

				SoundEngine.PlaySound(SoundID.Research with { Pitch = 0.9f }, Projectile.Center);
				SoundEngine.PlaySound(SoundID.Item35, Projectile.Center);
			}

			SwingArc = 2; //Initiate a swing
			Counter = 0;

			Projectile.timeLeft++;
			Projectile.knockBack *= 3;
			Move = MoveType.Wave;

			Player owner = Main.player[Projectile.owner];
			owner.velocity -= Projectile.velocity * 5;
			owner.SetImmuneTimeForAllTypes(30);

			if (Projectile.owner == Main.myPlayer)
			{
				Projectile.velocity = Projectile.DirectionTo(Main.MouseWorld);
				Projectile.netUpdate = true;

				Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, Projectile.velocity * 10, ModContent.ProjectileType<CinderWave>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
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
				return base.GetRotation(out armRotation, out stretch) + MathHelper.PiOver4;
			}
		}

		public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
		{
			base.ModifyHitNPC(target, ref modifiers);

			if (hitSweetSpot)
			{
				for (int i = 0; i < 5; i++)
				{
					float magnitude = Main.rand.NextFloat();
					ParticleHandler.SpawnParticle(new EmberParticle(GetEndPosition(), Projectile.velocity.RotatedByRandom(0.5f) * magnitude * -5f, Color.PaleVioletRed, 0.4f * (1f - magnitude), 30, 3));
				}

				_motionCone?.SetColors(Color.White.Additive(100), Color.PaleVioletRed);
			}
		}

		public override bool? CanDamage() => (Move == MoveType.Stance || Counter > 5) ? false : null;

		public override bool PreDraw(ref Color lightColor)
		{
			float offset = Move == MoveType.Lunge ? Math.Max(30 * (0.5f - Progress * 2), -2) : 0;
			float mult = 1f - Counter / 5f;

			DrawHeld(Projectile.GetAlpha(lightColor), new Vector2(0, TextureAssets.Projectile[Type].Value.Height) + new Vector2(-offset, offset), Projectile.rotation);

			if (Move == MoveType.Wave)
			{
				int direction = Projectile.spriteDirection * Math.Sign(SwingArc);
				SpriteEffects effects = direction == -1 ? SpriteEffects.FlipVertically : default;
				float rotation = Projectile.rotation - MathHelper.PiOver4 - 0.5f * direction;

				DrawSmear(Projectile.GetAlpha(lightColor.MultiplyRGB(Color.PaleVioletRed)) * 0.5f, rotation, (int)(Progress * 8f), GetConfig<RapierConfiguration>().Reach + 10, effects: effects);
				DrawSmear(Projectile.GetAlpha(lightColor.MultiplyRGB(Color.LightGray)), rotation, (int)(Progress * 12f), GetConfig<RapierConfiguration>().Reach + 10, effects: effects);
				DrawSmear(Projectile.GetAlpha(lightColor.MultiplyRGB(Color.White)) * 0.7f * (1f - Progress), rotation, (int)(Progress * 15f), GetConfig<RapierConfiguration>().Reach + 12, effects: effects);
			}

			if (mult > 0)
				DrawStar(lightColor, 0.8f, mult);

			return false;
		}

		void IDrawPixelated.DrawPixelated(SpriteBatch spriteBatch) => _motionCone?.CustomDraw(spriteBatch);
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
		Item.UseSound = RapierProjectile.DefaultSwing;
		Item.shoot = ModContent.ProjectileType<CinderbrandSwing>();
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
		CinderbrandSwing.MoveType moveType = (player.altFunctionUse == 2) ? CinderbrandSwing.MoveType.Stance : CinderbrandSwing.MoveType.Lunge;
		SwungProjectile.Spawn(position, velocity, type, damage, knockback, player, 0, source, (int)moveType);

		return false;
	}

	public override void ModifyTooltips(List<TooltipLine> tooltips) => tooltips.RemoveAll(static x => x.Mod == "Terraria" && x.Name == "CritChance"); //Remove the line indicating crit chance

	public override void AddRecipes() => CreateRecipe().AddRecipeGroup(RecipeGroupID.Wood, 4).AddIngredient(ItemID.SilverBar, 6).AddTile(TileID.Anvils).Register();
}