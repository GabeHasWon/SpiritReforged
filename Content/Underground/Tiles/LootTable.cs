using SpiritReforged.Common.TileCommon.PresetTiles;
using SpiritReforged.Content.Underground.Pottery;
using Terraria.GameContent.ItemDropRules;

namespace SpiritReforged.Content.Underground.Tiles;

/// <summary> Facilitates a tile with an <see cref="ILoot"/> drop table.<br/>
/// Does not drop items automatically unless used alongside <see cref="PotTile"/>. See <see cref="LootTable.Resolve"/>. </summary>
public interface ILootTile
{
	public void AddLoot(int objectStyle, ILoot loot);
}

/// <summary> A self-contained loot table. </summary>
public readonly struct LootTable() : ILoot
{
	public readonly List<IItemDropRule> rules = [];

	public IItemDropRule Add(IItemDropRule entry)
	{
		rules.Add(entry);
		return entry;
	}

	public IItemDropRule Remove(IItemDropRule entry)
	{
		rules.Remove(entry);
		return entry;
	}

	public List<IItemDropRule> Get(bool includeGlobalDrops = true) => rules;
	public void RemoveWhere(Predicate<IItemDropRule> predicate, bool includeGlobalDrops = true)
	{
		foreach (var entry in Get())
		{
			if (predicate(entry))
				Remove(entry);
		}
	}

	public void Resolve(Rectangle position, Player player)
	{
		LootTableHandler.ForcedItemRegion = position;

		foreach (var rule in Get())
			LootTableHandler.ResolvePlayerRule(rule, player);

		LootTableHandler.ForcedItemRegion = Rectangle.Empty;
	}

	/// <summary> Calls <see cref="Resolve(Rectangle, Player)"/> using tile data from the given coordinates. </summary>
	public static bool Resolve(int i, int j, ushort type, int frameX, int frameY)
	{
		if (RecordHandler.ActionByType.TryGetValue(type, out var action))
		{
			Tile t = new(); //Fabricate a tile. If this method is called in KillMultiTile, the tile at (i, j) is unusable
			t.TileFrameX = (short)frameX;
			t.TileFrameY = (short)frameY;
			t.TileType = type;

			var data = TileObjectData.GetTileData(t); //data can be null here
			Point size = new(data?.Width ?? 2, data?.Height ?? 2);

			var loot = new LootTable();
			action.Invoke(TileObjectData.GetTileStyle(t), loot);

			var spawn = new Vector2(i, j).ToWorldCoordinates(size.X * 8, size.Y * 8);
			var p = Main.player[Player.FindClosest(spawn, 0, 0)];

			loot.Resolve(new Rectangle(i * 16, j * 16, size.X * 16, size.Y * 16), p);
			return true;
		}

		return false;
	}
}

public class LootTableHandler : ILoadable
{
	/// <summary> Forces <see cref="CommonCode.DropItem(DropAttemptInfo, int, int, bool)"/> to spawn within this area rather than a NPC/player hitbox (depending on <see cref="DropAttemptInfo"/>).</summary>
	internal static Rectangle ForcedItemRegion;

	public void Load(Mod mod) => On_CommonCode.DropItem_DropAttemptInfo_int_int_bool += ModifyCommonDrop;

	/// <summary> Grants control over position when spawning items with ILoot (prompted by <see cref="LootTable.Resolve"/>). </summary>
	private static void ModifyCommonDrop(On_CommonCode.orig_DropItem_DropAttemptInfo_int_int_bool orig, DropAttemptInfo info, int item, int stack, bool scattered)
	{
		if (ForcedItemRegion != Rectangle.Empty)
		{
			CommonCode.DropItem(ForcedItemRegion, info.player.GetSource_Misc("LootTable"), item, stack, scattered);
			return; //Skips orig
		}

		orig(info, item, stack, scattered);
	}

	public void Unload() { }

	public static ItemDropAttemptResult ResolvePlayerRule(IItemDropRule rule, Player player)
	{
		return ResolveRule(rule, GetInfo(player));

		static DropAttemptInfo GetInfo(Player player)
		{
			var info = default(DropAttemptInfo);
			info.player = player;
			info.npc = null;
			info.IsExpertMode = Main.expertMode;
			info.IsMasterMode = Main.masterMode;
			info.IsInSimulation = false;
			info.rng = Main.rand;

			return info;
		}
	}

	private static ItemDropAttemptResult ResolveRule(IItemDropRule rule, DropAttemptInfo info)
	{
		if (!rule.CanDrop(info))
		{
			ItemDropAttemptResult result = default;
			result.State = ItemDropAttemptResultState.DoesntFillConditions;

			ItemDropAttemptResult result2 = result;
			ResolveRuleChains(ref info, ref result2, rule.ChainedRules);

			return result2;
		}

		ItemDropAttemptResult result3 = (rule is not INestedItemDropRule nestedItemDropRule) ? rule.TryDroppingItem(info) : nestedItemDropRule.TryDroppingItem(info, ResolveRule);
		ResolveRuleChains(ref info, ref result3, rule.ChainedRules);

		return result3;
	}

	private static void ResolveRuleChains(ref DropAttemptInfo info, ref ItemDropAttemptResult parentResult, List<IItemDropRuleChainAttempt> ruleChains)
	{
		if (ruleChains == null)
			return;

		for (int i = 0; i < ruleChains.Count; i++)
		{
			IItemDropRuleChainAttempt chainAttempt = ruleChains[i];

			if (chainAttempt.CanChainIntoRule(parentResult))
				ResolveRule(chainAttempt.RuleToChain, info);
		}
	}
}