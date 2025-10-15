using SpiritReforged.Common.PlayerCommon;

namespace SpiritReforged.Common.ItemCommon.Abstract;

/// <summary>Automatically provides equip flags for items in <see cref="EquipsPlayer.equips"/>.
/// <br/>See <see cref="PlayerCommon.PlayerExtensions"/> for additional helpers. </summary>
public abstract class EquippableItem : ModItem
{
	public sealed override void UpdateEquip(Player player)
	{
		player.GetModPlayer<PlayerFlags>().SetFlag(Name);
		UpdateEquippable(player);
	}

	public virtual void UpdateEquippable(Player player) { }
}