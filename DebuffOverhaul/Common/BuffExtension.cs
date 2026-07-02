namespace SpiritReforged.DebuffOverhaul.Common;

public abstract class BuffExtension : ILoadable
{
    public static class Handler
    {
        private static readonly Dictionary<int, BuffExtension> BuffByType = [];

        /// <summary> Gets a new instance associated with the provided buff type. </summary>
        public static BuffExtension FromType(int type)
        {
            if (BuffByType.TryGetValue(type, out var value))
            {
                var result = (BuffExtension)value.MemberwiseClone();
                result.Type = type;

                return result;
            }

            return null;
        }

        public static bool Register(BuffExtension extension, int type) => BuffByType.TryAdd(type, extension);
        public static void Register(BuffExtension extension, params int[] types)
        {
            foreach (var type in types)
                Register(extension, type);
        }
    }

    public int BuffTime => NPC.FindBuffIndex(Type) is int value && value != -1 ? NPC.buffTime[value] : 0;

    /// <summary> The NPC this instance is applied to. </summary>
    public NPC NPC { get; private set; }
    /// <summary> The specific type of buff this instance is applied on behalf of. </summary>
    public int Type { get; private set; }
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
    public virtual void PostDrawHealthBar(SpriteBatch spriteBatch, HealthBarHook.Options options) { }
}