using SpiritReforged.Common;
using SpiritReforged.Common.Easing;
using SpiritReforged.Common.ModCompat;
using SpiritReforged.Common.ProjectileCommon.Abstract;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Forest.Katanas;

public class DamascusKatana : ModItem
{
	public sealed class DamascusKatanaSwing : SwungProjectile
	{
		public bool Secondary { get => Projectile.ai[0] == 1; set => Projectile.ai[0] = value ? 1 : 0; }

		public override LocalizedText DisplayName => ModContent.GetInstance<DamascusKatana>().DisplayName;

		public override void SetStaticDefaults() => Main.projFrames[Type] = 3;

		public override IConfiguration SetConfiguration() => new BasicConfiguration(EaseFunction.EaseCubicOut, 60, 25);

		public override void AI()
		{
			base.AI();

			if (SwingArc == 0)
				HoldDistance = Math.Max(40 * (0.5f - Progress * 2), -10);

			if (Secondary && Counter == 1)
			{
				Player owner = Main.player[Projectile.owner];

				if (owner.velocity.Y == 0)
					owner.velocity.Y -= 8;
			}
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (Secondary)
				target.velocity.Y -= 8 * target.knockBackResist;
		}

		public override bool PreDraw(ref Color lightColor)
		{
			SpriteEffects effects = (SwingDirection == -1) ? SpriteEffects.FlipVertically : default;
			Vector2 origin = new(4, 18); //The handle
			Rectangle source = (SwingArc == 0) ? TextureAssets.Projectile[Type].Frame(1, Main.projFrames[Type], 0, Main.projFrames[Type] - 1, 0, -2) : default;

			if (Secondary)
				DrawSmear(Color.Gray, Projectile.rotation, effects);

			DrawHeld(lightColor, origin, Projectile.rotation, effects, source);

			return false;
		}
	}

	private float _swingArc;

	public override void SetStaticDefaults() => SpiritSets.IsSword[Type] = true;

	public override void SetDefaults()
	{
		Item.DefaultToSpear(ModContent.ProjectileType<DamascusKatanaSwing>(), 1, 19);
		Item.SetShopValues(ItemRarityColor.White0, Item.sellPrice(silver: 30));
		Item.damage = 12;
		Item.knockBack = 3;
		Item.autoReuse = true;
		MoRHelper.SetSlashBonus(Item);
	}

	public override bool AltFunctionUse(Player player) => true;

	public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
	{
		if (player.altFunctionUse == 2)
		{
			SwungProjectile.Spawn(position, Vector2.Normalize(new Vector2(1 * player.direction, -1)), type, damage, knockback, player, -4, source, 1);
		}
		else
		{
			_swingArc = _swingArc switch
			{
				3f => -5f,
				-5f => 0f,
				_ => 3f
			};

			SwungProjectile.Spawn(position, velocity, type, damage, knockback, player, _swingArc, source);
		}

		return false;
	}

	public override void AddRecipes() { }
}