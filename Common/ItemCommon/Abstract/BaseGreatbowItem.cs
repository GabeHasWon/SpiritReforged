using SpiritReforged.Common.Misc;
using SpiritReforged.Common.ProjectileCommon;
using SpiritReforged.Common.ProjectileCommon.Abstract;
using Terraria.DataStructures;

namespace SpiritReforged.Common.ItemCommon.Abstract;

public abstract class BaseGreatbowItem : ModItem
{
	private static int ProjType;

	internal virtual float ChargeScaling => 2;
	internal virtual float PerfectShotScaling => 1.33f;
	internal virtual int PerfectShotWindow => 40;

	public override void SetStaticDefaults()
	{
		TryFindHeldProjectile(out ModProjectile shoot);
		if (shoot != null)
			ProjType = shoot.Type;

		SafeSetStaticDefaults();
	}

	public override void SetDefaults()
	{
		Item.useTime = Item.useAnimation = 60;
		Item.knockBack = 1f;
		Item.noMelee = true;
		Item.channel = true;
		Item.noUseGraphic = true;
		Item.DamageType = DamageClass.Ranged;
		Item.useTurn = false;
		Item.autoReuse = false;
		Item.useStyle = ItemUseStyleID.Shoot;
		Item.shootSpeed = 10;
		Item.useAmmo = AmmoID.Arrow;
		Item.shoot = ProjType;

		SafeSetDefaults();
	}

	private void TryFindHeldProjectile(out ModProjectile shoot)
	{
		string filePath = Name;
		if (filePath.Contains("Item"))
			filePath = filePath[..^4];

		ContentUtils.TryFindFromArray(Mod.Name, filePath, ["Held", "held", "Proj", "proj", "Projectile", "projectile"], out ModProjectile projectile);
		shoot = projectile;
	}

	internal virtual void SafeSetStaticDefaults() { }

	internal abstract void SafeSetDefaults();

	public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
	{
		int chargeTime = (int)(Item.useTime / player.GetTotalAttackSpeed(DamageClass.Ranged));

		PreNewProjectile.New(source, position, Vector2.Zero, Item.shoot, damage, knockback, player.whoAmI, 0, chargeTime, type, preSpawnAction: delegate (Projectile p)
		{
			var bowProj = p.ModProjectile as BaseChargeBow;

			bowProj.SetStats(
				ChargeScaling,
				PerfectShotScaling,
				PerfectShotWindow);
		});

		return false;
	}

	public override bool CanUseItem(Player player) => player.ownedProjectileCounts[Item.shoot] == 0;

	public override void ModifyTooltips(List<TooltipLine> tooltips)
	{
		StatModifier rangedStat = Main.LocalPlayer.GetTotalDamage(DamageClass.Ranged);
		int damageIndex = 0;

		foreach (TooltipLine line in tooltips)
		{
			damageIndex++;
			if (line.Mod == "Terraria" && line.Name == "Damage") //Replace the vanilla text with our own
			{
				line.Text = $"{(int)rangedStat.ApplyTo(Item.damage)}-{(int)rangedStat.ApplyTo(Item.damage * ChargeScaling)}" + Language.GetText("LegacyTooltip.3");
				break;
			}
		}

		string perfectShotText = $"{(int)rangedStat.ApplyTo(Item.damage * ChargeScaling * PerfectShotScaling)}" 
			+ Language.GetText("LegacyTooltip.3") 
			+ Mod.GetLocalization("Items.TooltipExtras.GreatbowDamage");

		tooltips.Insert(damageIndex, new TooltipLine(SpiritReforgedMod.Instance, "SpiritReforged:PerfectShot", perfectShotText) { });
	}
}
