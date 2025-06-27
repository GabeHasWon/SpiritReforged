using SpiritReforged.Common.PlayerCommon;
using System.Linq;

namespace SpiritReforged.Common.BuffCommon;

internal class BuffPlayer : ModPlayer
{
	/// <summary> Used specifically by <see cref="ModifyBuffTime"/>. </summary>
	/// <param name="buffType"> The type of buff being applied. </param>
	/// <param name="buffTime"> The duration this buff is being applied for. </param>
	/// <param name="player"> The player this buff is being applied to. </param>
	/// <param name="quickBuff"> Whether quick buff was used (<see cref="PlayerExtensions.UsedQuickBuff"/>). </param>
	public delegate void ModifyBuffTimeDelegate(int buffType, ref int buffTime, Player player, bool quickBuff);
	public delegate void QuickBuffDelegate(Player player);

	/// <summary> Allows you to dynamically modify buff times before they are applied. </summary>
	public static event ModifyBuffTimeDelegate ModifyBuffTime;
	public static event QuickBuffDelegate QuickBuff;

	/// <summary> Whether quick buff is being used. Only valid in specific methods, like <see cref="CombinedHooks.CanUseItem(Player, Item)"/>. </summary>
	public bool usedQuickBuff;

	public static bool CanHeal(Player player) => !player.cursed && !player.CCed && !player.dead && player.statLife != player.statLifeMax2 && player.potionDelay == 0;
	public static IEnumerable<Item> SortByPriority(IEnumerable<Item> list, Player player, bool healing = false)
	{
		if (healing)
		{
			return list.OrderBy(x => player.GetHealLife(x, true));
		}
		else
		{
			return list.OrderBy(x => FindFoodPriority(x.buffType) + x.buffTime);
		}
	}

	public static int FindFoodPriority(int type) => type switch
	{
		BuffID.WellFed2 => 1,
		BuffID.WellFed3 => 2,
		_ => 0
	}; //Mimics a private vanilla method

	public override void Load()
	{
		On_Player.AddBuff += BuffTime;
		On_Player.QuickBuff += TrackQuickBuff;
	}

	private static void TrackQuickBuff(On_Player.orig_QuickBuff orig, Player self)
	{
		self.GetModPlayer<BuffPlayer>().usedQuickBuff = true;
		QuickBuff?.Invoke(self);

		orig(self);

		self.GetModPlayer<BuffPlayer>().usedQuickBuff = false;
	}

	private static void BuffTime(On_Player.orig_AddBuff orig, Player self, int type, int timeToAdd, bool quiet, bool foodHack)
	{
		ModifyBuffTime.Invoke(type, ref timeToAdd, self, self.UsedQuickBuff());
		orig(self, type, timeToAdd, quiet, foodHack);
	}

	public override void Unload() => ModifyBuffTime = null;
}
