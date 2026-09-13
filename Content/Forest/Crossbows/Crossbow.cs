using SpiritReforged.Common.ProjectileCommon.Abstract;
using SpiritReforged.Common.Visuals;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.Graphics;

namespace SpiritReforged.Content.Forest.Crossbows;

public class Crossbow : ModItem
{
	public sealed class ReloadPlayer : ModPlayer
	{
		public static readonly Asset<Texture2D> Meter = DrawHelpers.RequestLocal<Crossbow>("ReloadMeter", false);

		public bool Reloading => _reloadTime > 0;

		public int PerfectRange => (int)_perfectRange.ApplyTo(10);

		private StatModifier _perfectRange = StatModifier.Default;
		private int _reloadTime, _reloadTimeMax, _perfectReloadSpot;

		public void StartReload(int duration)
		{
			_reloadTime = _reloadTimeMax = duration;
			_perfectReloadSpot = Main.rand.Next(PerfectRange, (int)(duration / 1.5f));
		}

		public override void DrawPlayer(Camera camera)
		{
			if (Main.LocalPlayer.TryGetModPlayer(out ReloadPlayer reloadPlayer) && reloadPlayer.Reloading)
			{
				SpriteBatch spriteBatch = Main.spriteBatch;
				Texture2D texture = Meter.Value;

				Rectangle backSource = texture.Frame(1, 2, 0, 0, 0, -2);
				Vector2 position = Main.LocalPlayer.Bottom + new Vector2(0, 10 + Main.LocalPlayer.gfxOffY) - Main.screenPosition;
				float progress = 1f - (float)reloadPlayer._reloadTime / reloadPlayer._reloadTimeMax;

				spriteBatch.Draw(texture, position, backSource, Color.White, 0, backSource.Size() / 2, 1, 0, 0); //Draw meter background

				DrawPerfectReload(spriteBatch, position, backSource, reloadPlayer);

				float halfProgress = backSource.Width * progress / 2;
				Rectangle fillSource = texture.Frame(1, 2, 0, 1, 0, -2) with { Width = (int)halfProgress };

				spriteBatch.Draw(texture, position, fillSource, Color.White, 0, new Vector2(fillSource.Width, fillSource.Height / 2), 1, 0, 0); //Draw meter fill left
				spriteBatch.Draw(texture, position, fillSource, Color.White, 0, new Vector2(0, fillSource.Height / 2), 1, SpriteEffects.FlipHorizontally, 0); //Draw meter fill right
			}
		}

		private static void DrawPerfectReload(SpriteBatch spriteBatch, Vector2 origin, Rectangle barRegion, ReloadPlayer modPlayer)
		{
			int width = barRegion.Width / 2;
			float progress = 1f - (float)modPlayer._perfectReloadSpot / modPlayer._reloadTimeMax;

			Texture2D pixel = TextureAssets.MagicPixel.Value; //Draw perfect reload
			Rectangle source = new(0, 0, 1, 2);
			Vector2 stretch = new(width * ((float)modPlayer.PerfectRange / modPlayer._reloadTimeMax), 1);
			Vector2 position = origin + new Vector2(progress * width, 0);

			//Draw right side
			DrawHelpers.DrawOutline(default, default, default, default, (offset) =>
				spriteBatch.Draw(pixel, position + offset, source, Color.Goldenrod * 0.4f, 0, source.Size() / 2f, stretch, 0, 0));

			spriteBatch.Draw(pixel, position, source, Color.Yellow, 0, source.Size() / 2f, stretch, 0, 0); //Draw right

			//Draw left side
			position = origin - new Vector2(progress * width, 0);
			DrawHelpers.DrawOutline(default, default, default, default, (offset) =>
				spriteBatch.Draw(pixel, position + offset, source, Color.Goldenrod * 0.4f, 0, source.Size() / 2f, stretch, 0, 0));

			spriteBatch.Draw(pixel, position, source, Color.Yellow, 0, source.Size() / 2f, stretch, 0, 0); //Draw left
		}

		public override void ResetEffects()
		{
			_reloadTime = Math.Max(_reloadTime - 1, 0);

			if (Reloading && Main.mouseLeft && Main.mouseLeftRelease)
			{
				Rectangle perfectRectangle = new(_perfectReloadSpot - PerfectRange / 2, 0, PerfectRange, 0);
				if (_reloadTime > perfectRectangle.Left && _reloadTime < perfectRectangle.Right)
				{
					_reloadTime = 0; //Perform a perfect reload
					SoundEngine.PlaySound(SoundID.MaxMana);
				}
			}
		}
	}

	public class CrossbowHeld : SwungProjectile
	{
		public override float SwingTime
		{
			get
			{
				Player owner = Main.player[Projectile.owner];
				return (owner.TryGetModPlayer(out ReloadPlayer reloadPlayer) && reloadPlayer.Reloading) ? 80 : base.SwingTime;
			}
		}

		public override LocalizedText DisplayName => ModContent.GetInstance<Crossbow>().DisplayName;

		public override string Texture => ModContent.GetInstance<Crossbow>().Texture;

		public override IConfiguration SetConfiguration() => new BasicConfiguration(null, 30, 10);

		public override void AI()
		{
			base.AI();

			Player owner = Main.player[Projectile.owner];
			if (owner.TryGetModPlayer(out ReloadPlayer reloadPlayer) && !reloadPlayer.Reloading)
			{
				if (Counter == 1)
				{
					for (int i = 0; i < 8; i++)
						Dust.NewDustPerfect(GetEndPosition(-8), Main.rand.NextFromList(DustID.GoldCoin, DustID.Smoke), (Vector2.Normalize(Projectile.velocity) * Main.rand.NextFloat(3f)).RotatedByRandom(1f)).noGravity = true;
				}

				if (Counter == SwingTime - 3)
				{
					reloadPlayer.StartReload(80); //Start reload
					Counter = 0;
					Projectile.timeLeft++;
				}
			}
		}

		public override float GetRotation(out float armRotation, out Player.CompositeArmStretchAmount stretch)
		{
			float value = base.GetRotation(out armRotation, out stretch);
			Player owner = Main.player[Projectile.owner];

			if (owner.TryGetModPlayer(out ReloadPlayer reloadPlayer) && reloadPlayer.Reloading)
				value += MathHelper.PiOver4 * Projectile.direction;

			return value;
		}

		public override bool PreDraw(ref Color lightColor)
		{
			Player owner = Main.player[Projectile.owner];
			Texture2D texture = TextureAssets.Projectile[Type].Value;
			SpriteEffects effects = (Projectile.direction == -1) ? SpriteEffects.FlipVertically : SpriteEffects.None;

			float holdDistance = 8 + 20 * (1f - Math.Min(Progress * 3, 1));
			bool reloading = false;

			if (owner.TryGetModPlayer(out ReloadPlayer reloadPlayer) && reloadPlayer.Reloading)
			{
				holdDistance = 12;
				reloading = true;
			}

			Vector2 origin = new(holdDistance, texture.Height / 2);
			DrawHeld(lightColor, origin, Projectile.rotation, effects);

			if (reloading && Progress > 0.5f)
				DrawBolt(lightColor);

			return false;
		}

		public void DrawBolt(Color lightColor)
		{
			float progress = Math.Min((Progress - 0.5f) / 0.5f * 1.5f, 1);

			Texture2D texture = TextureAssets.Item[ModContent.ItemType<Bolt>()].Value;
			SpriteEffects effects = (Projectile.direction == -1) ? SpriteEffects.FlipVertically : SpriteEffects.None;
			Vector2 position = Projectile.Center - Main.screenPosition + new Vector2(10 * progress, -4 * Projectile.direction + Main.player[Projectile.owner].gfxOffY).RotatedBy(Projectile.rotation);

			Main.EntitySpriteDraw(texture, position, null, Projectile.GetAlpha(lightColor), Projectile.rotation + MathHelper.PiOver2 * Projectile.direction, texture.Size() / 2, Projectile.scale, effects);
		}

		public override bool? CanDamage() => false;
	}

	public override void SetDefaults()
    {
		Item.DefaultToBow(30, 10, true);
		Item.useAmmo = ModContent.ItemType<Bolt>();
		Item.damage = 10;
		Item.knockBack = 4.5f;
		Item.noUseGraphic = true;
		Item.rare = ItemRarityID.Blue;
	}

	public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
		SwungProjectile.Spawn(position, velocity, ModContent.ProjectileType<CrossbowHeld>(), damage, knockback, player, 0, source);
		return true;
    }
}