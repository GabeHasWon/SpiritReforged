﻿using System.Linq;
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
	public class LootPoolDrop : IItemDropRule
	{
		public List<IItemDropRuleChainAttempt> ChainedRules { get; private set; }

		private readonly Range[] stacks;

		public int[] dropIds;
		public int chanceDenominator;
		public int chanceNumerator;
		public int amount;

		/// <summary>
		/// Like a OneFromOptions, but you can specify the stacks of each item.
		/// </summary>
		/// <param name="stacks">Stack range (inclusive) of the dropped item.</param>
		/// <param name="amount">How many items are dropped.</param>
		/// <param name="options">Item IDs to choose from.</param>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		public LootPoolDrop(Range[] stacks, int amount, int chanceDenominator, int chanceNumerator, params int[] options)
		{
			if (amount > options.Length)
				throw new ArgumentOutOfRangeException(nameof(amount), "amount must be less than the number of options");

			this.amount = amount;
			this.chanceDenominator = chanceDenominator;
			this.stacks = stacks;
			this.chanceNumerator = chanceNumerator;

			dropIds = options;
			ChainedRules = [];
		}

		/// <summary>
		/// Like a OneFromOptions, but you can specify the stacks of all items.
		/// </summary>
		/// <param name="maxStack">Max stack of the dropped item, INCLUSIVE.</param>
		/// <param name="amount">How many items are dropped.</param>
		/// <param name="options">Item IDs to choose from.</param>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		public static LootPoolDrop SameStack(int minStack, int maxStack, int amount, int denominator, int numerator, params int[] options)
		{
			var stacks = new Range[options.Length];

			for (int i = 0; i < options.Length; ++i)
				stacks[i] = minStack..maxStack;

			return new LootPoolDrop(stacks, amount, denominator, numerator, options);
		}

		public ItemDropAttemptResult TryDroppingItem(DropAttemptInfo info)
		{
			ItemDropAttemptResult result = default;

			if (info.player.RollLuck(chanceDenominator) < chanceNumerator)
			{
				var savedDropIds = dropIds.ToList();
				var savedStacks = stacks.ToList();

				int count = 0;
				int index = info.rng.Next(savedDropIds.Count);

				CommonCode.DropItem(info, savedDropIds[index], info.rng.Next(savedStacks[index].Start.Value, savedStacks[index].End.Value + 1));
				savedDropIds.RemoveAt(index);
				savedStacks.RemoveAt(index);

				while (++count < amount)
				{
					int index2 = info.rng.Next(savedDropIds.Count);
					CommonCode.DropItem(info, savedDropIds[index2], info.rng.Next(savedStacks[index2].Start.Value, savedStacks[index2].End.Value + 1));

					savedDropIds.RemoveAt(index2);
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
			float dropRate = 1f / (dropIds.Length - amount + 1) * realChance;

			for (int i = 0; i < dropIds.Length; i++)
				drops.Add(new DropRateInfo(dropIds[i], stacks[i].Start.Value, stacks[i].End.Value, dropRate, ratesInfo.conditions));

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