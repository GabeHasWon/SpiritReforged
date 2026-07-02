namespace SpiritReforged.Common.NPCCommon;

public sealed class StopGoresHook : ILoadable
{
    /// <summary> Determines when NPCs should be prevented from spawning gores on death. </summary>
    public static Func<NPC, bool> Conditions { get; set; }

    private static bool Tracking;

    public void Load(Mod mod)
    {
        On_NPC.HitEffect_HitInfo += TrackGore;
        On_Gore.NewGore_IEntitySource_Vector2_Vector2_int_float += StopGore;
    }

    /// <summary> Tracks on hit gores for removal according to <see cref="ShouldTrackGore"/>. </summary>
    private static void TrackGore(On_NPC.orig_HitEffect_HitInfo orig, NPC self, NPC.HitInfo hit)
    {
        var enumerator = Conditions.GetInvocationList().GetEnumerator();
        while (enumerator.MoveNext())
            if (enumerator.Current is Func<NPC, bool> func && (Tracking = func.Invoke(self)))
                break;

        orig(self, hit);
        Tracking = false;
    }

    /// <summary> Deactivates the spawned gore according to <see cref="TrackGore"/>. </summary>
    private static int StopGore(On_Gore.orig_NewGore_IEntitySource_Vector2_Vector2_int_float orig, Terraria.DataStructures.IEntitySource source, Vector2 Position, Vector2 Velocity, int Type, float Scale)
    {
        int result = orig(source, Position, Velocity, Type, Scale);

        if (Tracking)
            Main.gore[result].active = false; //Instantly deactivate the spawned gore

        return result;
    }

    public void Unload() { }
}
