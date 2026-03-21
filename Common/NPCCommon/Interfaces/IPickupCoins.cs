using ILLogger;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using Terraria.ModLoader.Core;

namespace SpiritReforged.Common.NPCCommon.Interfaces;

/// <summary>
/// Adds in a hook for NPCs to conditionally pick up coins.
/// </summary>
internal interface IPickupCoins
{
	private class PickupCoinHook : ILoadable
	{
		void ILoadable.Load(Mod mod) => IL_Item.GetPickedUpByMonsters_Money += AddPickupHook;

		private void AddPickupHook(ILContext il)
		{
			ILCursor c = new(il);

			if (!c.TryGotoNext(MoveType.After, x => x.MatchLdsfld(typeof(NPCID.Sets), nameof(NPCID.Sets.CantTakeLunchMoney))))
			{
				SpiritReforgedMod.Instance.LogIL("Pickup Coin Hook", "Member NPCID.Sets.CantTakeLunchMoney not found.");
				return;
			}

			ILLabel label = null;

			if (!c.TryGotoNext(MoveType.After, x => x.MatchBrtrue(out label)) || label is null)
			{
				SpiritReforgedMod.Instance.LogIL("Pickup Coin Hook", "Label for continue not found.");
				return;
			}

			c.Emit(OpCodes.Ldloc_S, (byte)2);
			c.EmitDelegate(Invoke);
			c.Emit(OpCodes.Brfalse, label);
		}

		void ILoadable.Unload() { }
	}

	public static GlobalHookList<GlobalNPC> Hook = NPCLoader.AddModHook(GlobalHookList<GlobalNPC>.Create(i => ((IPickupCoins)i).CanPickupCoins));

	public bool CanPickupCoins();

	public static bool Invoke(NPC npc)
	{
		if (npc.ModNPC is IPickupCoins coins && !coins.CanPickupCoins())
			return false;

		foreach (IPickupCoins coin in Hook.Enumerate(npc))
		{
			if (!coin.CanPickupCoins())
				return false;
		}

		return true;
	}
}
