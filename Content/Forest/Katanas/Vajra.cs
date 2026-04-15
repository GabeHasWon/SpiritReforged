using SpiritReforged.Common;
using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.ModCompat;
using SpiritReforged.Common.PlayerCommon;
using SpiritReforged.Common.ProjectileCommon.Abstract;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Forest.Katanas;

public class Vajra : ModItem, IDrawHeld
{
	public sealed class VajraSwing : SwungProjectile
	{
		public override LocalizedText DisplayName => ModContent.GetInstance<Vajra>().DisplayName;

		public override void SetStaticDefaults() => Main.projFrames[Type] = 4;

		public override IConfiguration SetConfiguration() => new BasicConfiguration(Common.Easing.EaseFunction.EaseCubicOut, 84, 25);

		public override bool PreDraw(ref Color lightColor)
		{
			SpriteEffects effects = (SwingDirection == -1) ? SpriteEffects.FlipVertically : default;
			Vector2 origin = new(4, 24); //The handle

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
		Item.shoot = ModContent.ProjectileType<VajraSwing>();
		Item.shootSpeed = 1f;
		Item.autoReuse = true;
		Item.useTurn = true;
		Item.noUseGraphic = true;
		Item.noMelee = true;
		MoRHelper.SetSlashBonus(Item);
	}

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