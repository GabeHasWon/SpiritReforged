using SpiritReforged.Common.BuffCommon;
using SpiritReforged.Common.Multiplayer;
using System.IO;
using System.Linq;
using Terraria.ModLoader.IO;

namespace SpiritReforged.Common.ItemCommon.Backpacks;

internal class BackpackPlayer : ModPlayer
{
	[Flags]
	private enum BackpackState
	{
		None = 0,
		HasBackpack = 1,
		HasVanity = 2,
		HasDye = 4
	}

	public Item backpack = new();
	public Item vanityBackpack = new();
	public Item packDye = new();
	public bool packVisible = true;

	private int _lastSelectedEquipPage = 0;
	private bool _hadBackpack = false;
	private BackpackState _state = BackpackState.None;
	private BackpackState _oldState = BackpackState.None;

	#region detours
	public override void Load()
	{
		On_Player.QuickBuff_PickBestFoodItem += SelectFoodFromPack;
		On_Player.QuickHeal_GetItemToUse += SelectHealingFromPack;
	}

	private static Item SelectFoodFromPack(On_Player.orig_QuickBuff_PickBestFoodItem orig, Player self)
	{
		var result = orig(self);

		if (self.TryGetModPlayer(out BackpackPlayer p) && p.backpack.ModItem is BackpackItem i)
		{
			Item item = BuffPlayer.SortByPriority(i.items, self).LastOrDefault();

			if (!item.IsAir && item.buffTime > 0 && item.buffType != 0 && !HasBetterFoodBuff(self, item.buffType, item.buffTime))
				return item;
		}

		return result;

		static bool HasBetterFoodBuff(Player p, int compareType, int compareTime)
		{
			int bestTime = 0;
			int bestPriority = 0;

			for (int i = 0; i < Player.MaxBuffs; i++)
			{
				if (p.buffTime[i] > 0 && BuffPlayer.FindFoodPriority(p.buffType[i]) is int newPriority && newPriority > bestPriority && p.buffTime[i] > bestTime)
				{
					bestTime = p.buffTime[i] + 1;
					bestPriority = newPriority;
				}
			}

			return bestPriority >= BuffPlayer.FindFoodPriority(compareType) || bestTime > compareTime;
		}
	}

	private static Item SelectHealingFromPack(On_Player.orig_QuickHeal_GetItemToUse orig, Player self)
	{
		var result = orig(self);

		if (self.TryGetModPlayer(out BackpackPlayer p) && p.backpack.ModItem is BackpackItem i)
		{
			Item item = BuffPlayer.SortByPriority(i.items, self, true).LastOrDefault();

			if (!item.IsAir && item.potion && item.healLife > 0)
				return item;
		}

		return result;
	}
	#endregion

	public override void UpdateEquips()
	{
		if (Player.HeldItem.ModItem is BackpackItem) //Open the equip menu when a backpack is picked up
		{
			if (!_hadBackpack)
				_lastSelectedEquipPage = Main.EquipPageSelected;

			Main.EquipPageSelected = 2;
		}
		else if (_hadBackpack)
		{
			Main.EquipPageSelected = _lastSelectedEquipPage;
		}

		_hadBackpack = Player.HeldItem.ModItem is BackpackItem;

		if (backpack.ModItem is BackpackItem bp) //Update backpack contents as though they were in the inventory
		{
			foreach (var item in bp.items)
			{
				ItemLoader.UpdateInventory(item, Player);
				Player.RefreshInfoAccsFromItemType(item);
				Player.RefreshMechanicalAccsFromItemType(item.type);
			}
		}

		if (Main.netMode == NetmodeID.MultiplayerClient)
		{
			// Track current & old state so we can sync as needed

			_oldState = _state;
			_state = BackpackState.None;

			if (backpack != null && !backpack.IsAir)
				_state |= BackpackState.HasBackpack;

			if (vanityBackpack != null && !vanityBackpack.IsAir)
				_state |= BackpackState.HasVanity;

			if (packDye != null && !packDye.IsAir)
				_state |= BackpackState.HasDye;

			if (_oldState != _state)
				new BackpackPlayerData(packVisible, (byte)Player.whoAmI).Send();
		}
	}

	public override void FrameEffects() //This way, players can be seen wearing backpacks in the selection screen
	{
		if (vanityBackpack != null && !vanityBackpack.IsAir)
			ApplyEquip(vanityBackpack);
		else if (backpack != null && !backpack.IsAir && packVisible)
			ApplyEquip(backpack);
	}

	private void ApplyEquip(Item backpack)
	{
		Player.back = EquipLoader.GetEquipSlot(Mod, backpack.ModItem.Name, EquipType.Back);
		Player.front = EquipLoader.GetEquipSlot(Mod, backpack.ModItem.Name, EquipType.Front);

		if (!packDye.IsAir) 
		{
			Player.cBack = packDye.dye;
			Player.cFront = packDye.dye;
		}
	}

	public override void SaveData(TagCompound tag)
	{
		if (backpack is not null)
			tag.Add("backpack", ItemIO.Save(backpack));

		if (vanityBackpack is not null)
			tag.Add("vanity", ItemIO.Save(vanityBackpack));
		
		if (packDye is not null)
			tag.Add("dye", ItemIO.Save(packDye));

		tag.Add(nameof(packVisible), packVisible);
	}

	public override void LoadData(TagCompound tag)
	{
		if (tag.TryGet("backpack", out TagCompound item))
			backpack = ItemIO.Load(item);

		if (tag.TryGet("vanity", out TagCompound vanity))
			vanityBackpack = ItemIO.Load(vanity);

		if (tag.TryGet("dye", out TagCompound dye))
			packDye = ItemIO.Load(dye);

		packVisible = tag.Get<bool>(nameof(packVisible));
	}

	public override void SyncPlayer(int toWho, int fromWho, bool newPlayer) => new BackpackPlayerData(packVisible, (byte)Player.whoAmI).Send();
}

internal class BackpackPlayerData : PacketData
{
	private readonly bool _visibility;
	private readonly byte _playerIndex;

	public BackpackPlayerData() { }
	public BackpackPlayerData(bool value, byte playerIndex)
	{
		_visibility = value;
		_playerIndex = playerIndex;
	}

	public override void OnReceive(BinaryReader reader, int whoAmI)
	{
		bool visibility = reader.ReadBoolean();
		byte who = reader.ReadByte();

		Item backpack = ItemIO.Receive(reader);
		Item vanity = ItemIO.Receive(reader);
		Item dye = ItemIO.Receive(reader);

		Player player = Main.player[who];
		player.GetModPlayer<BackpackPlayer>().packVisible = visibility;

		if (Main.myPlayer != who) // Don't override backpack on player who sent the packet
		{
			player.GetModPlayer<BackpackPlayer>().backpack = backpack;
			player.GetModPlayer<BackpackPlayer>().vanityBackpack = vanity;
			player.GetModPlayer<BackpackPlayer>().packDye = dye;
		}

		if (Main.netMode == NetmodeID.Server)
			new BackpackPlayerData(visibility, who).Send(ignoreClient: who);
	}

	public override void OnSend(ModPacket modPacket)
	{
		modPacket.Write(_visibility);
		modPacket.Write(_playerIndex);

		BackpackPlayer player = Main.player[_playerIndex].GetModPlayer<BackpackPlayer>();
		ItemIO.Send(player.backpack, modPacket);
		ItemIO.Send(player.vanityBackpack, modPacket);
		ItemIO.Send(player.packDye, modPacket);
	}
}
