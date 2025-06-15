using Terraria.DataStructures;
using SpiritReforged.Common.ProjectileCommon.Abstract;
using SpiritReforged.Common.ProjectileCommon;
using SpiritReforged.Common.ModCompat;

namespace SpiritReforged.Common.ItemCommon.Abstract;

public abstract class ClubItem : ModItem
{
	internal virtual int ChargeTime { get; set; }
	internal virtual int SwingTime { get; set; }
	internal virtual float DamageScaling => 2;
	internal virtual float KnockbackScaling => 1.5f;

	public virtual void SafeSetDefaults() { }

	public sealed override void SetDefaults()
	{
		Item.channel = true;
		Item.autoReuse = true;
		Item.useTime = 320;
		Item.useAnimation = 320;
		Item.useStyle = ItemUseStyleID.Swing;
		Item.DamageType = DamageClass.Melee;
		Item.noMelee = true;
		Item.noUseGraphic = true;
		Item.useTurn = true;
		Item.shootSpeed = 1f;
		Item.reuseDelay = 10;

		MoRHelper.SetHammerBonus(Item);

		SafeSetDefaults();
	}

	public override bool MeleePrefix() => true;

	public override Vector2? HoldoutOffset() => new Vector2(-10, 0);

	public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
	{
		PreNewProjectile.New(source, position, velocity, type, damage, knockback, player.whoAmI, preSpawnAction: delegate (Projectile p)
		{
			var clubProj = p.ModProjectile as BaseClubProj;
			float speedMult = player.GetTotalAttackSpeed(DamageClass.Melee);
			float swingSpeedMult = MathHelper.Lerp(speedMult, 1, 0.5f);

			float scale = player.GetAdjustedItemScale(Item);

			clubProj.SetStats(
				(int)(ChargeTime * MathHelper.Max(.15f, 2 - speedMult)),
				(int)(SwingTime * MathHelper.Max(.15f, 2 - swingSpeedMult)),
				DamageScaling,
				KnockbackScaling,
				scale);
		});

		return false;
	}

	public override void ModifyTooltips(List<TooltipLine> tooltips)
	{
		StatModifier meleeStat = Main.LocalPlayer.GetTotalDamage(DamageClass.Melee);

		foreach (TooltipLine line in tooltips)
			if (line.Mod == "Terraria" && line.Name == "Damage") //Replace the vanilla text with our own
				line.Text = $"{(int)meleeStat.ApplyTo(Item.damage)}-{(int)meleeStat.ApplyTo(Item.damage * DamageScaling)}" + Language.GetText("LegacyTooltip.2");
	}
}