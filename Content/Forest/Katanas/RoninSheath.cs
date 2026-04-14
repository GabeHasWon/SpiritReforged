using SpiritReforged.Common;
using SpiritReforged.Common.ItemCommon.Abstract;
using SpiritReforged.Common.PlayerCommon;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Forest.Katanas;

[AutoloadEquip(EquipType.Waist)]
public class RoninSheath : EquippableItem
{
	public const float Cooldown = 0.15f;
	public const float Damage = 0.2f;

	public sealed class SheathLayer : PlayerDrawLayer
	{
		public override Position GetDefaultPosition() => new BeforeParent(PlayerDrawLayers.HeldItem);

		public override bool GetDefaultVisibility(PlayerDrawSet drawInfo) => drawInfo.drawPlayer.HasEquip<RoninSheath>();

		protected override void Draw(ref PlayerDrawSet drawInfo) { } //Show swag
	}

	public sealed class SheathDamagePlayer : ModPlayer
	{
		public override void ModifyWeaponDamage(Item item, ref StatModifier damage)
		{
			if (Player.HasEquip<RoninSheath>() && SpiritSets.IsKatana[Player.HeldItem.type])
				damage *= Damage + 1;
		}
	}

	public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(Math.Round(Cooldown * 100), Math.Round(Damage * 100));

	public override void SetDefaults()
	{
		Item.width = 30;
		Item.height = 34;
		Item.value = Item.sellPrice(gold: 1);
		Item.rare = ItemRarityID.Blue;
		Item.accessory = true;
	}

	public override void UpdateAccessory(Player player, bool hideVisual) => player.GetModPlayer<DashSwordPlayer>().statDashCooldown *= Cooldown + 1;
}
