using SpiritReforged.Common.ItemCommon.Abstract;
using SpiritReforged.Common.PlayerCommon;
using SpiritReforged.Common.ProjectileCommon.Abstract;

namespace SpiritReforged.Content.Forest.Rapiers;

public class SharpeningStone : EquippableItem
{
	public sealed class SweetspotBonusPlayer : ModPlayer
	{
		public float sweetspotAdditive;

		public static bool HoldingRapier(Player player) => player.HoldingProjectile(out Projectile held) && held.ModProjectile is RapierProjectile;

		public override void ResetEffects() => sweetspotAdditive = 0;

		public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
		{
			if (sweetspotAdditive > 0 && HoldingRapier(Player))
				modifiers.CritDamage += sweetspotAdditive;
		}
	}

	public const float CRIT_BONUS = 0.1f;

	public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(Math.Round(CRIT_BONUS * 100));

	public override void SetDefaults()
	{
		Item.width = 32;
		Item.height = 22;
		Item.value = Item.sellPrice(silver: 80);
		Item.rare = ItemRarityID.Blue;
		Item.accessory = true;
	}

	public override void UpdateAccessory(Player player, bool hideVisual)
	{
		if (player.TryGetModPlayer(out SweetspotBonusPlayer bonusPlayer))
			bonusPlayer.sweetspotAdditive += CRIT_BONUS;
	}
}