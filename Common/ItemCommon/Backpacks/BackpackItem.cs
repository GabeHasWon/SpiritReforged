using SpiritReforged.Common.UI.BackpackInterface;
using SpiritReforged.Common.UI.Misc;
using SpiritReforged.Content.Aether.Items;
using System.IO;
using System.Linq;
using Terraria.ModLoader.IO;
using Terraria.UI;

namespace SpiritReforged.Common.ItemCommon.Backpacks;

public abstract class BackpackItem : ModItem
{
	protected override bool CloneNewInstances => true;

	public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(SlotCount);

	/// <summary> The absolute number of slots this backpack has. </summary> //Check gameMenu to assist with LoadData
	public int SlotCount => slotCount + ((Main.gameMenu || Main.LocalPlayer.TryGetModPlayer(out GlitterPurse.GlitterPursePlayer pursePlayer) && pursePlayer.usedGlitterPurse) ? GlitterPurse.SlotIncrease : 0);

	public Item[] Items
	{
		get
		{
			_items ??= Enumerable.Repeat(new Item(), SlotCount).ToArray();

			if (_items.Length != SlotCount) //SlotCount has changed, readjust the array and preserve contents
			{
				var preScale = (Item[])_items.Clone();
				_items = Enumerable.Repeat(new Item(), SlotCount).ToArray();

				for (int i = 0; i < _items.Length; i++)
				{
					if (i < preScale.Length)
						_items[i] = preScale[i].Clone();
				}
			}

			return _items;
		}
	}

	private Item[] _items;
	/// <summary> The number slots this backpack has by default. </summary>
	protected int slotCount;

	public override ModItem Clone(Item newEntity)
	{
		ModItem clone = base.Clone(newEntity);
		(clone as BackpackItem)._items = _items;
		(clone as BackpackItem).slotCount = slotCount;

		return clone;
	}

	public override void SetStaticDefaults() => ItemID.Sets.ShimmerTransformToItem[Type] = ModContent.ItemType<GlitterPurse>();

	public override bool CanRightClick() => true;
	public override bool ConsumeItem(Player player) => false; //Prevent RightClick from destroying the item

	/// <summary> Controls which <see cref="BasicItemSlot"/>s are added by this backpack as UI elements. </summary>
	/// <param name="number"> the index of <see cref="Items"/>. </param>
	/// <param name="position"> the default position of this element. </param>
	public virtual BasicItemSlot SetupSlot(int number, Vector2 position)
	{
		var pixelDimension = StyleDimension.FromPixels(32);
		return new PackInventorySlot(Items, number)
		{
			Left = new StyleDimension(position.X, 0),
			Top = new StyleDimension(position.Y, 0),
			Width = pixelDimension,
			Height = pixelDimension
		};
	}

	public override void RightClick(Player player) //Attempt to swap this backpack into the backpack slot
	{
		if (!BackpackUISlot.CanClickItem(player.GetModPlayer<BackpackPlayer>().backpack))
			return;

		var oldPack = player.GetModPlayer<BackpackPlayer>().backpack;

		player.GetModPlayer<BackpackPlayer>().backpack = Item.Clone();
		Item.SetDefaults(oldPack.type);
	}

	public override void NetSend(BinaryWriter writer)
	{
		foreach (var item in Items)
			ItemIO.Send(item, writer, true);
	}

	public override void NetReceive(BinaryReader reader)
	{
		foreach (var item in Items)
			ItemIO.Receive(item, reader, true);
	}

	public override void SaveData(TagCompound tag)
	{
		TagCompound packCompound = [];

		for (int i = 0; i < Items.Length; i++)
		{
			Item item = Items[i];

			if (item != null && !item.IsAir) //Don't bother saving air
				packCompound["item" + i] = ItemIO.Save(item);
		}

		tag["packContents"] = packCompound;
	}

	public override void LoadData(TagCompound tag)
	{
		TagCompound packCompound = tag.GetCompound("packContents");

		if (packCompound.Count == 0) //Legacy loading
		{
			for (int i = 0; i < Items.Length; i++)
			{
				if (tag.TryGet("item" + i, out TagCompound itemTag))
					Items[i] = ItemIO.Load(itemTag);
			}
		}
		else //New loading
		{
			foreach (var item in packCompound)
			{
				if (packCompound.TryGet(item.Key, out TagCompound value))
				{
					int index = int.Parse(item.Key[item.Key.Length - 1].ToString()); //The last value in the key is always an integer corresponding to the slot
					
					if (index < Items.Length)
						Items[index] = ItemIO.Load(value);
				}
			}
		}
	}
}