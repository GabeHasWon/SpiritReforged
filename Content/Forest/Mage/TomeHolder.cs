using Microsoft.Xna.Framework.Graphics;
using SpiritReforged.Common;
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
			const int inactive_time = 120;

			Vector2 targetCenter = Owner.Center - new Vector2(30 * Owner.direction, 20 * Owner.gravDir);
			float opacity = Counter / UseTime;

			if (Counter < 1)
			{
				if (Owner.ItemAnimationActive) //If using an item, start the counter
				{
					Counter = 1;
				}
				else if (Counter-- < -inactive_time)
				{
					targetCenter = Owner.Center;

					if (Projectile.DistanceSQ(targetCenter) < 50)
						Projectile.Kill();
				}
			}
			else if (Counter++ >= UseTime && Item != null)
			{
				if (Owner.whoAmI == Main.myPlayer)
					ProjectileDuplicator.ShootFrom(Projectile.Center - Owner.Size / 2, Owner, Item, ProjectileDuplicator.ShootSettings.None);

				Counter = 0;
			}

			Projectile.Center = Vector2.Lerp(Projectile.Center, targetCenter, 0.05f);
			Projectile.rotation = (EaseFunction.EaseSine.Ease((float)Main.timeForVisualEffects / 50f) - 0.5f) * 0.1f;
			Projectile.direction = Projectile.spriteDirection = Math.Sign(Owner.Center.X - Projectile.Center.X);
		}

		public override bool? CanDamage() => false;

		public override bool PreDraw(ref Color lightColor)
		{
			Texture2D texture = TextureAssets.Item[ItemType].Value;
			Texture2D bloom = AssetLoader.LoadedTextures["Bloom"].Value;
			float opacity = Counter / UseTime;

			SpriteEffects effects = (Projectile.spriteDirection == -1) ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
			Vector2 position = Projectile.Center - Main.screenPosition + new Vector2(0, Projectile.gfxOffY + EaseFunction.EaseSine.Ease((float)Main.timeForVisualEffects / 30f) * 3);

			if (opacity > 0)
			{
				DrawHelpers.DrawOutline(default, default, default, default, (offset) =>
					Main.EntitySpriteDraw(texture, position + offset, null, Projectile.GetAlpha(Color.White).Additive() * opacity, Projectile.rotation, texture.Size() / 2, Projectile.scale, effects));

				Main.EntitySpriteDraw(bloom, position, null, Projectile.GetAlpha(Color.Cyan).Additive() * 0.5f * opacity, Projectile.rotation, bloom.Size() / 2, Projectile.scale * 0.3f, effects);
			}

			Main.EntitySpriteDraw(texture, position, null, Projectile.GetAlpha(lightColor), Projectile.rotation, texture.Size() / 2, Projectile.scale, effects);
			return false;
		}
	}

	private sealed class TomeSummonPlayer : ModPlayer
	{
		public bool TryGetTome(out int type)
		{
			const int hotbar_slots = 9;

			for (int i = 0; i < hotbar_slots; i++)
			{
				Item item = Player.inventory[i];
				if (SpiritSets.MagicBook[item.type])
				{
					type = item.type;
					return true;
				}
			}

			type = ItemID.None;
			return false;
		}

		public override bool Shoot(Item item, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
		{
			if (Player.HasEquip<TomeHolder>() && Player.ownedProjectileCounts[ModContent.ProjectileType<MagicTome>()] == 0 && TryGetTome(out int tomeType))
				Projectile.NewProjectile(source, position, Vector2.Zero, ModContent.ProjectileType<MagicTome>(), damage, knockback, Player.whoAmI, tomeType);

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