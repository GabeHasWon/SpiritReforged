using SpiritReforged.Common;
using SpiritReforged.Common.ModCompat;
using SpiritReforged.Common.ProjectileCommon.Abstract;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Forest.Rapiers;

public class EstocSabreDuo : ModItem
{
	public override void SetStaticDefaults()
	{
		SpiritSets.IsSword[Type] = true;
		ItemID.Sets.ItemsThatAllowRepeatedRightClick[Type] = true;
	}

	public override void SetDefaults()
	{
		Item.DefaultToSpear(ModContent.ProjectileType<SilverEstoc.SilverEstocSwing>(), 1f, 18);
		Item.SetShopValues(ItemRarityColor.Blue1, Item.sellPrice(silver: 50));
		Item.damage = 14;
		Item.knockBack = 4;
		Item.UseSound = RapierProjectile.DefaultSwing;
		Item.autoReuse = true;
		MoRHelper.SetSlashBonus(Item);
	}

	public override bool AltFunctionUse(Player player) => true;

	public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
	{
		if (player.altFunctionUse == 2)
			SwungProjectile.Spawn(position, velocity, ModContent.ProjectileType<TungstenSabre.TungstenSabreSwing>(), damage, knockback, player, Main.rand.NextFromList(-5, 5), source, (int)SilverEstoc.SilverEstocSwing.MoveType.Swing);
		else
			SwungProjectile.Spawn(position, velocity, type, damage, knockback * 0.2f, player, 0, source, (int)SilverEstoc.SilverEstocSwing.MoveType.Lunge);

		return false;
	}

	public override void ModifyTooltips(List<TooltipLine> tooltips) => tooltips.RemoveAll(static x => x.Mod == "Terraria" && x.Name == "CritChance"); //Remove the line indicating crit chance
}