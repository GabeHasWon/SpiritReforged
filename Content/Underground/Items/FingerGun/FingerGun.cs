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
		return MathHelper.Lerp(1, 2.5f, manaPercentage);
	}

	public override void HoldItem(Player player)
	{
		float manaPercentage = player.statMana / (float)player.statManaMax2;
		if (Main.rand.NextBool(3) && !Main.dedServ && player.HandPosition != null && !player.ItemAnimationActive)
		{
			Vector2 handPos = player.HandPosition.Value;

			var fire = new FireParticle(handPos + Vector2.UnitX * player.direction * 3,
							   -Vector2.UnitY * Main.rand.NextFloat(0.9f, 1.1f) / 2,
							   [Color.LightCyan.Additive(), Color.Cyan.Additive(), Color.DarkGreen.Additive()],
							   manaPercentage * manaPercentage,
							   Main.rand.NextFloat(0.01f, 0.035f),
							   EaseFunction.EaseCircularIn,
							   Main.rand.Next(10, 40));

			ParticleHandler.SpawnParticle(fire);
		}
	}

	public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
	{
		float manaPercentage = player.statMana / (float)player.statManaMax2;
		velocity = velocity.RotatedByRandom(MathHelper.Pi / 32);
		velocity *= Main.rand.NextFloat(0.9f, 1.2f) * 1.33f;
		position = player.GetFrontHandPosition(player.compositeFrontArm.stretch, player.compositeFrontArm.rotation);

		if (!Main.dedServ)
		{
			Vector2 handPos = player.GetFrontHandPosition(Player.CompositeArmStretchAmount.Full, velocity.ToRotation() - MathHelper.PiOver2);
			ParticleHandler.SpawnParticle(new LightBurst(handPos, Main.rand.NextFloatDirection(), Color.Lerp(Color.LightCyan, Color.Cyan, 0.5f).Additive(), 0.25f, 12));

			for (int i = 0; i < 5; i++)
				ParticleHandler.SpawnParticle(new FireParticle(handPos,
												   Main.rand.NextVector2Unit() * Main.rand.NextFloat() - Vector2.UnitY * Main.rand.NextFloat(2), 
												   [Color.LightCyan.Additive(), Color.Cyan.Additive(), Color.DarkGreen.Additive()],
												   manaPercentage * manaPercentage,
												   0,
												   Main.rand.NextFloat(0.01f, 0.035f),
												   EaseFunction.EaseCircularIn,
												   Main.rand.Next(10, 40)));
		}
	}
}

public class FingerGunArmManager : ModPlayer
{
	private static bool IsFingerGunHeld(Player player) => player.HeldItem.type == ModContent.ItemType<FingerGun>();

	public override void PostUpdate()
	{
		if(IsFingerGunHeld(Player))
		{
			if (Player.ItemAnimationActive)
			{
				float animProgress = Player.itemAnimation / (float)Player.itemAnimationMax;

				int signDirection = Math.Sign(Player.DirectionTo(Main.MouseWorld).X);
				if (signDirection != Player.direction && signDirection != 0)
					Player.ChangeDir(signDirection);

				float armRot = Player.AngleTo(Main.MouseWorld) - MathHelper.PiOver2;
				Player.CompositeArmStretchAmount armStretch;

				armStretch = animProgress switch
				{
					< 0.25f => Player.CompositeArmStretchAmount.Full,
					< 0.5f => Player.CompositeArmStretchAmount.ThreeQuarters,
					< 0.75f => Player.CompositeArmStretchAmount.Quarter,
					_ => Player.CompositeArmStretchAmount.ThreeQuarters
				};

				armRot += Player.direction * (EaseFunction.CompoundEase([EaseFunction.EaseCircularOut, EaseFunction.EaseSine, EaseFunction.EaseCircularIn]).Ease(animProgress) - 0.5f);

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
}