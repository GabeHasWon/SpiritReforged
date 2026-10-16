using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.PrimitiveRendering;
using SpiritReforged.Common.PrimitiveRendering.PrimitiveShape;
using SpiritReforged.Common.ProjectileCommon.Abstract;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Particles;
using Terraria.Audio;
using Terraria.Graphics;

namespace SpiritReforged.Content.Underground.Items;

public class Trickaxe : ModItem
{
	public class MiningMeterPlayer : ModPlayer
	{
		public static readonly Asset<Texture2D> Meter = DrawHelpers.RequestLocal<Trickaxe>("MiningMeter", false);

		public override void DrawPlayer(Camera camera)
		{
			if (Main.myPlayer != Player.whoAmI)
				return; //Local drawing only

			SpriteBatch spriteBatch = Main.spriteBatch;
			var position = (Main.LocalPlayer.Bottom + new Vector2(0, 10 + Main.LocalPlayer.gfxOffY) - Main.screenPosition).ToPoint().ToVector2();

			if (Main.LocalPlayer.HeldItem.ModItem is Trickaxe trickaxe) //Draw the full reload meter
			{
				Texture2D texture = Meter.Value;
				Rectangle backSource = texture.Frame(1, 2, 0, 0, 0, -2);
				float progress = (float)Math.Min(trickaxe._charges / (float)ChargesMax, 1f);

				spriteBatch.Draw(texture, position, backSource, Color.White, 0, backSource.Size() / 2, 1, 0, 0);
				Rectangle fillSource = texture.Frame(1, 2, 0, 1, 0, -2) with { Width = (int)(backSource.Width * progress) };

				spriteBatch.Draw(texture, position - new Vector2(backSource.Width / 2, 0), fillSource, Color.White, 0, new Vector2(0, fillSource.Height / 2), 1, 0, 0);
			}
		}
	}

	public class TrickaxeHeld : SwungProjectile
	{
		public override LocalizedText DisplayName => ModContent.GetInstance<Trickaxe>().DisplayName;

		public override string Texture => ModContent.GetInstance<Trickaxe>().Texture;

		public override IConfiguration SetConfiguration() => new BasicConfiguration(EaseFunction.EaseCubicOut, 50, 20);

		public override void AI()
		{
			base.AI();
			if (SwingArc == 0 && Counter == 1)
			{
				for (int i = 0; i < 4; i++)
					Dust.NewDustPerfect(GetEndPosition() + Main.rand.NextVector2Circular(8, 8), DustID.Smoke, Vector2.Normalize(Projectile.velocity));
			}
		}

		public override float GetRotation(out float armRotation, out Player.CompositeArmStretchAmount stretch)
		{
			float value = base.GetRotation(out armRotation, out stretch);
			return value + MathHelper.PiOver4 * ((SwingDirection == 0) ? 1 : SwingDirection);
		}

		public override bool PreDraw(ref Color lightColor)
		{
			SpriteEffects effects = (SwingDirection == -1) ? SpriteEffects.FlipVertically : SpriteEffects.None;
			Vector2 origin = new(4, (effects == SpriteEffects.FlipVertically) ? 4 : TextureAssets.Projectile[Type].Height() - 4); //The handle

			DrawHeld(lightColor, origin, Projectile.rotation, effects);
			return false;
		}
	}

	public class MiningCharge : ModProjectile, IDrawPixelated
	{
		private bool _collided;

		public override void SetStaticDefaults() => ProjectileID.Sets.Explosive[Type] = true;

		public override void SetDefaults()
		{
			Projectile.Size = new Vector2(4);
			Projectile.friendly = true;
			Projectile.penetrate = -1;
		}

		public override void AI()
		{
			Projectile.rotation += Projectile.velocity.X * 0.1f;

			if (++Projectile.ai[0] > 30)
				Projectile.velocity.Y += 0.5f;

			if (_collided)
			{
				const int detonation_time = 30;
				Projectile.timeLeft = Math.Min(Projectile.timeLeft, detonation_time);
			}
		}

		public override bool OnTileCollide(Vector2 oldVelocity)
		{
			_collided = true;
			return true;
		}

		public override void OnKill(int timeLeft)
		{
			Rectangle region = Projectile.Hitbox;
			region.Inflate(50, 50);

			if (Main.myPlayer == Projectile.owner)
			{
				(region.X, region.Y, region.Width, region.Height) = (region.X / 16, region.Y / 16, region.Width / 16, region.Height / 16); //Convert tile tile coordinates
				Projectile.ExplodeTiles(Projectile.Center, region.Width / 2, region.Left, region.Right, region.Top, region.Bottom, true);
			}

			if (!Main.dedServ)
			{
				for (int i = 0; i < 5; i++)
					ParticleHandler.SpawnParticle(new CompositeSmoke(Projectile.Center, Main.rand.NextVector2Circular(2, 2), Color.LightGray, Main.rand.Next(50, 70), false, false));

				for (int i = 0; i < 5; i++)
					ParticleHandler.SpawnParticle(new SmallCompositeSmoke(Projectile.Center, Main.rand.NextVector2Circular(2, 2), Color.DarkGray, 50, false, false));

				for (int i = 0; i < 8; i++)
					ParticleHandler.SpawnParticle(new EmberParticle(Projectile.Center, Main.rand.NextVector2Circular(4, 4), Color.OrangeRed, Color.Red, 1, 40, 3));

				SoundEngine.PlaySound(SoundID.DD2_ExplosiveTrapExplode, Projectile.Center);
			}
		}

		public override bool PreDraw(ref Color lightColor)
		{
			Texture2D texture = TextureAssets.Projectile[Type].Value;
			Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, Projectile.GetAlpha(lightColor), Projectile.rotation, texture.Size() / 2, Projectile.scale, 0);

			return false;
		}

		void IDrawPixelated.DrawPixelated(SpriteBatch spriteBatch)
		{
			if (Projectile.timeLeft <= 30)
			{
				float progress = 1f - Projectile.timeLeft / 30f;
				DrawRing(Projectile.Center, Color.Red.Additive(), Vector2.One * 100 * progress, 0.1f, progress);
				DrawRing(Projectile.Center, Color.Red.Additive() * 0.5f, Vector2.One * 100 * progress, 0.3f, progress);
			}
		}

		public static void DrawRing(Vector2 position, Color color, Vector2 scale, float thickness, float progress)
		{
			Effect effect = AssetLoader.LoadedShaders["PulseCircle"].Value;
			effect.Parameters["RingColor"].SetValue(color.ToVector4());
			effect.Parameters["BloomColor"].SetValue(color.ToVector4());
			effect.Parameters["RingWidth"].SetValue(thickness * MathHelper.Lerp(1, 0, EaseFunction.EaseCubicIn.Ease(progress)));

			SquarePrimitive square = new()
			{
				Color = color * EaseFunction.EaseCubicOut.Ease(progress),
				Height = scale.X,
				Length = scale.Y,
				Position = position - Main.screenPosition,
				Rotation = 0,
				ColorXCoordMod = 1
			};

			PrimitiveRenderer.DrawPrimitiveShape(square, effect, "GeometricStyle");
		}
	}

	public const int ChargesMax = 10;
	private int _charges;

	public override void Load() => On_Player.PickTile += On_Player_PickTile;

	private static void On_Player_PickTile(On_Player.orig_PickTile orig, Player self, int x, int y, int pickPower)
	{
		orig(self, x, y, pickPower);

		if (!Main.tile[x, y].HasTile && self.HeldItem.ModItem is Trickaxe trickaxe)
			trickaxe._charges++;
	}

	public override void SetDefaults()
	{
		Item.CloneDefaults(ItemID.SilverPickaxe);
		Item.rare = ItemRarityID.Blue;
		Item.Size = new Vector2(30);
		Item.noUseGraphic = true;
		Item.noMelee = true;
	}

	public override bool AltFunctionUse(Player player) => _charges >= ChargesMax;

	public override bool? UseItem(Player player)
	{
		int damage = player.GetWeaponDamage(Item);
		float knockback = player.GetWeaponKnockback(Item);

		if (player.whoAmI == Main.myPlayer)
		{
			Vector2 velocity = player.DirectionTo(Main.MouseWorld) * 8f;
			float swingArc = 3.5f;

			if (player.altFunctionUse == 2)
			{
				_charges = 0;
				Projectile.NewProjectile(player.GetSource_ItemUse(Item), player.Center, velocity, ModContent.ProjectileType<MiningCharge>(), damage, knockback, player.whoAmI);
				swingArc = 0;
			}

			SwungProjectile.Spawn(player.Center, velocity, ModContent.ProjectileType<TrickaxeHeld>(), damage, knockback, player, swingArc, player.GetSource_ItemUse(Item));
		}

		return true;
	}
}