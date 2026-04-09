using SpiritReforged.Common.ItemCommon.Abstract;
using SpiritReforged.Common.PlayerCommon;
using SpiritReforged.Common.ProjectileCommon.Abstract;

namespace SpiritReforged.Content.Forest.Rapiers;

public class SharpeningStone : EquippableItem
{
	public sealed class SharpeningStonePlayer : ModPlayer
	{
		public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
		{
			if (Player.HasEquip<SharpeningStone>() && Player.HoldingProjectile(out Projectile held) && held.ModProjectile is RapierProjectile)
				modifiers.CritDamage *= 1.25f;
		}
	}

	public override void SetDefaults()
	{
		Item.width = 32;
		Item.height = 22;
		Item.value = Item.sellPrice(gold: 1);
		Item.rare = ItemRarityID.Blue;
		Item.accessory = true;
	}
}