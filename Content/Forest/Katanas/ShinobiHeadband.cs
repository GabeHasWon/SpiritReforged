using SpiritReforged.Common.ItemCommon.Abstract;
using SpiritReforged.Common.PlayerCommon;

namespace SpiritReforged.Content.Forest.Katanas;

[AutoloadEquip(EquipType.Face)]
public class ShinobiHeadband : EquippableItem
{
	public sealed class ShinobiHeadbandPlayer : ModPlayer
	{
		private int _hitCount;

		public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (Player.HasEquip<ShinobiHeadband>() && proj.whoAmI == Player.heldProj && ++_hitCount >= 3)
			{
				_hitCount = 0;
				Player.GetModPlayer<DashSwordPlayer>().internalCooldown = 0;

				CombatText.NewText(Player.getRect(), Color.Yellow, "Refresh (Debug)");
			}
		}
	}

	public override void SetStaticDefaults() => ArmorIDs.Head.Sets.DrawFullHair[Item.headSlot] = true;

	public override void SetDefaults()
	{
		Item.width = 30;
		Item.height = 34;
		Item.value = Item.sellPrice(gold: 1);
		Item.rare = ItemRarityID.Blue;
		Item.accessory = true;
	}
}