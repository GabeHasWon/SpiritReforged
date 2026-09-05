using SpiritReforged.Common.UI.BackpackInterface;
using SpiritReforged.Common.UI.Misc;
using SpiritReforged.Content.Aether.Items;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.ModLoader.IO;
using Terraria.UI;

namespace SpiritReforged.Common.ItemCommon.Backpacks;

public abstract class BackpackItem : ModItem
{
	protected override bool CloneNewInstances => true;

	public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(Items.Length);

	public Item[] Items
	{
		get
		{
			int count = slotCount + ((Main.LocalPlayer.TryGetModPlayer(out GlitterPurse.GlitterPursePlayer pursePlayer) && pursePlayer.usedGlitterPurse) ? GlitterPurse.SlotIncrease : 0);
			_items ??= Enumerable.Repeat(new Item(), count).ToArray();

			if (_items.Length < count) //Length has increased, resize the array and preserve contents
			{
				var preScale = (Item[])_items.Clone();
				_items = Enumerable.Repeat(new Item(), count).ToArray(); //Elongate the array

				for (int i = 0; i < _items.Length; i++)
				{
					if (i < preScale.Length)
						_items[i] = preScale[i].Clone();
				}
			}

			return _items;
		}
		set => _items = value;
	}

	private Item[] _items;

	/// <summary> The number slots this backpack has by default. </summary>
	protected int slotCount;

	public int UsedSlotCount(Player player) => slotCount 
		+ ((Main.LocalPlayer.TryGetModPlayer(out GlitterPurse.GlitterPursePlayer pursePlayer) && pursePlayer.usedGlitterPurse) ? GlitterPurse.SlotIncrease : 0);

	/// <summary>
	/// Solely used to make sure _items isn't copied between instances when spawned by the Hiker.
	/// </summary>
	internal void EnsureNewItemArray(Player player)
	{
		int length = _items?.Length ?? UsedSlotCount(player);
		_items = new Item[length];
		
		for (int i = 0; i < length; ++i)
		{
			_items[i] = new Item(0);
			_items[i].TurnToAir();
		}
	}

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

		Item oldPack = player.GetModPlayer<BackpackPlayer>().backpack;

		player.GetModPlayer<BackpackPlayer>().backpack = Item.Clone();
		Item.SetDefaults(oldPack.type);
	}

	public override void NetSend(BinaryWriter writer)
	{
		writer.Write((byte)Items.Length); //Write the length of the array

		foreach (Item item in Items)
			ItemIO.Send(item, writer, true);
	}

	public override void NetReceive(BinaryReader reader)
	{
		int length = reader.ReadByte(); //Read the length of the array
		List<Item> items = [];

		for (int i = 0; i < length; i++)
			items.Add(ItemIO.Receive(reader, true));

		Items = items.ToArray();
	}

	public override void SaveData(TagCompound tag)
	{
		TagCompound packCompound = [];

		for (int i = 0; i < Items.Length; i++)
		{
			Item item = Items[i];

			if (item?.IsAir == false) //Don't bother saving air
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
			List<Item> loaded = [];
			foreach (var item in packCompound)
			{
				if (packCompound.TryGet(item.Key, out TagCompound value))
				{
					int index = int.Parse(item.Key[item.Key.Length - 1].ToString()); //The last value in the key is always an integer corresponding to the slot

					while (index > loaded.Count)
						loaded.Add(new()); //Fill the empty space to ensure index is consistent

					loaded.Add(ItemIO.Load(value));
				}
			}

			Items = loaded.ToArray(); //Load all items regardless of normal slot limit
		}
	}
}