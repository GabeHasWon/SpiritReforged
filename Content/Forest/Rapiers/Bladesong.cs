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

public class Bladesong : ModItem
{
	public class BladesongSwing : RapierProjectile, FreeDodgePlayer.IImmuneTo
	{
		public enum MoveType { Swing, Vanish }

		public MoveType Move { get => (MoveType)Projectile.ai[0]; set => Projectile.ai[0] = (int)value; }

		public override float SwingTime => (Move == MoveType.Vanish) ? FreeDodgeTime : base.SwingTime * 1.5f;

		public override string Texture => ModContent.GetInstance<Bladesong>().Texture;
		public override LocalizedText DisplayName => ModContent.GetInstance<Bladesong>().DisplayName;

		private BasicNoiseCone _motionCone;

		public override IConfiguration SetConfiguration() => new RapierConfiguration(EaseFunction.EaseCubicInOut, 78, 12, 18, 15);

		public override void AI()
		{
			const int reach = 150;
			const float start_swing = 0.1f;
			const float end_swing = 0.7f;

			if (Counter == 10)
				SoundEngine.PlaySound(SoundID.DD2_WyvernDiveDown, Projectile.Center);

			base.AI();

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

			/*if (!Main.dedServ && Move == MoveType.Swing && Counter == 1)
			{
				Vector2 position = Projectile.Center - Projectile.velocity * 8;
				ParticleHandler.SpawnParticle(_motionCone = (BasicNoiseCone)new BasicNoiseCone(position, Projectile.velocity, 14, new(50, 150)).SetColors(Color.White.Additive(100), Color.SteelBlue).SetIntensity(2).AttachTo(Projectile));
			}*/
		}

		public bool ImmuneTo(PlayerDeathReason damageSource, int cooldownCounter, bool dodgeable)
		{
			if (Move != MoveType.Vanish)
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
			Move = MoveType.Swing;

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

		public override float GetRotation(out float armRotation, out Player.CompositeArmStretchAmount stretch) => base.GetRotation(out armRotation, out stretch) + MathHelper.PiOver4 + Math.Max((Progress - 0.7f) / 0.3f, 0) * SwingDirection;

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (hitSweetSpot)
			{
				for (int i = 0; i < 5; i++)
				{
					float magnitude = Main.rand.NextFloat();
					ParticleHandler.SpawnParticle(new EmberParticle(GetEndPosition(), Projectile.velocity.RotatedByRandom(0.5f) * magnitude * -5f, Color.PaleVioletRed, 0.4f * (1f - magnitude), 30, 3));
				}

				_motionCone?.SetColors(Color.White.Additive(100), Color.PaleVioletRed);
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

		public override bool? CanDamage() => null;
	}

	private int _swingDirection = 1;

	public override void SetStaticDefaults() => SpiritSets.IsSword[Type] = true;

	public override void SetDefaults()
	{
		Item.DefaultToSpear(ModContent.ProjectileType<BladesongSwing>(), 1f, 30);
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
		BladesongSwing.MoveType moveType = (player.altFunctionUse == 2) ? BladesongSwing.MoveType.Vanish : BladesongSwing.MoveType.Swing;
		SwungProjectile.Spawn(position, velocity, type, damage, knockback, player, 5 * _swingDirection, source, (int)moveType);

		_swingDirection = -_swingDirection;
		return false;
	}

	public override void ModifyTooltips(List<TooltipLine> tooltips) => tooltips.RemoveAll(static x => x.Mod == "Terraria" && x.Name == "CritChance"); //Remove the line indicating crit chance

	public override void AddRecipes() => CreateRecipe().AddRecipeGroup("Tier2HMBars", 5).AddIngredient(ItemID.SoulofFlight, 10).AddIngredient(ItemID.Feather, 5).AddTile(TileID.MythrilAnvil).Register();
}