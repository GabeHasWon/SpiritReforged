using System.IO;

namespace SpiritReforged.Common.DebuffOverhaul;

/// <summary>An extended buff data system designed for NPCs. See <see cref="ExtendedBuffGlobalNPC"/> for application.</summary>
public abstract class BuffExtension : ILoadable
{
    public static class BuffHandler
    {
        private static readonly Dictionary<int, BuffExtension> BuffByType = [];

        /// <summary> Gets a new instance associated with the provided buff type, and optionally provides an NPC instance. </summary>
        public static BuffExtension FromType(int type, NPC npc = null)
        {
            if (BuffByType.TryGetValue(type, out var value))
            {
                var result = (BuffExtension)value.MemberwiseClone();
                result.Type = type;
				result.NPC = npc;

                return result;
            }

            return null;
        }

        public static bool Register(BuffExtension extension, int type) => BuffByType.TryAdd(type, extension);
        public static void Register(BuffExtension extension, params int[] types)
        {
            foreach (int type in types)
                Register(extension, type);
        }
    }

	public int BuffTime => (NPC.FindBuffIndex(Type) is int value && value != -1) ? NPC.buffTime[value] : 0;

    /// <summary> The NPC this instance has been applied to. </summary>
    public NPC NPC { get; private set; }
    /// <summary> The specific type of buff this instance is applied on behalf of. </summary>
    public int Type { get; private set; }
	/// <summary> Whether this extension uses custom VFX. True by default. </summary>
    public bool UsesCustomVFX { get; private set; } = true;

    public void Load(Mod mod) => Load();
    public bool Active() => NPC.HasBuff(Type);
    public void ApplyTo(NPC npc, bool reApplied)
    {
		NPC = npc;
        OnApply(reApplied);
    }

    public virtual void Load() { }
    public virtual void Unload() { }

    protected virtual void OnApply(bool reApplied) { }
    public virtual void UpdateLifeRegen(ref int damage) { }
    public virtual void DoVisuals() => UsesCustomVFX = false;
    public virtual void PostDrawHealthBar(SpriteBatch spriteBatch, NPC npc, HealthBarHook.Options options) { }

	public virtual void NetSend(BinaryWriter writer) { }
	public virtual void NetReceive(BinaryReader reader) { }
}