using System.Linq;
using Terraria.GameContent.ItemDropRules;

namespace SpiritReforged.Common.ItemCommon;

public class DropRules
{
	/// <summary>Wrapper for having two droprates with stacks on a Common ItemDropRule.</summary>
	public static IItemDropRule NormalvsExpertStacked(int itemID, int normal, int expert, int minStack, int maxStack)
	{
		var rule = new DropBasedOnExpertMode(ItemDropRule.Common(itemID, normal, minStack, maxStack), ItemDropRule.Common(itemID, expert, minStack, maxStack));
		return rule;
	}

	/// <summary>
	/// Taken from the same implementation in Verdant.
	/// </summary>
	/// <remarks> Like a OneFromOptions, but you can specify the stacks of each item. </remarks>
	/// <param name="dropCount">How many items are dropped.</param>
	/// <param name="stacks">Stack range and item type.</param>
	/// <exception cref="ArgumentOutOfRangeException"></exception>
	public class LootPoolDrop(int dropCount, int chanceDenominator, int chanceNumerator, params LootPoolDrop.StackPool[] stacks) : IItemDropRule
	{
		public readonly record struct StackPool(int ItemType, Range Stack);

		public List<IItemDropRuleChainAttempt> ChainedRules { get; private set; } = [];

		private readonly StackPool[] _stacks = stacks;

		public int dropCount = dropCount;
		public int chanceDenominator = chanceDenominator;
		public int chanceNumerator = chanceNumerator;

		/// <summary> Like a OneFromOptions, but you can specify the stacks of all items. </summary>
		/// <param name="maxStack">Max stack of the dropped item, INCLUSIVE.</param>
		/// <param name="dropCount">How many items are dropped.</param>
		/// <param name="options">Item IDs to choose from.</param>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		public static LootPoolDrop SameStack(int dropCount, int minStack, int maxStack, int denominator, int numerator, params int[] options)
		{
			var stackPool = new StackPool[options.Length];

			for (int i = 0; i < stackPool.Length; i++)
				stackPool[i] = new(options[i], minStack..maxStack);

			return new LootPoolDrop(dropCount, denominator, numerator, stackPool);
		}

		public ItemDropAttemptResult TryDroppingItem(DropAttemptInfo info)
		{
			ItemDropAttemptResult result = default;

			if (info.player.RollLuck(chanceDenominator) < chanceNumerator)
			{
				var savedStacks = _stacks.ToList();

				int count = 0;
				int index = info.rng.Next(savedStacks.Count);

				CommonCode.DropItem(info, savedStacks[index].ItemType, info.rng.Next(savedStacks[index].Stack.Start.Value, savedStacks[index].Stack.End.Value + 1));
				savedStacks.RemoveAt(index);

				while (++count < dropCount)
				{
					int index2 = info.rng.Next(savedStacks.Count);
					CommonCode.DropItem(info, savedStacks[index2].ItemType, info.rng.Next(savedStacks[index2].Stack.Start.Value, savedStacks[index2].Stack.End.Value + 1));

					savedStacks.RemoveAt(index2);
				}

				result.State = ItemDropAttemptResultState.Success;
				return result;
			}

			result.State = ItemDropAttemptResultState.FailedRandomRoll;
			return result;
		}

		public void ReportDroprates(List<DropRateInfo> drops, DropRateInfoChainFeed ratesInfo)
		{
			float baseChance = chanceNumerator / chanceDenominator;
			float realChance = baseChance * ratesInfo.parentDroprateChance;
			float dropRate = 1f / (_stacks.Length - dropCount + 1) * realChance;

			foreach (var stack in _stacks)
				drops.Add(new DropRateInfo(stack.ItemType, stack.Stack.Start.Value, stack.Stack.End.Value, dropRate, ratesInfo.conditions));

			Chains.ReportDroprates(ChainedRules, baseChance, drops, ratesInfo);
		}

		public bool CanDrop(DropAttemptInfo info) => true;
	}
}

public class DropConditions
{
	public class Dynamic(Func<bool> condition, string locKey, bool canShowInUI = true) : IItemDropRuleCondition, IProvideItemConditionDescription
	{
		public bool CanDrop(DropAttemptInfo info) => condition.Invoke();
		public bool CanShowItemDropInUI() => canShowInUI;
		public string GetConditionDescription() => Language.GetTextValue(locKey);
	}

	public class Standard(Condition condition, bool canShowInUI = true) : IItemDropRuleCondition, IProvideItemConditionDescription
	{
		public bool CanDrop(DropAttemptInfo info) => condition.IsMet();
		public bool CanShowItemDropInUI() => canShowInUI;
		public string GetConditionDescription() => Language.GetTextValue(condition.Description.Key);
	}
}