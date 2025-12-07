using SpiritReforged.Common.UI.BackpackInterface;
using System.IO;
using Terraria.ModLoader.IO;

namespace SpiritReforged.Common.ItemCommon.Backpacks;

public abstract class BackpackItem : ModItem
{
	protected override bool CloneNewInstances => true;

	public Item[] items;

	/// <summary> How many slots this backpack has. </summary>
	protected abstract int SlotCap { get; }

	public override ModItem Clone(Item newEntity)
	{
		ModItem clone = base.Clone(newEntity);
		(clone as BackpackItem).items = items;
		return clone;
	}

	public sealed override void SetDefaults()
	{
		Defaults();

		if (items is null)
		{
			items = new Item[SlotCap];

			for (int i = 0; i < SlotCap; i++)
				items[i] = new Item();
		}
	}

	public virtual void Defaults() { }
	public override bool CanRightClick() => true;
	public override bool ConsumeItem(Player player) => false; //Prevent RightClick from destroying the item

	public override void RightClick(Player player) //Attempt to swap this backpack into the backpack slot
	{
		if (!BackpackUISlot.CanClickItem(player.GetModPlayer<BackpackPlayer>().backpack))
			return;

		var oldPack = player.GetModPlayer<BackpackPlayer>().backpack;

		player.GetModPlayer<BackpackPlayer>().backpack = Item.Clone();
		Item.SetDefaults(oldPack.type);
	}

	public override void SaveData(TagCompound tag)
	{
		for (int i = 0; i < items.Length; i++)
		{
			if (items[i] is not null && !items[i].IsAir) //Don't bother saving air
				tag.Add("item" + i, ItemIO.Save(items[i]));
		}
	}

	public override void LoadData(TagCompound tag)
	{
		items = new Item[SlotCap];

		for (int i = 0; i < items.Length; i++)
		{
			if (tag.TryGet("item" + i, out TagCompound itemTag)) //All entries of 'items' are currently null. Avoid a null check, or we won't get our data
				items[i] = ItemIO.Load(itemTag);
			else
				items[i] = new Item();
		}
	}

	public override void NetSend(BinaryWriter writer)
	{
		foreach (var item in items)
			ItemIO.Send(item, writer, true);
	}

	public override void NetReceive(BinaryReader reader)
	{
		foreach (var item in items)
			ItemIO.Receive(item, reader, true);
	}
}
