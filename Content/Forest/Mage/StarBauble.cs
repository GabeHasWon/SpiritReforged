using SpiritReforged.Common.Easing;
using SpiritReforged.Common.ItemCommon.Abstract;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.PlayerCommon;
using SpiritReforged.Common.ProjectileCommon.Abstract;
using SpiritReforged.Common.Visuals;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Forest.Mage;

public class StarBauble : EquippableItem
{
	public sealed class BaubleWandSwung : SwungProjectile, IDrawPixelated
	{
		public override string Texture => AssetLoader.EmptyTexture;

		public int ItemType
		{
			get => (int)Projectile.ai[0];
			set => Projectile.ai[0] = value;
		}

		private bool _resetConfiguration;

		public override IConfiguration SetConfiguration() => new BasicConfiguration(EaseFunction.EaseCubicOut, (int)TextureAssets.Item[ItemType].Size().Length() + 28, 25);

		public override void AI()
		{
			base.AI();
			if (!Main.dedServ && !_resetConfiguration)
			{
				SetConfiguration(); //Set the configuration again as ItemType will always equal zero before AI runs once
				_resetConfiguration = true;
			}
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			Player owner = Main.player[Projectile.owner];
			int increase = (int)Math.Ceiling(damageDone * 0.1f);
			int statManaOld = owner.statMana;

			owner.statMana = Math.Min(owner.statMana + increase, owner.statManaMax2);

			if (statManaOld < owner.statMana)
			owner.ManaEffect(owner.statMana - statManaOld);
		}

		public override float GetRotation(out float armRotation, out Player.CompositeArmStretchAmount stretch)
		{
			float value = base.GetRotation(out armRotation, out stretch);
			return value + (MathHelper.PiOver4 - Progress) * SwingDirection;
		}

		public override bool PreDraw(ref Color lightColor)
		{
			Texture2D texture = TextureAssets.Item[ItemType].Value;
			SpriteEffects effects = (SwingDirection == -1) ? SpriteEffects.FlipVertically : default;
			Vector2 origin = new(4, (effects == SpriteEffects.FlipVertically) ? 6 : texture.Height - 6); //The handle

			Vector2 position = Projectile.Center - Main.screenPosition + new Vector2(0, Projectile.gfxOffY);
			Main.EntitySpriteDraw(texture, position, null, Projectile.GetAlpha(lightColor), Projectile.rotation, origin, Projectile.scale, effects);

			return false;
		}

		void IDrawPixelated.DrawPixelated(SpriteBatch spriteBatch) => DrawPixelatedSmear(spriteBatch, TextureColorCache.GetBrightestColor(TextureAssets.Item[ItemType].Value));

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
	}

	private sealed class BaubleGlobalItem : GlobalItem
	{
		public static bool Active(Item item, Player player) => item.DamageType.CountsAsClass(DamageClass.Magic) && player.HasEquip<StarBauble>();

		public override void Load() => On_PlayerDrawLayers.DrawPlayer_27_HeldItem += HideItem;

		private static void HideItem(On_PlayerDrawLayers.orig_DrawPlayer_27_HeldItem orig, ref PlayerDrawSet drawinfo)
		{
			if (drawinfo.drawPlayer.altFunctionUse == 2 && Active(drawinfo.heldItem, drawinfo.drawPlayer))
			{
				bool noUseGraphic = drawinfo.heldItem.noUseGraphic;
				drawinfo.heldItem.noUseGraphic = true;

				orig(ref drawinfo);

				drawinfo.heldItem.noUseGraphic = noUseGraphic;
			}
			else
			{
				orig(ref drawinfo);
			}
		}

		public override bool AltFunctionUse(Item item, Player player) => Active(item, player);

		public override bool Shoot(Item item, Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
		{
			if (player.altFunctionUse == 2 && Active(item, player))
			{
				SwungProjectile.Spawn(position, velocity, ModContent.ProjectileType<BaubleWandSwung>(), damage, knockback, player, 3, source, item.type);
				return false;
			}

			return true;
		}

		public override void ModifyManaCost(Item item, Player player, ref float reduce, ref float mult)
		{
			if (player.altFunctionUse == 2 && Active(item, player))
				mult = 0; //Consume no mana on alt function
		}
	}

	public override void SetDefaults()
	{
		Item.rare = ItemRarityID.Blue;
		Item.accessory = true;
	}
}