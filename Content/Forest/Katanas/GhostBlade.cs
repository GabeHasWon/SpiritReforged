using SpiritReforged.Common;
using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.ModCompat;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.PrimitiveRendering;
using SpiritReforged.Common.PrimitiveRendering.Trail_Components;
using SpiritReforged.Common.PrimitiveRendering.Trails;
using SpiritReforged.Common.ProjectileCommon.Abstract;
using SpiritReforged.Common.Visuals.Glowmasks;
using SpiritReforged.Content.Particles;
using System.IO;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Forest.Katanas;

public class GhostBlade : ModItem
{
	[AutoloadGlowmask("255,255,255", false)]
	public sealed class GhostShuriken : ModProjectile
	{
		private bool _returning;
		private bool _spawnedTrail;

		private readonly VertexTrail[] _trails = new VertexTrail[2];

		public override void SetDefaults()
		{
			Projectile.penetrate = -1;
			Projectile.friendly = true;
			Projectile.extraUpdates = 1;
			Projectile.timeLeft = 200;
		}

		public override void AI()
		{
			if (!Main.dedServ && !_spawnedTrail)
			{
				TrailSystem.ProjectileRenderer.CreateTrail(Projectile, _trails[0] = new VertexTrail(new StandardColorTrail(Color.Cyan.Additive(100)), new NoCap(), new EntityTrailPosition(Projectile), new DefaultShader(), 16, 50));
				TrailSystem.ProjectileRenderer.CreateTrail(Projectile, _trails[1] = new VertexTrail(new StandardColorTrail(Color.White.Additive(100)), new NoCap(), new EntityTrailPosition(Projectile), new DefaultShader(), 8, 50));
				
				_spawnedTrail = true;
			}

			if (Projectile.owner == Main.myPlayer && Projectile.numUpdates == 0 && _returning)
			{
				foreach (Projectile other in Main.ActiveProjectiles)
				{
					if (other.type == ModContent.ProjectileType<GhostBladeSwing>() && other.ModProjectile.Colliding(other.Hitbox, Projectile.Hitbox) == true)
					{
						Vector2 velocity = Projectile.DirectionTo(Main.MouseWorld) * Projectile.velocity.Length();

						for (int i = 0; i < 2; i++)
							Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, velocity.RotatedBy(0.2f * (i - 0.5f)).RotatedByRandom(0.1f), Projectile.type, Projectile.damage, Projectile.knockBack, Projectile.owner);

						Projectile.Kill();
						Projectile.netUpdate = true;
					} //Split mod
				}
			}

			Projectile.rotation += 0.2f * Projectile.direction;
			Projectile.Opacity = Math.Min(Projectile.timeLeft / 20f, 1);

			if (!Main.dedServ && Projectile.Opacity != 1)
			{
				foreach (VertexTrail trail in _trails)
					trail.Opacity = Projectile.Opacity;
			}
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			Projectile.velocity = Projectile.DirectionTo(Main.player[Projectile.owner].Center) * Projectile.velocity.Length();
			_returning = true;

			SpawnHitEffects();
			Projectile.netUpdate = true;
		}

		public override void OnKill(int timeLeft)
		{
			if (timeLeft > 0)
				SpawnHitEffects();
		}

		public void SpawnHitEffects()
		{
			if (Main.dedServ)
				return;

			ParticleHandler.SpawnParticle(new PulseCircle(Projectile.Center, Color.Cyan.Additive(), 0.2f, 100, 14, EaseFunction.EaseCubicOut));
			ParticleHandler.SpawnParticle(new PulseCircle(Projectile.Center, Color.White.Additive(), 0.1f, 100, 14, EaseFunction.EaseCubicOut));

			for (int i = 0; i < 8; i++)
				Dust.NewDustPerfect(Projectile.Center, DustID.BlueTorch, Main.rand.NextVector2Circular(4, 4), Scale: 2).noGravity = true;
		}

		public override bool PreDraw(ref Color lightColor)
		{
			Texture2D texture = TextureAssets.Projectile[Type].Value;
			Texture2D glowmask = GlowmaskProjectile.ProjIdToGlowmask[Type].Glowmask.Value;
			Rectangle source = texture.Frame();

			Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, source, Projectile.GetAlpha(lightColor), Projectile.rotation, source.Size() / 2, Projectile.scale, 0);
			Main.EntitySpriteDraw(glowmask, Projectile.Center - Main.screenPosition, source, Projectile.GetAlpha(Color.White).Additive(), Projectile.rotation, source.Size() / 2, Projectile.scale, 0);
			
			return false;
		}

		public override void SendExtraAI(BinaryWriter writer) => writer.Write(_returning);

		public override void ReceiveExtraAI(BinaryReader reader) => _returning = reader.ReadBoolean();
	}

	[AutoloadGlowmask("255,255,255", false)]
	public sealed class GhostBladeSwing : SwungProjectile
	{
		public override LocalizedText DisplayName => ModContent.GetInstance<GhostBlade>().DisplayName;

		public override void SetStaticDefaults() => Main.projFrames[Type] = 4;

		public override IConfiguration SetConfiguration() => new BasicConfiguration(EaseFunction.EaseCubicOut, 68, 40);

		public override bool PreDraw(ref Color lightColor)
		{
			SpriteEffects effects = (SwingDirection == -1) ? SpriteEffects.FlipVertically : default;
			Vector2 origin = new(10, 26); //The handle

			DrawHeld(lightColor, origin, Projectile.rotation, effects);
			DrawHeld(Color.White.Additive(), origin, Projectile.rotation, effects, texture: GlowmaskProjectile.ProjIdToGlowmask[Type].Glowmask.Value);

			return false;
		}
	}

	private float _swingArc;

	public override void SetStaticDefaults() => SpiritSets.IsSword[Type] = true;

	public override void SetDefaults()
	{
		Item.DefaultToSpear(ModContent.ProjectileType<GhostBladeSwing>(), 8, 18);
		Item.SetShopValues(ItemRarityColor.Blue1, Item.sellPrice(gold: 1, silver: 30));
		Item.useStyle = ItemUseStyleID.Swing;
		Item.damage = 12;
		Item.knockBack = 3;
		Item.autoReuse = true;
		MoRHelper.SetSlashBonus(Item);
	}

	public override bool AltFunctionUse(Player player) => player.ownedProjectileCounts[ModContent.ProjectileType<GhostShuriken>()] == 0;

	public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
	{
		if (player.altFunctionUse == 2)
		{
			Projectile.NewProjectile(source, position, velocity, ModContent.ProjectileType<GhostShuriken>(), damage, knockback, player.whoAmI);
			return false;
		}

		_swingArc = _swingArc switch
		{
			5f => -5f,
			_ => 5f
		};

		SwungProjectile.Spawn(position, Vector2.Normalize(velocity), type, damage, knockback, player, _swingArc, source, player.altFunctionUse - 1);
		return false;
	}
}