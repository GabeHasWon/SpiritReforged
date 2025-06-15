namespace SpiritReforged.Common.ItemCommon.Abstract;

/// <summary>Automatically provides equip flags for items in <see cref="ItemEquipPlayer.equips"/>.
/// <br/>See <see cref="PlayerCommon.PlayerExtensions"/> for additional helpers. </summary>
public abstract class EquippableItem : ModItem
{
	public sealed override void UpdateEquip(Player player)
	{
		player.GetModPlayer<ItemEquipPlayer>().equips[Name] = true;
		UpdateEquippable(player);
	}

	public virtual void UpdateEquippable(Player player) { }
}

public class ItemEquipPlayer : ModPlayer
{
	/// <summary> Tracks the internal names of <see cref="EquippableItem"/>s and whether they are currently equipped. </summary>
	public readonly Dictionary<string, bool> equips = [];

	public override void Initialize()
	{
		equips.Clear();

		foreach (var item in ModContent.GetContent<EquippableItem>())
			equips.Add(item.Name, false);
	}

	public override void ResetEffects()
	{
		foreach (string item in equips.Keys)
			equips[item] = false;
	}

	public override void UpdateDead() => ResetEffects();
}