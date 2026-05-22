using ILLogger;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using Terraria.GameContent.UI;
using Terraria.ModLoader.Core;

namespace SpiritReforged.Common.EmoteCommon;

/// <summary>
/// Allows Projectiles and NPCs to modify their emote's draw offsets dynamically. Supports both the mod entities and their globals.
/// </summary>
internal interface IModifyEmotePosition
{
	private class ModifyEmoteHooks : ILoadable
	{
		public void Load(Mod mod) => IL_EmoteBubble.Draw += ModifyDrawPos;

		private static void ModifyDrawPos(ILContext il)
        {
            ILCursor c = new(il);

            if (!c.TryGotoNext(x => x.MatchCall(typeof(Utils), nameof(Utils.Floor))))
            {
                SpiritReforgedMod.Instance.LogIL("Modify Emote Position", "Method Utils.Floor not found.");
                return;
            }

            int localIndex = -1;

            if (!c.TryGotoNext(x => x.MatchStloc(out localIndex)))
            {
                SpiritReforgedMod.Instance.LogIL("Modify Emote Position", "Stloc for 'position' not found.");
                return;
            }

            c.Index++;
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldloca_S, (byte)localIndex);
            c.EmitDelegate(ModifyPosition);
        }

        private static void ModifyPosition(EmoteBubble bubble, ref Vector2 position)
        {
            if (bubble.anchor.type == WorldUIAnchor.AnchorType.Entity)
            {
                if (bubble.anchor.entity is Projectile projectile)
                    Invoke(projectile, ref position);
                else if (bubble.anchor.entity is NPC npc)
                    Invoke(npc, ref position);
            }
        }

		public void Unload() { }
	}

	public static GlobalHookList<GlobalProjectile> ProjHook = ProjectileLoader.AddModHook(GlobalHookList<GlobalProjectile>.Create(i => ((IModifyEmotePosition)i).ModifyEmotePosition));
    public static GlobalHookList<GlobalNPC> NPCHook = NPCLoader.AddModHook(GlobalHookList<GlobalNPC>.Create(i => ((IModifyEmotePosition)i).ModifyEmotePosition));

    public void ModifyEmotePosition(ref Vector2 position);

    public static void Invoke(Projectile projectile, ref Vector2 position)
    {
        if (projectile.ModProjectile is IModifyEmotePosition emoteMod)
            emoteMod.ModifyEmotePosition(ref position);

        foreach (IModifyEmotePosition g in ProjHook.Enumerate(projectile))
            g.ModifyEmotePosition(ref position);
    }

    public static void Invoke(NPC npc, ref Vector2 position)
    {
        if (npc.ModNPC is IModifyEmotePosition emoteMod)
            emoteMod.ModifyEmotePosition(ref position);

        foreach (IModifyEmotePosition g in NPCHook.Enumerate(npc))
            g.ModifyEmotePosition(ref position);
    }
}
