using SpiritReforged.Common;
using SpiritReforged.Common.Easing;
using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.ModCompat;
using SpiritReforged.Common.PlayerCommon;
using SpiritReforged.Common.ProjectileCommon.Abstract;
using SpiritReforged.Common.Visuals;
using Terraria.Audio;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Forest.Katanas;

public class Onimaru : ModItem, IDrawHeld
{
	public sealed class OnimaruSwing : SwungProjectile, IDrawPixelated
	{
		public bool Secondary { get => Projectile.ai[0] == 1; set => Projectile.ai[0] = value ? 1 : 0; }

		public override LocalizedText DisplayName => ModContent.GetInstance<Onimaru>().DisplayName;

		private bool _returningDash;

		public override IConfiguration SetConfiguration() => new BasicConfiguration(EaseFunction.EaseQuarticOut, 60, 25);

		public override void AI()
		{
			const int magnitude = 30;

			if (Secondary)
			{
				Player owner = Main.player[Projectile.owner];

				DashSwordPlayer mp = owner.GetModPlayer<DashSwordPlayer>();
				mp.SetDash(40);

				if (_returningDash)
				{
					if (Counter == SwingTime / 2)
						SoundEngine.PlaySound(SoundID.DD2_WyvernDiveDown with { Pitch = 1 }, Projectile.Center);

					owner.armorEffectDrawShadow = true;
					base.AI();
				}
				else
				{
					Projectile.Center = owner.Center;
					Projectile.Opacity = 0;
					owner.heldProj = Projectile.whoAmI;

					if (++Counter < SwingTime - 2)
						owner.itemAnimation = owner.itemTime = Projectile.timeLeft = 2;

					if (Counter >= SwingTime - 3) //Begin a returning dash
					{
						Counter = 0;
						SwingArc = -SwingArc;

						Projectile.Opacity = 1;
						Projectile.ResetLocalNPCHitImmunity();

						_returningDash = true;
					}
				}

				if (Counter > SwingTime - 10)
				{
					owner.velocity *= 0.5f;
				}
				else
				{
					Vector2 dashVelocity = Projectile.velocity * (_returningDash ? -magnitude : magnitude);
					owner.velocity = Vector2.Lerp(owner.velocity, dashVelocity, EaseFunction.EaseQuinticIn.Ease(Math.Min(Progress * 2, 1)));
				}
			}
			else
			{
				base.AI();
			}
		}

		public override bool? CanDamage() => (Secondary && !_returningDash) ? false : base.CanDamage(); //Don't damage enemies during the initial dash

		public override bool PreDraw(ref Color lightColor)
		{
			SpriteEffects effects = (SwingDirection == -1) ? SpriteEffects.FlipVertically : default;
			Vector2 origin = new(4, 10); //The handle
			Rectangle source = TextureAssets.Projectile[Type].Frame();

			DrawHeld(Projectile.GetAlpha(lightColor), origin, Projectile.rotation, effects, source);
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
				float rotation = Projectile.rotation + SwingDirection * Progress;

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

	public static readonly Asset<Texture2D> HeldTexture = DrawHelpers.RequestLocal<Onimaru>("Onimaru_Held", false);
	private float _swingArc;

	public override void SetStaticDefaults() => SpiritSets.IsSword[Type] = SpiritSets.IsKatana[Type] = true;

	public override void SetDefaults()
	{
		Item.DefaultToSpear(ModContent.ProjectileType<OnimaruSwing>(), 1, 22);
		Item.SetShopValues(ItemRarityColor.Green2, Item.sellPrice(silver: 30));
		Item.damage = 12;
		Item.knockBack = 3;
		Item.autoReuse = true;
		MoRHelper.SetSlashBonus(Item);
	}

	public override void HoldItem(Player player)
	{
		if (!player.ItemAnimationActive)
			player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Quarter, -0.35f * player.direction);
	}

	public override bool AltFunctionUse(Player player) => true;

	public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
	{
		_swingArc = _swingArc switch
		{
			3f => 5f,
			5f => -5f,
			_ => 3f
		};

		SwungProjectile.Spawn(position, velocity, type, damage, knockback, player, _swingArc, source, player.altFunctionUse - 1);
		return false;
	}

	public override void AddRecipes() { }

	void IDrawHeld.DrawHeld(ref PlayerDrawSet drawinfo)
	{
		if (!drawinfo.drawPlayer.ItemAnimationActive)
			IDrawHeld.DrawSwordHeld(ref drawinfo, HeldTexture.Value);
	}
}