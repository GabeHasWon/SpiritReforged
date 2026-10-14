using SpiritReforged.Common.Easing;
using SpiritReforged.Common.PlayerCommon;
using SpiritReforged.Common.PrimitiveRendering;
using SpiritReforged.Common.PrimitiveRendering.Trail_Components;
using SpiritReforged.Common.PrimitiveRendering.Trails;
using SpiritReforged.Common.ProjectileCommon;
using SpiritReforged.Common.ProjectileCommon.Abstract;
using SpiritReforged.Common.Visuals;
using Terraria.Audio;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Underground.Items;

public class Lumberjack : ModItem
{
	public class LumberjackSaw : ModProjectile, IDrawPixelated
	{
		public const int FALL_TIME = 200;

		public override LocalizedText DisplayName => ModContent.GetInstance<Lumberjack>().DisplayName;

		public override string Texture => ModContent.GetInstance<LumberjackHeld>().Texture;

		private VertexTrail _trail;

		public override void SetDefaults()
		{
			Projectile.friendly = true;
			Projectile.Size = new Vector2(20);
			Projectile.penetrate = -1;
			Projectile.timeLeft = FALL_TIME + 50;
			Projectile.extraUpdates = 1;
			Projectile.hide = true;
		}

		public override void AI()
		{
			if (!Main.dedServ)
			{
				_trail ??= new VertexTrail(new GradientTrail(Color.LightGray, Color.Transparent), new NoCap(), new EntityTrailPosition(Projectile), new DefaultShader(), 5, 40, -1, (factor) => 5f);
				_trail.Update();
			}

			Projectile.rotation += 0.5f * Math.Clamp(Projectile.velocity.X, -1, 1);

			if (Projectile.timeLeft <= FALL_TIME)
			{
				Projectile.velocity.X *= 0.99f;
				Projectile.velocity.Y += 0.5f;

				if (Projectile.timeLeft <= 10)
					Projectile.scale *= 0.9f;
			}
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => Projectile.Bounce(Projectile.velocity);

		public override bool OnTileCollide(Vector2 oldVelocity)
		{
			if (Projectile.timeLeft > FALL_TIME)
			{
				Projectile.Bounce(oldVelocity);
			}
			else
			{
				_trail?.Dissolve();

				Vector2 center = Projectile.Center;
				Projectile.Size = new Vector2(10);
				Projectile.Center = center; //The shrink

				Projectile.velocity *= 0.8f;
			}

			return false;
		}

		public override bool PreDraw(ref Color lightColor)
		{
			Texture2D texture = TextureAssets.Projectile[Type].Value;
			Rectangle source = new(42, 0, 30, 30);

			Vector2 position = Projectile.Center - Main.screenPosition + new Vector2(0, Projectile.gfxOffY);
			Main.EntitySpriteDraw(texture, position, source, lightColor, Projectile.rotation, source.Size() / 2, Projectile.scale, 0);

			return false;
		}

		public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI) => behindNPCsAndTiles.Add(index);

		void IDrawPixelated.DrawPixelated(SpriteBatch spriteBatch) => _trail?.Draw(TrailSystem.TrailShaders, Main.graphics.GraphicsDevice, Matrix.Identity);
	}

	public class LumberjackHeld : SwungProjectile, IDrawPixelated
	{
		public override LocalizedText DisplayName => ModContent.GetInstance<Lumberjack>().DisplayName;

		public override float SwingTime => base.SwingTime * 1.5f;

		private float _spinSpeed;
		private bool _launchedSaw;

		public override IConfiguration SetConfiguration()
		{
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 10;

			return new BasicConfiguration(EaseFunction.EaseSine, 50, 25);
		}

		public override void AI()
		{
			Player owner = Main.player[Projectile.owner];

			Projectile.velocity = owner.DirectionTo(PlayerMouseHandler.GetMouse(Projectile.owner));
			Projectile.spriteDirection = Projectile.direction = owner.direction = (Projectile.velocity.X > 0) ? 1 : -1;
			Projectile.rotation = GetRotation(out float armRotation, out Player.CompositeArmStretchAmount stretch);
			Projectile.Center = owner.GetFrontHandPosition(stretch, armRotation);

			owner.SetCompositeArmFront(true, stretch, armRotation);
			owner.heldProj = Projectile.whoAmI;

			if (owner.channel)
			{
				Counter--;
				_spinSpeed = Math.Min(_spinSpeed + 0.02f, 1);

				if (++Projectile.localAI[0] % 30 == 0)
					SoundEngine.PlaySound(SoundID.Item22 with { Pitch = _spinSpeed * 0.1f }, Projectile.Center);
			}
			else
			{
				if (!_launchedSaw && _spinSpeed == 1)
				{
					_launchedSaw = true;
					SoundEngine.PlaySound(SoundID.Item23 with { Pitch = 0.5f }, Projectile.Center);

					if (Projectile.owner == Main.myPlayer)
						Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, Projectile.velocity * 10, ModContent.ProjectileType<LumberjackSaw>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
				}

				_spinSpeed = Math.Min(1f - Progress, 0.9f);
			}

			if (++Counter < SwingTime - 2)
				owner.itemAnimation = owner.itemTime = Projectile.timeLeft = 2;

			if (Main.rand.NextBool(3))
				Dust.NewDustPerfect(GetEndPosition(-24), DustID.Smoke, (Vector2.UnitY * -Main.rand.NextFloat(1, 2)).RotatedByRandom(0.3f), 100).noGravity = true;
		}

		public override bool PreDraw(ref Color lightColor)
		{
			Texture2D texture = TextureAssets.Projectile[Type].Value;
			SpriteEffects effects = SwingDirection == -1 ? SpriteEffects.FlipVertically : default;
			Rectangle source = new(0, 6, 40, 22);
			Vector2 origin = new(4, source.Height / 2); //The handle

			Vector2 position = Projectile.Center - Main.screenPosition + new Vector2(0, Projectile.gfxOffY);
			Main.EntitySpriteDraw(texture, position, source, lightColor, Projectile.rotation, origin, Projectile.scale, effects);

			return false;
		}

		private void DrawSawblade(SpriteBatch spriteBatch)
		{
			Texture2D texture = TextureAssets.Projectile[Type].Value;
			SpriteEffects effects = SwingDirection == -1 ? SpriteEffects.FlipVertically : default;
			Rectangle source = new(42, 0, 30, 30);
			float rotation = Projectile.localAI[0] * _spinSpeed * 0.5f;
			float scale = (Projectile.scale + EaseFunction.EaseSine.Ease(Math.Max(_spinSpeed - 0.8f, 0) / 0.2f) * 0.5f) / 2;

			Vector2 position = Projectile.Center - Main.screenPosition + new Vector2(42, 0).RotatedBy(Projectile.rotation);
			Vector2 mainPosition = position;
			IDrawPixelated.PixelateDrawPosition(ref mainPosition);

			DrawHelpers.DrawOutline(default, default, default, default, (offset) =>
			{
				Vector2 outlinePosition = position + offset;
				IDrawPixelated.PixelateDrawPosition(ref outlinePosition);

				spriteBatch.Draw(texture, outlinePosition, source, Projectile.GetAlpha(Color.Black) * 0.25f, rotation, source.Size() / 2, scale, effects, 0);
			});

			spriteBatch.Draw(texture, mainPosition, source, Projectile.GetAlpha(Lighting.GetColor((position + Main.screenPosition).ToTileCoordinates())), rotation, source.Size() / 2, scale, effects, 0);
		}

		void IDrawPixelated.DrawPixelated(SpriteBatch spriteBatch)
		{
			if (!_launchedSaw)
				DrawSawblade(spriteBatch);
		}
	}

	public override void SetDefaults()
	{
		Item.Size = new(38, 40);
		Item.damage = 12;
		Item.useTime = Item.useAnimation = 10;
		Item.useStyle = ItemUseStyleID.Swing;
		Item.UseSound = SoundID.Item23;
		Item.noUseGraphic = true;
		Item.noMelee = true;
		Item.channel = true;
		Item.DamageType = DamageClass.Ranged;
		Item.knockBack = 4;
		Item.useTurn = true;
		Item.rare = ItemRarityID.Blue;
		Item.shootSpeed = 1;
		Item.shoot = ModContent.ProjectileType<LumberjackHeld>();
		Item.axe = 20;
	}

	public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
	{
		SwungProjectile.Spawn(position, velocity, type, damage, knockback, player, 0.5f, source);
		return false;
	}
}