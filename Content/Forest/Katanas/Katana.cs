using SpiritReforged.Common;
using SpiritReforged.Common.Easing;
using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.ModCompat;
using SpiritReforged.Common.ModCompat.Replacement;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.PlayerCommon;
using SpiritReforged.Common.ProjectileCommon.Abstract;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Granite.Armor;
using SpiritReforged.Content.Jungle.Bamboo.Items;
using SpiritReforged.Content.Particles;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using static System.Net.Mime.MediaTypeNames;

namespace SpiritReforged.Content.Forest.Katanas;

[ReplaceContent(ItemID.Katana)]
public class Katana : ModItem, IDrawHeld
{
	public sealed class KatanaSwing : SwungProjectile, IDrawPixelated
	{
		public bool Secondary { get => Projectile.ai[0] == 1; set => Projectile.ai[0] = value ? 1 : 0; }

		public override string Texture => ModContent.GetInstance<Katana>().Texture;

		public override LocalizedText DisplayName => ModContent.GetInstance<Katana>().DisplayName;

		public override float SwingTime => Secondary ? base.SwingTime * 2 : base.SwingTime;

		private bool _falling = true;

		public override IConfiguration SetConfiguration() => new BasicConfiguration(EaseFunction.EaseQuarticOut, 72, 25);

		public override float GetRotation(out float armRotation, out Player.CompositeArmStretchAmount stretch) => base.GetRotation(out armRotation, out stretch) + MathHelper.PiOver4 * SwingDirection;

		public override void AI()
		{
			base.AI();
			if (Secondary) //Leap
			{
				Player owner = Main.player[Projectile.owner];
				if (Counter == 1) //Just started the swing
				{
					owner.velocity += Projectile.velocity * 8;
					owner.velocity.Y -= 6;
				}

				DashSwordPlayer mp = owner.GetModPlayer<DashSwordPlayer>();
				mp.SetDash(30);

				if (_falling)
				{
					if (owner.velocity.Y == 0) //Impact
					{
						if (!Main.dedServ)
						{
							if (Projectile.owner == Main.myPlayer)
								ScreenshakeHelper.Shake(Projectile.Center, Vector2.UnitY, 6f, 3, 20);

							ParticleHandler.SpawnParticle(new TexturedPulseCircle(owner.Bottom + new Vector2(0, 5), Color.Silver.Additive() * 0.3f, 0.2f, 220, 10, "Scorch", new Vector2(2), EaseFunction.EaseCircularOut).WithSkew(0.5f, MathHelper.PiOver2));

							for (int i = 0; i < 20; i++)
							{
								float reach = 50;
								float speed = Main.rand.NextFloat(3);

								Dust.NewDustPerfect(owner.Bottom + new Vector2(reach * Main.rand.NextFloat(-1f, 1f), 0), DustID.Smoke, Vector2.UnitY * -speed, (int)MathHelper.Lerp(10, 200, speed / 3f), Scale: Main.rand.NextFloat(0.5f, 2f)).noGravity = true;
							}

							SoundEngine.PlaySound(SoundID.Run with { Pitch = 0.5f }, Projectile.Center);
						}

						_falling = false;
					}

					owner.noKnockback = true;
					owner.noFallDmg = true;
					owner.maxFallSpeed = 20f;

					if (Counter >= SwingTime - 3)
					{
						Counter--; //Pause

						owner.velocity.X *= 0.98f;
						owner.velocity.Y += 0.5f * owner.gravDir;
					}
				}
			}
		}

		public override bool PreDraw(ref Color lightColor)
		{
			SpriteEffects effects = (SwingDirection == -1) ? SpriteEffects.FlipVertically : default;
			DrawHeld(lightColor, new Vector2(0, (effects == SpriteEffects.FlipVertically) ? 0 : TextureAssets.Projectile[Type].Value.Height), Projectile.rotation, effects);
			return false;
		}

		void IDrawPixelated.DrawPixelated(SpriteBatch spriteBatch)
		{
			if (SwingArc != 0)
			{
				Player owner = Main.player[Projectile.owner];

				//Draw a custom smear
				Main.instance.LoadProjectile(985);
				Texture2D smear = TextureAssets.Projectile[985].Value;

				SpriteEffects effects = (SwingDirection == -1) ? SpriteEffects.FlipVertically : default;
				Rectangle source = smear.Frame(1, 4, 0, (int)(Progress * 14f));
				float rotation = Projectile.rotation + SwingDirection * Progress - MathHelper.PiOver4 * SwingDirection;

				Color lightColor = Lighting.GetColor(Projectile.Center.ToTileCoordinates());
				Vector2 origin = new(source.Width, source.Height / 2);
				Vector2 smearWorldPosition = owner.Center + (Vector2.UnitX * (GetConfig<BasicConfiguration>().Reach + 10)).RotatedBy(rotation);
				Vector2 smearDrawPosition = smearWorldPosition - Main.screenPosition;

				IDrawPixelated.PixelateDrawPosition(ref smearDrawPosition);

				spriteBatch.Draw(smear, smearDrawPosition, source, Projectile.GetAlpha(lightColor.MultiplyRGB(new Color(104, 83, 64))), rotation, origin, 0.45f, effects, 0);
				spriteBatch.Draw(smear, smearDrawPosition, source, Projectile.GetAlpha(lightColor.MultiplyRGB(new Color(187, 192, 173))), rotation, origin, 0.4f, effects, 0);
				spriteBatch.Draw(smear, smearDrawPosition, source, Projectile.GetAlpha(lightColor.MultiplyRGB(new Color(255, 253, 200))), rotation, origin, 0.2f, effects, 0);
			}
		}
	}

	public override string Texture => "Terraria/Images/Item_" + ItemID.Katana;

	public static readonly SoundStyle Swing = new("SpiritReforged/Assets/SFX/Item/KatanaSwing", 3);

	public static readonly Asset<Texture2D> HeldTexture = DrawHelpers.RequestLocal<Katana>("Katana_Held", false);
	private float _swingArc;

	public override void SetStaticDefaults()
	{
		SpiritSets.IsSword[Type] = SpiritSets.IsKatana[Type] = true;
		ItemID.Sets.ShimmerTransformToItem[Type] = ItemID.Katana;
	}

	public override void SetDefaults()
	{
		Item.CloneDefaults(ItemID.Katana);
		Item.DefaultToSpear(ModContent.ProjectileType<KatanaSwing>(), 1, Item.useAnimation);
		Item.UseSound = Swing;
		MoRHelper.SetSlashBonus(Item);
	}

	public override void HoldItem(Player player)
	{
		if (!player.ItemAnimationActive)
			player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Quarter, -0.35f * player.direction);
	}

	public override bool AltFunctionUse(Player player) => player.GetModPlayer<DashSwordPlayer>().HasDashCharge;

	public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
	{
		_swingArc = _swingArc switch
		{
			3f => 5f,
			5f => -5f,
			_ => 3f
		};

		float arc = (player.altFunctionUse == 2) ? 8 : _swingArc;
		SwungProjectile.Spawn(position, velocity, type, damage, knockback, player, arc, source, player.altFunctionUse - 1);
		return false;
	}

	void IDrawHeld.DrawHeld(ref PlayerDrawSet drawinfo)
	{
		if (!drawinfo.drawPlayer.ItemAnimationActive)
			IDrawHeld.DrawSwordHeld(ref drawinfo, HeldTexture.Value);
	}
}