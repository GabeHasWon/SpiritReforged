using SpiritReforged.Common.Easing;
using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.ModCompat;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.PlayerCommon;
using SpiritReforged.Common.Visuals.Glowmasks;
using SpiritReforged.Content.Particles;
using Terraria.DataStructures;
using Terraria.GameContent.ItemDropRules;

namespace SpiritReforged.Content.Underground.Items.FingerGun;

[AutoloadGlowmask("255,255,255")]
[AutoloadEquip(EquipType.HandsOn)]
public class FingerGun : ModItem
{
	public override void SetStaticDefaults()
	{
		ItemLootDatabase.AddItemRule(ItemID.IronCrate, ItemDropRule.Common(Type, 15));
		ItemLootDatabase.AddItemRule(ItemID.IronCrateHard, ItemDropRule.Common(Type, 15));

		MoRHelper.AddElement(Item, MoRHelper.Arcane, true);
	}

	public override void SetDefaults()
	{
		Item.DamageType = DamageClass.Magic;
		Item.Size = new(32, 28);
		Item.damage = 12;
		Item.ArmorPenetration = 14;
		Item.mana = 3;
		Item.crit = 10;
		Item.knockBack = 1;
		Item.useTime = Item.useAnimation = 30;
		Item.useStyle = ItemUseStyleID.RaiseLamp;
		Item.noMelee = true;
		Item.noUseGraphic = true;
		Item.value = Item.buyPrice(0, 1, 0, 0);
		Item.rare = ItemRarityID.Blue;
		Item.autoReuse = true;
		Item.shoot = ModContent.ProjectileType<FingerShot>();
		Item.UseSound = SoundID.Item114.WithPitchOffset(0.33f) with { MaxInstances = 3 };
		Item.shootSpeed = 18f;
	}

	public override float UseSpeedMultiplier(Player player)
	{
		float manaPercentage = player.statMana / (float)player.statManaMax2;
		return MathHelper.Lerp(1, 3, manaPercentage);
	}

	public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
	{
		velocity = velocity.RotatedByRandom(MathHelper.Pi / 16);
		velocity *= Main.rand.NextFloat(0.9f, 1.2f) * 0.66f;
		position = player.GetFrontHandPosition(player.compositeFrontArm.stretch, player.compositeFrontArm.rotation) + velocity;

		if (!Main.dedServ)
		{
			Vector2 handPos = player.GetFrontHandPosition(Player.CompositeArmStretchAmount.Full, velocity.ToRotation() - MathHelper.PiOver2);
			handPos += velocity / 3;
			ParticleHandler.SpawnParticle(new LightBurst(handPos, Main.rand.NextFloatDirection(), Color.Lerp(Color.LightCyan, Color.Cyan, 0.5f).Additive(), 0.25f, 20));
			ParticleHandler.SpawnParticle(new TexturedPulseCircle(handPos, Color.Cyan.Additive(), 1, 60, 16, "GlowTrail_2", new(1, 0.5f), EaseFunction.EaseCircularOut, false, 0.5f).WithSkew(0.8f, velocity.ToRotation() - MathHelper.Pi));
		}
	}
}

public class FingerGunArmManager : ModPlayer
{
	public override void Load() => AssetLoader.LoadedTextures.Add("FingerFlame", Mod.Assets.Request<Texture2D>("Content/Underground/Items/FingerGun/FingerGun_Flame", AssetRequestMode.ImmediateLoad));

	private static bool IsFingerGunHeld(Player player) => player.HeldItem.type == ModContent.ItemType<FingerGun>();

	public override void PostUpdate()
	{
		if(IsFingerGunHeld(Player))
		{
			Player.GetModPlayer<ExtraDrawOnPlayer>().DrawDict.Add(FireHandDraw, ExtraDrawOnPlayer.DrawType.Additive);

			if (Player.ItemAnimationActive)
			{
				float animProgress = Player.itemAnimation / (float)Player.itemAnimationMax;

				int signDirection = Math.Sign(Player.DirectionTo(Main.MouseWorld).X);
				if (signDirection != Player.direction && signDirection != 0)
					Player.ChangeDir(signDirection);

				float armRot = Player.AngleTo(Main.MouseWorld) - MathHelper.PiOver2;
				Player.CompositeArmStretchAmount armStretch = Player.CompositeArmStretchAmount.Full;

				armStretch = animProgress switch
				{
					< 0.25f => Player.CompositeArmStretchAmount.Full,
					< 0.5f => Player.CompositeArmStretchAmount.ThreeQuarters,
					< 0.75f => Player.CompositeArmStretchAmount.Quarter,
					_ => Player.CompositeArmStretchAmount.ThreeQuarters
				};

				armRot += Player.direction * (EaseFunction.CompoundEase([EaseFunction.EaseCubicOut, EaseFunction.EaseSine]).Ease(animProgress) - 0.5f);

				Player.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, 0);
				Player.SetCompositeArmFront(true, armStretch, armRot);
			}
		}
	}

	public override void ModifyDrawInfo(ref PlayerDrawSet drawInfo)
	{
		if(IsFingerGunHeld(drawInfo.drawPlayer))
		{
			drawInfo.drawPlayer.handon = EquipLoader.GetEquipSlot(SpiritReforgedMod.Instance, "FingerGun", EquipType.HandsOn);
			drawInfo.cHandOn = -1;
		}
	}

	private void FireHandDraw(SpriteBatch spriteBatch)
	{
		if(IsFingerGunHeld(Player) && !Player.dead && Player.active)
		{
			int curFrame = (int)(12 * Main.GlobalTimeWrappedHourly % 4);
			Texture2D flameTex = AssetLoader.LoadedTextures["FingerFlame"].Value;

			Rectangle flameDrawRect = flameTex.Bounds;
			flameDrawRect.Height /= 4;
			flameDrawRect.Y = flameDrawRect.Height * curFrame;

			Vector2 handPos = (Player.HandPosition ?? Player.MountedCenter) - Main.screenPosition;
			if (Player.compositeFrontArm.enabled)
				handPos = Player.GetFrontHandPosition(Player.compositeFrontArm.stretch, Player.compositeFrontArm.rotation) - Main.screenPosition;

			Vector2 flameOrigin = flameDrawRect.Size() / 2;
			flameOrigin.Y += flameDrawRect.Height / 4;

			Color flameColor = Color.White;
			flameColor *= EaseFunction.EaseQuadIn.Ease(Player.statMana / (float)Player.statManaMax2) * 0.9f;

			Main.EntitySpriteDraw(flameTex, handPos, flameDrawRect, flameColor, 0, flameOrigin, 1f, SpriteEffects.None);
		}
	}
}