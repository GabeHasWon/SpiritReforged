using SpiritReforged.Common.Easing;
using SpiritReforged.Common.ItemCommon.Abstract;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.PlayerCommon;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Desert.Silk;
using SpiritReforged.Content.Particles;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Forest.Mage;

public class TomeHolder : EquippableItem
{
	public sealed class MagicTome : ModProjectile
	{
		public override string Texture => AssetLoader.EmptyTexture;

		public Player Owner => Main.player[Projectile.owner];

		public Item Item
		{
			get => _item ??= ContentSamples.ItemsByType[ItemType].Clone();
			set => _item = value;
		}
		private Item _item;

		public int UseTime
		{
			get => (ItemType != ItemID.None && _itemUseTime == 0) ? (_itemUseTime = Item.useTime) : (int)(_itemUseTime / (Owner.GetTotalAttackSpeed(DamageClass.Magic) * FIRE_RATE));
			set => _itemUseTime = value;
		}
		private int _itemUseTime;

		public int ItemType
		{
			get => (int)Projectile.ai[0];
			set => Projectile.ai[0] = value;
		}

		public ref float Counter => ref Projectile.ai[1];

		public override void SetDefaults()
		{
			Projectile.DamageType = DamageClass.Magic;
			Projectile.Size = new Vector2(16);
			Projectile.tileCollide = false;
		}

		public override void AI()
		{
			Vector2 targetCenter = Owner.Center - new Vector2(30 * Owner.direction, 20 * Owner.gravDir);
			Projectile.Center = targetCenter - Owner.velocity;

			if (!Owner.ItemAnimationActive && Counter++ >= UseTime && Item != null)
			{
				if (Owner.whoAmI == Main.myPlayer)
					ProjectileDuplicator.ShootFrom(Projectile.Center - Owner.Size / 2, Owner, Item, true);

				Counter = 0;
			}

			if (!Main.dedServ && Main.rand.NextBool(10))
				ParticleHandler.SpawnParticle(new EmberParticle(Main.rand.NextVector2FromRectangle(Projectile.Hitbox), -Vector2.UnitY, Color.Cyan, 0.8f, 25, 2));
		}

		public override bool? CanDamage() => false;

		public override bool PreDraw(ref Color lightColor)
		{
			Texture2D texture = TextureAssets.Item[ItemType].Value;
			Texture2D bloom = AssetLoader.LoadedTextures["Bloom"].Value;

			SpriteEffects effects = (Owner.direction == -1) ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
			Vector2 position = Projectile.Center - Main.screenPosition + new Vector2(0, Projectile.gfxOffY + EaseFunction.EaseSine.Ease((float)Main.timeForVisualEffects / 30f) * 3);

			DrawHelpers.DrawOutline(default, default, default, default, (offset) =>
				Main.EntitySpriteDraw(TextureColorCache.ColorSolid(texture, Color.White), position + offset, null, Projectile.GetAlpha(Color.Cyan).Additive(), Projectile.rotation, texture.Size() / 2, Projectile.scale, effects));

			Main.EntitySpriteDraw(bloom, position, null, Projectile.GetAlpha(Color.Cyan).Additive() * 0.5f, Projectile.rotation, bloom.Size() / 2, Projectile.scale * 0.3f, effects);
			Main.EntitySpriteDraw(texture, position, null, Projectile.GetAlpha(lightColor), Projectile.rotation, texture.Size() / 2, Projectile.scale, effects);

			return false;
		}
	}

	private sealed class TomeSummonPlayer : ModPlayer
	{
		public override bool Shoot(Item item, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
		{
			if (Player.HasEquip<TomeHolder>() && Player.ownedProjectileCounts[ModContent.ProjectileType<MagicTome>()] == 0)
			{
				int tomeType = ItemID.CrystalStorm;
				Projectile.NewProjectile(source, position, Vector2.Zero, ModContent.ProjectileType<MagicTome>(), damage, knockback, Player.whoAmI, tomeType);
			}

			return true;
		}
	}

	public const float FIRE_RATE = 0.2f;

	public override void SetDefaults()
	{
		Item.rare = ItemRarityID.Green;
		Item.accessory = true;
	}
}