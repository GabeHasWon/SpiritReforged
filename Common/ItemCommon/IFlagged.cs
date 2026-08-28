using SpiritReforged.Common.PlayerCommon;

namespace SpiritReforged.Common.ItemCommon;

/// <summary>Automatically provides equip flags for items within <see cref="PlayerFlags"/>.
/// <br/>See <see cref="PlayerExtensions"/> for additional helpers. </summary>
public interface IFlagged
{
	public sealed class EquipFlagGlobalItem : GlobalItem
	{
		public override void UpdateEquip(Item item, Player player)
		{
			if (item.ModItem is IFlagged)
				player.GetModPlayer<PlayerFlags>().SetFlag(item.ModItem.Name);
		}
	}
}