﻿namespace SpiritReforged.Common.ItemCommon;

public abstract class MinionAccessory : AccessoryItem
{
	public override bool CanAccessoryBeEquippedWith(Item equippedItem, Item incomingItem, Player player) => true;// !(equippedItem.ModItem is MinionAccessory && incomingItem.ModItem is MinionAccessory);
}
