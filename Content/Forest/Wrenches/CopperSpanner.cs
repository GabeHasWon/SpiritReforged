using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.ProjectileCommon;
using SpiritReforged.Common.ProjectileCommon.Abstract;
using SpiritReforged.Common.Subclasses.Wrenches;
using SpiritReforged.Common.Visuals;
using System.IO;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Forest.Wrenches;

public class CopperSpanner : ModItem
{
	private class SpeedUpProjectile : GlobalProjectile, IWrenchGlobal
	{
		public override bool InstancePerEntity => true;

		public int Duration { get; set; }

		public override void AI(Projectile projectile)
		{
			if (Duration > 0)
			{
				Duration--;
				projectile.GetGlobalProjectile<SpeedModifierProjectile>().SpeedModifier += 0.2f;

				IWrenchGlobal.ClientPassiveEffects(projectile, 0.5f);
			}
		}

		public override void PostDraw(Projectile projectile, Color lightColor)
		{
			if (Duration > 0)
				IWrenchGlobal.DrawDurationBar(projectile, Duration / (5 * 60f));
		}
	}

	public class CopperSpannerSwing : SwungProjectile, IDrawPixelated, IHitSentry
	{
		public override LocalizedText DisplayName => ModContent.GetInstance<CopperSpanner>().DisplayName;

		public override string Texture => ModContent.GetInstance<CopperSpanner>().Texture;

		protected bool _recoiling;

		public override IConfiguration SetConfiguration() => new BasicConfiguration(EaseFunction.EaseCubicOut, 40, 25);

		public override float GetRotation(out float armRotation, out Player.CompositeArmStretchAmount stretch)
		{
			float value = base.GetRotation(out armRotation, out stretch);
			return value + (MathHelper.PiOver4 - Progress) * SwingDirection;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => IHitSentry.DropScrap(Main.player[Projectile.owner], target);

		public override bool? CanDamage() => _recoiling ? false : base.CanDamage(); //Never deal damage while recoiling

		void IHitSentry.OnHitSentry(Player player, Projectile sentry, ref int cooldown)
		{
			IHitSentry.ClientHitEffects(sentry);

			player.GetModPlayer<WrenchPlayer>().StoredScrap--;
			sentry.GetGlobalProjectile<SpeedUpProjectile>().Duration = 5 * 60;

			SetRecoil();
		}

		public void SetRecoil()
		{
			_recoiling = true; //Sync me

			if (Main.myPlayer == Projectile.owner)
			{
				Projectile.velocity = Main.player[Projectile.owner].DirectionTo(Main.MouseWorld).RotatedBy(-MathHelper.PiOver4 * SwingDirection);
				Projectile.netUpdate = true;
			}

			SwingArc = -1;
			Counter = 0;
		}

		public override bool PreDraw(ref Color lightColor)
		{
			Texture2D texture = TextureAssets.Projectile[Type].Value;
			SpriteEffects effects = (SwingDirection == -1) ? SpriteEffects.FlipVertically : default;
			Vector2 origin = new(4, (effects == SpriteEffects.FlipVertically) ? 6 : texture.Height - 6); //The handle

			DrawHeld(lightColor, origin, Projectile.rotation, effects);
			return false;
		}

		void IDrawPixelated.DrawPixelated(SpriteBatch spriteBatch)
		{
			if (!_recoiling)
				DrawPixelatedSmear(spriteBatch, new Color(183, 88, 35));
		}

		public void DrawPixelatedSmear(SpriteBatch spriteBatch, Color color)
		{
			Player owner = Main.player[Projectile.owner];

			//Draw a custom smear
			Main.instance.LoadProjectile(985);
			Texture2D smear = TextureAssets.Projectile[985].Value;

			SpriteEffects effects = (SwingDirection == -1) ? SpriteEffects.FlipVertically : default;
			Rectangle source = smear.Frame(1, 4, 0, (int)(Progress * 14f));
			float rotation = Projectile.rotation - MathHelper.PiOver2 * SwingDirection + SwingDirection * Progress;

			Color lightColor = Lighting.GetColor(Projectile.Center.ToTileCoordinates());
			Vector2 origin = new(source.Width, source.Height / 2);
			Vector2 smearWorldPosition = owner.Center + (Vector2.UnitX * (GetConfig<BasicConfiguration>().Reach + 20)).RotatedBy(rotation);
			Vector2 smearDrawPosition = smearWorldPosition - Main.screenPosition;

			IDrawPixelated.PixelateDrawPosition(ref smearDrawPosition);

			spriteBatch.Draw(smear, smearDrawPosition, source, Projectile.GetAlpha(lightColor.MultiplyRGB(color)), rotation, origin, 0.25f, effects, 0);
			spriteBatch.Draw(smear, smearDrawPosition, source, Projectile.GetAlpha(lightColor.MultiplyRGB(color)).Additive(100), rotation, origin, 0.2f, effects, 0);
		}

		public override void SendExtraAI(BinaryWriter writer)
		{
			base.SendExtraAI(writer);
			writer.Write(_recoiling);
		}

		public override void ReceiveExtraAI(BinaryReader reader)
		{
			base.ReceiveExtraAI(reader);
			_recoiling = reader.ReadBoolean();
		}
	}

	public override void SetDefaults()
	{
		Item.Size = new(38, 40);
		Item.damage = 12;
		Item.useTime = Item.useAnimation = 22;
		Item.useStyle = ItemUseStyleID.Swing;
		Item.noUseGraphic = true;
		Item.noMelee = true;
		Item.DamageType = ModContent.GetInstance<WrenchClass>();
		Item.knockBack = 4;
		Item.useTurn = true;
		Item.rare = ItemRarityID.Blue;
		Item.shootSpeed = 1;
		Item.shoot = ModContent.ProjectileType<CopperSpannerSwing>();
	}

	public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
	{
		SwungProjectile.Spawn(position, velocity, type, damage, knockback, player, 3, source);
		return false;
	}

	public override void AddRecipes() => CreateRecipe().AddIngredient(ItemID.CopperBar, 12).AddTile(TileID.Anvils).Register();
}