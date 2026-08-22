using SpiritReforged.Common.Easing;
using SpiritReforged.Common.PlayerCommon;
using SpiritReforged.Common.ProjectileCommon.Abstract;
using SpiritReforged.Common.Subclasses.Greatshields;
using SpiritReforged.Common.Visuals;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Forest.Shields;

public class Roromaraugi : GreatshieldItem
{
	public class RoromaraugiSwing : SwungProjectile
	{
		public override string Texture => DrawHelpers.RequestLocal<Roromaraugi>("Roromaraugi_Held");

		public int ChargeTimeMax
		{
			get
			{
				Player owner = Main.player[Projectile.owner];
				return _chargeTimeMax = (_chargeTimeMax == 0) ? _chargeTimeMax = (int)(SwingTime * 1.5f * owner.GetTotalAttackSpeed(DamageClass.Melee)) : _chargeTimeMax;
			}
			set => _chargeTimeMax = value;
		}

		public bool FullyCharged => _chargeTime >= ChargeTimeMax;

		private int _chargeTimeMax;
		private int _chargeTime;
		private bool _released;

		public override IConfiguration SetConfiguration() => new BasicConfiguration(EaseFunction.EaseCubicOut, 54, 25);

		public override float GetRotation(out float armRotation, out Player.CompositeArmStretchAmount stretch)
		{
			float value = base.GetRotation(out armRotation, out stretch);
			//armRotation += MathHelper.PiOver2;

			return value + MathHelper.PiOver2 + Progress * SwingDirection;
		}

		public override void AI()
		{
			base.AI();
			Player owner = Main.player[Projectile.owner];

			if (owner.channel && !_released)
			{
				_chargeTime++;
				Projectile.velocity = owner.DirectionTo(PlayerMouseHandler.GetMouse(Projectile.owner));
			}
			else 
			{
				if (FullyCharged)
				{

				}

				_released = true;
			}

			//owner.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - MathHelper.PiOver2);
		}

		public override bool PreDraw(ref Color lightColor)
		{
			Texture2D texture = TextureAssets.Projectile[Type].Value;
			SpriteEffects effects = (SwingDirection == -1) ? SpriteEffects.FlipHorizontally : default;
			Vector2 origin = new(texture.Width / 2, texture.Height - 6); //The handle

			Color smearColor = Projectile.GetAlpha(lightColor).MultiplyRGB(Color.PaleVioletRed) * Math.Min(Counter / SwingTime * 3, 1) * 0.5f;
			float smearRotation = Projectile.rotation - SwingArc * 0.5f * Projectile.spriteDirection + ((Projectile.spriteDirection == -1) ? MathHelper.Pi : 0);

			DrawHeld(lightColor, origin, Projectile.rotation, effects);
			DrawSmear(smearColor, smearRotation, (SwingDirection == -1) ? SpriteEffects.FlipVertically : default);

			return false;
		}
	}

	public override DrawLayer Layer => DrawLayer.BackArm;

	public override ShieldInfo SetInfo()
	{
		Item.damage = 12;
		Item.useTime = Item.useAnimation = 40;
		Item.knockBack = 12;
		Item.shoot = ModContent.ProjectileType<RoromaraugiSwing>();

		return new ShieldInfo(30, 60);
	}

	public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
	{
		SwungProjectile.Spawn(position, velocity, type, damage, knockback, player, 3, source);
		return false;
	}

	public override void OnBlockDamage(Player player, Player.HurtInfo info) { }

	public override void DrawShield(ref PlayerDrawSet drawInfo, bool guarding)
	{
		if (drawInfo.drawPlayer.ownedProjectileCounts[ModContent.ProjectileType<RoromaraugiSwing>()] == 0) //Don't draw while performing a swing
		{
			const int idle_frame = 0;
			const int jump_frame = 5;

			Player player = drawInfo.drawPlayer;
			Texture2D texture = HeldTexture;
			Color color = Lighting.GetColor(player.Center.ToTileCoordinates());
			SpriteEffects effects = drawInfo.playerEffect;
			Vector2 origin = new(texture.Width / 2, texture.Height - 30);

			Vector2 offhand = GetOffhand(player, out int frame) + new Vector2(9 * player.direction, 3);
			Vector2 halfSize = player.Size / 2;

			float rotation = drawInfo.rotation;
			if (guarding)
			{
				rotation = player.AngleTo(PlayerMouseHandler.GetMouse(player.whoAmI)) + (player.direction == -1 ? MathHelper.Pi : 0);
				player.bodyFrame.Y = 0;
			}
			else if (frame is jump_frame or idle_frame)
			{
				rotation -= 0.3f * player.direction;
			}

			Vector2 position = drawInfo.Position + halfSize + (offhand - halfSize).RotatedBy(rotation) - Main.screenPosition;
			if (guarding)
			{
				float scale = 1f + EaseFunction.EaseSine.Ease((float)Main.timeForVisualEffects / 30f) * 0.3f;
				drawInfo.DrawDataCache.Add(new(texture, position.Floor(), null, color * (1f - (scale - 1f) / 0.4f), rotation, origin, scale, effects, 0));
			}

			drawInfo.DrawDataCache.Add(new(texture, position.Floor(), null, color, rotation, origin, 1, effects, 0));
		}
	}
}