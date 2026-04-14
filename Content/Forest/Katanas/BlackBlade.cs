using SpiritReforged.Common;
using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.ModCompat;
using SpiritReforged.Common.PlayerCommon;
using SpiritReforged.Common.ProjectileCommon.Abstract;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Forest.Katanas;

public class BlackBlade : ModItem, IDrawHeld
{
	public sealed class BlackBladeSwing : SwungProjectile
	{
		public override LocalizedText DisplayName => ModContent.GetInstance<BlackBlade>().DisplayName;

		public override void SetStaticDefaults() => Main.projFrames[Type] = 3;

		public override IConfiguration SetConfiguration() => new BasicConfiguration(Common.Easing.EaseFunction.EaseCubicOut, 68, 25);

		public override float GetRotation(out float armRotation, out Player.CompositeArmStretchAmount stretch)
		{
			float value = base.GetRotation(out armRotation, out stretch);

			if (Progress < 0.8f && Main.rand.NextBool())
			{
				float intensity = Main.rand.NextFloat();
				var position = Vector2.Lerp(Projectile.Center, GetEndPosition(), intensity);
				Dust.NewDustPerfect(position, Main.rand.NextFromList(DustID.Smoke, DustID.Ash), Vector2.UnitX.RotatedBy(value + MathHelper.PiOver2 * SwingDirection), 180, default, intensity * 1.5f).noGravity = true;
			}

			return value;
		}

		public override bool PreDraw(ref Color lightColor)
		{
			SpriteEffects effects = (SwingDirection == -1) ? SpriteEffects.FlipVertically : default;
			Vector2 origin = new(4, 30); //The handle

			DrawHeld(lightColor, origin, Projectile.rotation, effects);
			return false;
		}
	}

	private float _swingArc;

	public override void SetStaticDefaults() => SpiritSets.IsSword[Type] = true;

	public override void SetDefaults()
	{
		Item.damage = 12;
		Item.crit = 2;
		Item.knockBack = 3;
		Item.useTime = Item.useAnimation = 20;
		Item.DamageType = DamageClass.Melee;
		Item.width = Item.height = 46;
		Item.useStyle = ItemUseStyleID.Swing;
		Item.value = Item.sellPrice(silver: 3);
		Item.rare = ItemRarityID.White;
		Item.UseSound = SoundID.Item1;
		Item.shoot = ModContent.ProjectileType<BlackBladeSwing>();
		Item.shootSpeed = 1f;
		Item.autoReuse = true;
		Item.useTurn = true;
		Item.noUseGraphic = true;
		Item.noMelee = true;
		MoRHelper.SetSlashBonus(Item);
	}

	public override void HoldItem(Player player) { }

	public override bool AltFunctionUse(Player player) => player.GetModPlayer<DashSwordPlayer>().HasDashCharge;

	public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
	{
		_swingArc = _swingArc switch
		{
			3f => -4f,
			-4f => 5f,
			_ => 3f
		};

		SwungProjectile.Spawn(position, velocity, type, damage, knockback, player, _swingArc);
		return false;
	}

	public void DrawHeld(ref PlayerDrawSet info) { }

	public override void AddRecipes() { }
}