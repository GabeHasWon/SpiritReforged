using System.Linq;

namespace SpiritReforged.Common.BuffCommon.Stacking;

/// <summary> A stacking buff definition designed for NPCs. </summary>
public abstract class StackingBuff : ModType
{
	#region handler
	/// <summary> Creates a new instance from template T. </summary>
	public static StackingBuff NewBuff<T>() where T : StackingBuff => NewBuff(typeof(T).Name);
	/// <summary> Creates a new instance from template <paramref name="name"/>. </summary>
	public static StackingBuff NewBuff(string name) => Loaded[name].MemberwiseClone() as StackingBuff;

	/// <summary> All <see cref="StackingBuff"/> instances created during load. Should not be modified. </summary>
	private static readonly Dictionary<string, StackingBuff> Loaded = [];
	#endregion

	public byte MaxStacks { get; protected set; } = 1;
	public byte stacks;
	public int duration;

	protected sealed override void Register() { }

	public sealed override void Load()
	{
		Loaded.Add(Name, this);
		Load(Mod);
	}

	public virtual void Load(Mod mod) { }
	/// <summary> Called whenever this buff is added to an NPC. Can also be used to set defaults. </summary>
	public virtual void OnAdded() { }
	/// <summary> Called whenever this buff is removed from an NPC. </summary>
	public virtual void OnRemoved(bool timedOut) { }
	public virtual void UpdateEffects(NPC npc) { }
}

internal class StackingNPC : GlobalNPC
{
	public override bool InstancePerEntity => true;

	private readonly HashSet<StackingBuff> stackingBuffs = [];

	/// <inheritdoc cref="AddBuff(string, int, byte, bool)"/>
	internal void AddBuff<T>(int duration, byte stack) where T : StackingBuff => AddBuff(typeof(T).Name, duration, stack);

	/// <param name="stack"> The number of stacks to add. </param>
	internal void AddBuff(string name, int duration, byte stack)
	{
		var inst = StackingBuff.NewBuff(name);
		inst.OnAdded();

		foreach (var item in stackingBuffs) //Stack with an existing buff
		{
			if (item.Name == name)
			{
				stack += item.stacks;

				stackingBuffs.Remove(item);
				break;
			}
		}

		inst.duration = duration;
		inst.stacks = Math.Min(stack, inst.MaxStacks);

		stackingBuffs.Add(inst);
	}

	internal bool RemoveBuff<T>() where T : StackingBuff => RemoveBuff(typeof(T).Name);
	internal bool RemoveBuff(string name)
	{
		bool value = false;

		foreach (var item in stackingBuffs)
		{
			if (item.Name == name)
			{
				value = stackingBuffs.Remove(item);
				item.OnRemoved(false);

				break;
			}
		}

		return value;
	}

	internal bool HasBuff<T>(out T value) where T : StackingBuff
	{
		if (stackingBuffs.FirstOrDefault(x => x is T) is StackingBuff def)
		{
			value = def as T;
			return true;
		}

		value = null;
		return false;
	}

	public override void UpdateLifeRegen(NPC npc, ref int damage)
	{
		List<StackingBuff> removals = [];

		foreach (var buff in stackingBuffs)
		{
			buff.UpdateEffects(npc);

			if (--buff.duration <= 0)
				removals.Add(buff);
		}

		foreach (var entry in removals)
		{
			stackingBuffs.Remove(entry);
			entry.OnRemoved(true);
		}
	}
}

internal static class StackingHelper
{
	/// <param name="send"> Whether this interaction should be synced. </param>
	public static void AddStackingBuff<T>(this NPC npc, int duration, byte stack = 1, bool send = true) where T : StackingBuff
	{
		if (npc.TryGetGlobalNPC(out StackingNPC sNPC))
		{
			sNPC.AddBuff<T>(duration, stack);

			if (send && Main.netMode != NetmodeID.SinglePlayer)
				new StackAddData(nameof(T), (short)npc.whoAmI, (short)duration, stack).Send();
		}
	}

	/// <param name="send"> Whether this interaction should be synced. </param>
	public static bool RemoveStackingBuff<T>(this NPC npc, bool send = true) where T : StackingBuff
	{
		bool value = false;
		if (npc.TryGetGlobalNPC(out StackingNPC sNPC))
		{
			value = sNPC.RemoveBuff<T>();

			if (send && Main.netMode != NetmodeID.SinglePlayer)
				new StackRemovalData(nameof(T), (short)npc.whoAmI).Send();
		}

		return value;
	}

	public static bool HasStackingBuff<T>(this NPC npc, out T value) where T : StackingBuff
	{
		if (npc.TryGetGlobalNPC(out StackingNPC sNPC) && sNPC.HasBuff(out value))
			return true;

		value = null;
		return false;
	}
}