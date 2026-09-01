using SpiritReforged.Common.Easing;
using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.ItemCommon.MagazineSystem;
using SpiritReforged.Common.NPCCommon;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.Subclasses.Shotguns;
using SpiritReforged.Content.Aether.Items;
using SpiritReforged.Content.Particles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.Audio;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Underground.Items.Pepperbox;

public class Pepperbox() : ShotgunItem(new ShotgunStats())
{
	class PepperboxFlash : Particle
	{
		int _variant;
		public override ParticleLayer DrawLayer => ParticleLayer.BelowProjectile;
		public override ParticleDrawType DrawType => ParticleDrawType.Custom;

		public PepperboxFlash(Vector2 position, Vector2 velocity, float scale, int maxTime)
		{
			Position = position;
			Velocity = velocity;
			Scale = scale;
			MaxTime = maxTime;

			Color = Color.White;

			_variant = Main.rand.Next(3);
		}

		public override void Update()
		{
			Velocity *= 0.95f;
		}

		public override void CustomDraw(SpriteBatch spriteBatch)
		{
			var texture = ParticleHandler.GetTexture(Type);
			var bloom = AssetLoader.LoadedTextures["Bloom"].Value;

			var frame = texture.Frame(1, 3, 0, _variant);

			float progress = EaseBuilder.EaseCircularOut.Ease(1f - Progress);

			if (progress > 0.9f)
			{
				float lerp = (progress - 0.9f) / 0.1f;

				Main.spriteBatch.Draw(texture, Position - Main.screenPosition, frame, Color * lerp, 0f, frame.Size() / 2f, MathHelper.Lerp(Scale, Scale * 0.75f, 1f - lerp), 0f, 0f);
			}

			Main.spriteBatch.Draw(bloom, Position - Main.screenPosition, null, new Color(255, 150, 0, 0) * progress * 0.25f, 0f, bloom.Size() / 2f, MathHelper.Lerp(Scale, Scale * 0.75f, 1f - progress) * 0.33f, 0f, 0f);

			Main.spriteBatch.Draw(bloom, Position - Main.screenPosition, null, new Color(255, 180, 0, 0) * progress * 0.2f, 0f, bloom.Size() / 2f, MathHelper.Lerp(Scale, Scale * 0.75f, 1f - progress) * 0.25f, 0f, 0f);
		}
	}

	public override void SetStaticDefaults() => NPCShopHelper.AddEntry(NPCShopHelper.ConditionalEntry.FromNPC(NPCID.ArmsDealer, new NPCShop.Entry(Type, Condition.DownedEyeOfCthulhu)));

	public override void SafeSetDefaults()
	{
		Item.damage = 8;
		Item.knockBack = 6;
		Item.width = 40;
		Item.height = 20;
		Item.useTime = Item.useAnimation = 25;
		Item.useStyle = ItemUseStyleID.Shoot;
		Item.noMelee = true;
		Item.noUseGraphic = false;
		Item.value = Item.buyPrice(0, 1, 50, 0);
		Item.rare = ItemRarityID.Blue;
		Item.autoReuse = true;

		var globalItem = Item.GetGlobalItem<MagazineGlobalItem>();

		globalItem.ActivateMagazine(Item, (pitch, position) => SoundEngine.PlaySound(SoundID.Item36 with { Pitch = pitch}, position), new(-0.2f, 0.5f, 4, 90), new(52, 24), new(-24, -2), MagazineReloadType.OneAtATime, MagazineUIType.Shell, true, -6, -0.15f);
		globalItem.SetAnimations(new(0.04f, 0.96f), reloadStyle: ReloadUseStyle, reloadFrame: ReloadUseFrame);
	}

	public override void AdditionalShoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, ShotgunAmmoItem ammo, int damage, float knockback)
	{
		Vector2 shellPosition = position + new Vector2(12, -10 * player.direction).RotatedBy(velocity.ToRotation());
		Vector2 muzzlePosition = position + new Vector2(56, -2 * player.direction).RotatedBy(velocity.ToRotation());

		ParticleHandler.SpawnParticle(new ShotgunShellParticle(shellPosition, -velocity.RotatedByRandom(0.4f) * Main.rand.NextFloat(1f, 4f) - Vector2.UnitY * Main.rand.NextFloat(3f), 1f, 120, ammo));

		ParticleHandler.SpawnParticle(new PepperboxFlash(muzzlePosition, velocity, Main.rand.NextFloat(0.9f, 1.1f), 20 + Main.rand.Next(5, 10)));

		for (int i = 0; i < 3; i++)
		{
			ParticleHandler.SpawnParticle(new CompositeSmoke(muzzlePosition + Main.rand.NextVector2Circular(5f, 5f), velocity, Color.White, 75, false, false, (particle) => particle.Velocity.Y -= 0.01f, 0.02f));

			ParticleHandler.SpawnParticle(new EmberParticle(muzzlePosition, velocity.RotatedByRandom(0.3f) * Main.rand.NextFloat(3f), new Color(200, 160, 0, 0), 0.2f, 30, 2));
		}
	}

	public override bool ModifyItemDraw(ref PlayerDrawSet drawInfo, ref DrawData drawData, ref DrawData? coloredDrawData, ref DrawData? glowMaskDrawData)
	{
		/*if (Item.TryGetGlobalItem<MagazineGlobalItem>(out var magazineWeapon) && magazineWeapon.Reloading)
		{
			return false;
		}*/

		return true;
	}

	public static void ReloadUseStyle(Item item, Player player, Rectangle heldItemFrame, int shootDirection, float shootRotation, Vector2 itemSize, Vector2 itemOrigin, float animProgress)
	{
		float itemRotation = player.compositeBackArm.rotation + 1.5707964f * player.gravDir;
		Vector2 itemPosition = player.MountedCenter;

		if (animProgress < 0.15f)
		{
			float lerper = animProgress / 0.15f;
			itemPosition += itemRotation.ToRotationVector2() * MathHelper.Lerp(-4f, -8f, EaseFunction.EaseCircularInOut.Ease(lerper));
		}
		else
		{
			if (animProgress < 0.75f)
			{
				itemPosition += itemRotation.ToRotationVector2() * -8f;
			}
			else
			{
				float lerper = (animProgress - 0.75f) / 0.25f;
				itemPosition += itemRotation.ToRotationVector2() * MathHelper.Lerp(-8f, -2f, EaseFunction.EaseCircularInOut.Ease(lerper));
			}
		}

		ItemVisualHelpers.CleanHoldStyle(player, itemRotation, itemPosition, itemSize, itemOrigin, true, false, true);
	}

	public static void ReloadUseFrame(Item item, Player player, int shootDirection, float shootRotation, Vector2 itemSize, Vector2 itemOrigin, float animProgress)
	{
		float rotation = shootRotation * player.gravDir + 1.5707964f;
		float frontArmRotation = shootRotation * player.gravDir + 1.5707964f;

		Player.CompositeArmStretchAmount frontStretch = Player.CompositeArmStretchAmount.Full;

		if (animProgress < 0.25f)
		{
			if (animProgress < 0.1f)
			{
				float lerper = animProgress / 0.1f;
				rotation += MathHelper.Lerp(0f, -0.15f, EaseFunction.EaseCircularOut.Ease(lerper)) * player.direction;
				frontArmRotation += MathHelper.Lerp(0f, -0.15f, EaseFunction.EaseCircularOut.Ease(lerper)) * player.direction;
			}
			else
			{
				float lerper = (animProgress - 0.1f) / 0.15f;
				rotation += MathHelper.Lerp(-0.15f, 0.25f, EaseFunction.EaseCircularInOut.Ease(lerper)) * player.direction;
				frontArmRotation -= 0.15f * player.direction;
			}
		}
		else
		{
			if (animProgress > 0.75f)
			{
				float lerper = (animProgress - 0.75f) / 0.25f;
				rotation += MathHelper.Lerp(0.25f, 0f, EaseFunction.EaseCircularInOut.Ease(lerper)) * player.direction;
			}
			else
			{
				frontStretch = Player.CompositeArmStretchAmount.Quarter;

				frontArmRotation -= 0.15f * player.direction;

				rotation += 0.25f * player.direction;
			}
		}

		player.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, rotation);
		player.SetCompositeArmFront(true, frontStretch, frontArmRotation);
	}
}
