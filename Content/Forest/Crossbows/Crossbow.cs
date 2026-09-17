using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.ProjectileCommon.Abstract;
using SpiritReforged.Common.Visuals;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.Graphics;

namespace SpiritReforged.Content.Forest.Crossbows;

public class ReloadPlayer : ModPlayer
{
	public interface IPerfectReload
	{
		public bool PerfectReload { get; set;  }
	}

	public static readonly Asset<Texture2D> Meter = DrawHelpers.RequestLocal<Crossbow>("ReloadMeter", false);

	public bool Reloading => _reloadTime > 0;

	public int PerfectRange => (int)_perfectRange.ApplyTo(10);

	public int FullReloadTime { get; private set; }

	private StatModifier _perfectRange = StatModifier.Default;
	private int _reloadTime, _perfectReloadSpot;
	private float _visualCounter;

	public void StartReload(int duration)
	{
		_reloadTime = FullReloadTime = duration;
		_perfectReloadSpot = duration / 2;
	}

	public override void DrawPlayer(Camera camera)
	{
		if (Main.myPlayer != Player.whoAmI)
			return; //Local drawing only

		SpriteBatch spriteBatch = Main.spriteBatch;
		Texture2D pixel = TextureAssets.MagicPixel.Value;
		Rectangle pixelSource = new(0, 0, 2, 2);
		var position = (Main.LocalPlayer.Bottom + new Vector2(0, 10 + Main.LocalPlayer.gfxOffY) - Main.screenPosition).ToPoint().ToVector2();

		if (Main.LocalPlayer.TryGetModPlayer(out ReloadPlayer reloadPlayer) && reloadPlayer.Reloading) //Draw the full reload meter
		{
			Texture2D texture = Meter.Value;
			Rectangle backSource = texture.Frame(1, 2, 0, 0, 0, -2);
			float progress = 1f - (float)reloadPlayer._reloadTime / reloadPlayer.FullReloadTime;

			spriteBatch.Draw(texture, position, backSource, Color.White, 0, backSource.Size() / 2, 1, 0, 0); //Draw meter background

			DrawPerfectReload(spriteBatch, position, backSource, reloadPlayer);

			float halfProgress = backSource.Width * progress / 2;
			Rectangle fillSource = texture.Frame(1, 2, 0, 1, 0, -2) with { Width = (int)halfProgress };

			spriteBatch.Draw(texture, position, fillSource, Color.White, 0, new Vector2(fillSource.Width, fillSource.Height / 2), 1, 0, 0); //Draw meter fill left
			spriteBatch.Draw(texture, position, fillSource, Color.White, 0, new Vector2(0, fillSource.Height / 2), 1, SpriteEffects.FlipHorizontally, 0); //Draw meter fill right

			spriteBatch.Draw(pixel, position - new Vector2(fillSource.Width - 1, 0), pixelSource, Color.White * 0.7f, 0, pixelSource.Size() / 2, 1, 0, 0); //Draw left end
			spriteBatch.Draw(pixel, position + new Vector2(fillSource.Width - 1, 0), pixelSource, Color.White * 0.7f, 0, pixelSource.Size() / 2, 1, 0, 0); //Draw right end
		}

		if (Main.LocalPlayer.HeldItem.ModItem is IPerfectReload perfect && perfect.PerfectReload) //Draw a perfect reload indicator
		{
			position -= Main.LocalPlayer.velocity;

			Texture2D texture = AssetLoader.LoadedTextures["Star"].Value;
			Vector2 scale = new Vector2(1f + _visualCounter * 3, 1) * (1f + EaseFunction.EaseSine.Ease((float)Main.timeForVisualEffects / 50f) * 0.2f);

			spriteBatch.Draw(texture, position, null, Color.Goldenrod.Additive() * 0.8f, 0, texture.Size() / 2, scale * 0.12f, 0, 0);
			spriteBatch.Draw(texture, position, null, Color.White.Additive(), 0, texture.Size() / 2, scale * 0.1f, 0, 0);

			DrawHelpers.DrawOutline(default, default, default, default, (offset) =>
				spriteBatch.Draw(pixel, position + offset, pixelSource, Color.PaleVioletRed, 0, pixelSource.Size() / 2, 1, 0, 0));

			spriteBatch.Draw(pixel, position, pixelSource, Color.Yellow, 0, pixelSource.Size() / 2, 1, 0, 0); //Draw left end
		}
	}

	private static void DrawPerfectReload(SpriteBatch spriteBatch, Vector2 origin, Rectangle barRegion, ReloadPlayer modPlayer)
	{
		int width = barRegion.Width / 2;
		float progress = 1f - (float)modPlayer._perfectReloadSpot / modPlayer.FullReloadTime;

		Texture2D pixel = TextureAssets.MagicPixel.Value;
		Rectangle source = new(0, 0, 1, 2);
		Vector2 stretch = new(width * ((float)modPlayer.PerfectRange / modPlayer.FullReloadTime), 1);
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
		if (Reloading && Main.mouseLeft && Main.mouseLeftRelease)
		{
			Rectangle perfectRectangle = new(_perfectReloadSpot - PerfectRange / 2, 0, PerfectRange, 0);
			if (_reloadTime > perfectRectangle.Left && _reloadTime < perfectRectangle.Right)
			{
				_reloadTime = 0; //Perform a perfect reload
				_visualCounter = 1f;

				SoundEngine.PlaySound(SoundID.MaxMana, Player.Center);

				if (Player.HeldItem.ModItem is IPerfectReload perfect)
					perfect.PerfectReload = true;
			}
		}

		_reloadTime = Math.Max(_reloadTime - 1, 0);
		_visualCounter = Math.Max(_visualCounter - 0.1f, 0);
	}
}

public class Crossbow : ModItem, ReloadPlayer.IPerfectReload
{
	public class CrossbowHeld : SwungProjectile
	{
		public override float SwingTime
		{
			get
			{
				Player owner = Main.player[Projectile.owner];
				return (owner.TryGetModPlayer(out ReloadPlayer reloadPlayer) && (ReloadAnimation || reloadPlayer.Reloading)) ? reloadPlayer.FullReloadTime : base.SwingTime;
			}
		}

		public bool ReloadAnimation
		{
			get => Projectile.ai[0] == 1;
			set => Projectile.ai[0] = value ? 1 : 0;
		}

		public override LocalizedText DisplayName => ModContent.GetInstance<Crossbow>().DisplayName;

		public override string Texture => ModContent.GetInstance<Crossbow>().Texture;

		public bool GetReloadStatus(out ReloadPlayer status)
		{
			Player owner = Main.player[Projectile.owner];
			if (owner.TryGetModPlayer(out ReloadPlayer reloadPlayer))
			{
				status = reloadPlayer;
				return true;
			}

			status = null;
			return false;
		}

		public override IConfiguration SetConfiguration() => new CrossbowConfiguration(null, 30, 10, 90);

		public override void AI()
		{
			base.AI();

			if (!GetReloadStatus(out var status))
				return;

			if (ReloadAnimation)
			{
				if (Counter == (int)SwingTime / 2)
					SoundEngine.PlaySound(SoundID.Unlock with { Pitch = 0.3f }, Projectile.Center);

				if (!status.Reloading) //Reloading has ended sooner than expected (perfect reload)
					Counter = Math.Max(Counter, (int)SwingTime - 20);
			}
			else
			{
				if (Counter == 1)
				{
					for (int i = 0; i < 8; i++)
						Dust.NewDustPerfect(GetEndPosition(-8), Main.rand.NextFromList(DustID.GoldCoin, DustID.Smoke), (Vector2.Normalize(Projectile.velocity) * Main.rand.NextFloat(3f)).RotatedByRandom(1f)).noGravity = true;
				}

				if (!status.Reloading && Counter == SwingTime - 3)
				{
					status.StartReload(GetConfig<CrossbowConfiguration>().ReloadTime); //Start reload

					ReloadAnimation = true;
					Counter = 0;
					Projectile.timeLeft++; //Ensure the projectile doesn't time out
				}
			}
		}

		public override float GetRotation(out float armRotation, out Player.CompositeArmStretchAmount stretch)
		{
			float value = base.GetRotation(out armRotation, out stretch);
			if (ReloadAnimation)
			{
				//stretch = Player.CompositeArmStretchAmount.None;
				value += 0.4f * Projectile.direction * Math.Min(Progress * 3, 1);
			}

			return value;
		}

		public override bool PreDraw(ref Color lightColor)
		{
			Texture2D texture = TextureAssets.Projectile[Type].Value;
			SpriteEffects effects = (Projectile.direction == -1) ? SpriteEffects.FlipVertically : SpriteEffects.None;

			float reverseHoldDistance = 8 + 20 * (1f - Math.Min(Progress * 3, 1));

			if (ReloadAnimation)
				reverseHoldDistance = 16;

			Vector2 origin = new(reverseHoldDistance, texture.Height / 2);
			DrawHeld(lightColor, origin, Projectile.rotation, effects);

			if (ReloadAnimation)
				DrawBolt(lightColor);

			return false;
		}

		public virtual void DrawBolt(Color lightColor)
		{
			float progress = Math.Min((Progress - 0.5f) / 0.5f, 1);
			if (progress > 0)
			{
				Player owner = Main.player[Projectile.owner];
				owner.PickAmmo(owner.HeldItem, out int shotType, out _, out _, out _, out _, true);

				Texture2D texture = TextureAssets.Projectile[shotType].Value;
				SpriteEffects effects = (Projectile.direction == -1) ? SpriteEffects.FlipVertically : SpriteEffects.None;
				Vector2 position = Projectile.Center - Main.screenPosition + new Vector2(10 * EaseFunction.EaseQuinticOut.Ease(progress), -4 * Projectile.direction + Main.player[Projectile.owner].gfxOffY).RotatedBy(Projectile.rotation);

				Main.EntitySpriteDraw(texture, position, null, Projectile.GetAlpha(lightColor), Projectile.rotation + MathHelper.PiOver2 * Projectile.direction, texture.Size() / 2, Projectile.scale, effects);
			}
		}

		public override bool? CanDamage() => false;
	}

	public readonly record struct CrossbowConfiguration(EaseFunction Easing, int Reach, int Width, int ReloadTime) : SwungProjectile.IConfiguration;

	public bool PerfectReload { get; set; }

	public override void SetDefaults()
    {
		Item.DefaultToBow(50, 10, true);
		Item.UseSound = SoundID.DD2_BallistaTowerShot with { Pitch = 0.5f };
		Item.useAmmo = ModContent.ItemType<Bolt>();
		Item.damage = 10;
		Item.knockBack = 4.5f;
		Item.noUseGraphic = true;
		Item.rare = ItemRarityID.Blue;
	}

	public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
	{
		if (PerfectReload)
			damage *= 2;
	}

	public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
		SwungProjectile.Spawn(position, velocity, ModContent.ProjectileType<CrossbowHeld>(), damage, knockback, player, 0, source);
		PerfectReload = false;

		return true;
    }
}