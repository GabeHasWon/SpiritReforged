using SpiritReforged.Common.Easing;
using SpiritReforged.Common.ProjectileCommon.Abstract;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Desert;

public class WindshearScepter : ModItem
{
	public class WindshearScepterSwing : SwungProjectile
	{
		public override string Texture => ModContent.GetInstance<WindshearScepter>().Texture;
		public override LocalizedText DisplayName => ModContent.GetInstance<WindshearScepter>().DisplayName;

		public override Configuration SetConfiguration() => new(EaseFunction.EaseCubicOut, 58, 30);

		public override float GetRotation(out float armRotation)
		{
			int direction = Projectile.spriteDirection * Math.Sign(SwingArc);
			float value = base.GetRotation(out armRotation) + direction * Progress * 2;

			return value + MathHelper.PiOver4;
		}

		public override bool PreDraw(ref Color lightColor)
		{
			DrawHeld(lightColor, new Vector2(2, TextureAssets.Projectile[Type].Value.Height - 2), Projectile.rotation);
			return false;
		}
	}

	private float _swingArc = 3;

	public override void SetDefaults()
	{
		Item.damage = 18;
		Item.crit = 6;
		Item.knockBack = 3;
		Item.useTime = Item.useAnimation = 25;
		Item.DamageType = DamageClass.Magic;
		Item.width = Item.height = 46;
		Item.useStyle = ItemUseStyleID.Swing;
		Item.value = Item.sellPrice(gold: 1);
		Item.rare = ItemRarityID.Blue;
		Item.UseSound = SoundID.Item1;
		Item.shoot = ModContent.ProjectileType<WindshearScepterSwing>();
		Item.shootSpeed = 1f;
		Item.autoReuse = true;
		Item.useTurn = true;
		Item.noUseGraphic = true;
		Item.noMelee = true;
	}

	public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
	{
		SwungProjectile.Spawn(position, velocity, type, damage, knockback, player, _swingArc *= -1, source, player.altFunctionUse - 1);
		return false;
	}
}