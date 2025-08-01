using Terraria.DataStructures;

namespace SpiritReforged.Common.PlayerCommon;

public class PlayerEvents : ModPlayer
{
	public delegate void AnglerQuestDelegate(Player player, float rareMultiplier, List<Item> rewardItems);

	public static event Action<Player> OnKill;
	public static event AnglerQuestDelegate OnAnglerQuestReward;
	public static event Action<Player> OnPostUpdateRunSpeeds;

	/// <summary> Subscribes to <see cref="OnAnglerQuestReward"/> with reward chances based on the provided info. </summary>
	/// <param name="item"> The item to add as a reward. </param>
	/// <param name="chance"> The chance denominator. </param>
	/// <param name="rarityInfluence"> How much the internal rarity modifier affects <paramref name="chance"/>. Defaults to 1, which gives the modifer full influence. </param>
	public static void AddAnglerQuestReward(Item item, int chance, float rarityInfluence = 1f) => OnAnglerQuestReward += (player, rareMultiplier, rewardItems) =>
	{
		if (Main.rand.NextBool((int)Math.Max(chance * MathHelper.Lerp(1, rareMultiplier, rarityInfluence), 1)))
			rewardItems.Add(item);
	};
	/// <inheritdoc cref="AddAnglerQuestReward(Item, int, float)"/>
	/// <param name="itemType"> The item type to add as a reward. </param>
	public static void AddAnglerQuestReward(int itemType, int chance, float rarityInfluence = 1f) => AddAnglerQuestReward(new Item(itemType), chance, rarityInfluence);

	public override void Kill(double damage, int hitDirection, bool pvp, PlayerDeathReason damageSource) => OnKill?.Invoke(Player);
	public override void AnglerQuestReward(float rareMultiplier, List<Item> rewardItems) => OnAnglerQuestReward?.Invoke(Player, rareMultiplier, rewardItems);
	public override void PostUpdateRunSpeeds() => OnPostUpdateRunSpeeds?.Invoke(Player);
}