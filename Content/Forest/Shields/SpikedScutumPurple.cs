using SpiritReforged.Common.Easing;
using SpiritReforged.Common.ProjectileCommon.Abstract;
using SpiritReforged.Common.Subclasses.Greatshields;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Forest.Shields;

public class SpikedScutumPurple : GreatshieldItem
{
	public class ScutumBash : SwungProjectile
	{
		public override string Texture => AssetLoader.EmptyTexture;

		public override IConfiguration SetConfiguration() => new BasicConfiguration(EaseFunction.MultistepEase(EaseFunction.EaseSine, EaseFunction.EaseCubicOut, 0.3f), 40, 25);

		public override void AI()
		{
			base.AI();

			if (Progress < 0.5f)
			{
				Dust dust = Dust.NewDustPerfect(Projectile.Center + Projectile.velocity * 10 + Main.rand.NextVector2Circular(30, 30) * Main.rand.NextFloat(), DustID.Blood, Projectile.velocity, 100);
				dust.noGravity = true;
			}

			Main.player[Projectile.owner].SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - MathHelper.PiOver2);
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => target.AddBuff(BuffID.Bleeding, 60 * 5);

		public override bool PreDraw(ref Color lightColor)
		{
			int direction = Projectile.direction;
			Color color = Projectile.GetAlpha(lightColor);
			BasicConfiguration config = GetConfig<BasicConfiguration>();

			if (Main.player[Projectile.owner].HeldItem.ModItem is GreatshieldItem shieldItem)
			{
				Texture2D texture = shieldItem.HeldTexture;
				SpriteEffects effects = (direction == -1) ? SpriteEffects.FlipVertically : SpriteEffects.None;
				float rotation = Projectile.rotation + EaseFunction.EaseSine.Ease(Progress * 2) * 0.2f * Projectile.direction;

				Vector2 position = Projectile.Center - Main.screenPosition + new Vector2(0, Projectile.gfxOffY)
					+ (Vector2.UnitX * (config.Reach / 2f) * config.Easing.Ease(1f - Progress)).RotatedBy(Projectile.rotation);

				Main.EntitySpriteDraw(texture, position, null, color, rotation, texture.Size() / 2, Projectile.scale, effects);
			}

			#region wave
			Main.instance.LoadProjectile(ProjectileID.DD2SquireSonicBoom);

			Texture2D waveTexture = TextureAssets.Projectile[ProjectileID.DD2SquireSonicBoom].Value;
			Vector2 wavePosition = Projectile.Center - Main.screenPosition + (Vector2.UnitX * config.Reach * Progress).RotatedBy(Projectile.rotation);

			Main.EntitySpriteDraw(waveTexture, wavePosition, null, color * (1f - Progress) * 0.3f, Projectile.rotation + MathHelper.PiOver2, 
				waveTexture.Size() / 2, new Vector2(0.7f + Progress * 0.3f, 1f - Progress * 0.3f) * Projectile.scale * 0.7f, 0);
			#endregion

			return false;
		}
	}

	public override ShieldInfo SetInfo()
	{
		Item.defense = 6;
		Item.damage = 28;
		Item.useTime = Item.useAnimation = 40;
		Item.knockBack = 12;
		Item.shoot = ModContent.ProjectileType<ScutumBash>();

		return new ShieldInfo(40, 60);
	}

	public override void OnBlockDamage(Player player, Player.HurtInfo info) { }

	public override void DrawShield(ref PlayerDrawSet drawInfo, bool guarding)
	{
		if (drawInfo.drawPlayer.ownedProjectileCounts[ModContent.ProjectileType<ScutumBash>()] == 0) //Don't draw while performing a shield bash
			base.DrawShield(ref drawInfo, guarding);
	}

	public override void AddRecipes() => CreateRecipe().AddIngredient(ItemID.DemoniteBar, 5).AddIngredient(ItemID.Ebonwood, 18).AddTile(TileID.Anvils).Register();
}