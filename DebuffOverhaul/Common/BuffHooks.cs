namespace SpiritReforged.DebuffOverhaul.Common;

internal sealed class BuffDetours : ILoadable
{
    /// <summary> Whether combat text caused by damage over time should be prevented. </summary>
    public static bool BlockDoTText { get; set; }

    public void Load(Mod mod)
    {
        On_NPC.AddBuff += AddExtensionData;
        On_NPC.UpdateNPC_BuffApplyVFX += DisableVFX;
        On_NPC.DelBuff += ClearExtension;
        HealthBarHook.PostDrawHealthBar += DrawExtensionHealthBars;

        //Handle DoT combat text
        On_CombatText.NewText_Rectangle_Color_string_bool_bool += DisableDoT;
        On_NPC.UpdateNPC_BuffApplyDOTs += static (orig, self) =>
        {
            orig(self);
            BlockDoTText = false; //Reset to default
        };
    }

    private static void DrawExtensionHealthBars(HealthBarHook.Options options, Entity entity)
    {
        if (entity is NPC npc && npc.TryGetGlobalNPC<BuffGlobalNPC>(out var global))
            foreach (var type in global.buffByType.Keys)
            {
                global.buffByType[type].PostDrawHealthBar(Main.spriteBatch, options);
                break;
            }
    }

    private static void AddExtensionData(On_NPC.orig_AddBuff orig, NPC self, int type, int time, bool quiet)
    {
        if (!self.buffImmune[type] && self.TryGetGlobalNPC<BuffGlobalNPC>(out var global))
            if (global.buffByType.TryGetValue(type, out BuffExtension extension))
                extension.ApplyTo(self, true);
            else if (BuffExtension.Handler.FromType(type) is BuffExtension b)
            {
                global.buffByType.Add(type, b);
                global.buffByType[type].ApplyTo(self, false);
            }

        orig(self, type, time, quiet);
    }

    private static void DisableVFX(On_NPC.orig_UpdateNPC_BuffApplyVFX orig, NPC self)
    {
        bool doDefault = true;
        if (self.TryGetGlobalNPC<BuffGlobalNPC>(out var global))
            foreach (var type in global.buffByType.Keys)
            {
                BuffExtension b = global.buffByType[type];

                b.DoVisuals();
                doDefault |= !b.UsesCustomVFX;
            }

        if (doDefault)
            orig(self); //Skip orig
    }

    private static void ClearExtension(On_NPC.orig_DelBuff orig, NPC self, int buffIndex)
    {
        int type = self.buffType[buffIndex];

        orig(self, buffIndex);

        if (self.TryGetGlobalNPC<BuffGlobalNPC>(out var global))
            global.buffByType.Remove(type);
    }

    private static int DisableDoT(On_CombatText.orig_NewText_Rectangle_Color_string_bool_bool orig, Rectangle location, Color color, string text, bool dramatic, bool dot)
    {
        int value = orig(location, color, text, dramatic, dot);

        if (dot && BlockDoTText)
            Main.combatText[value].active = false;

        return value;
    }

    public void Unload() { }
}

public sealed class BuffGlobalNPC : GlobalNPC
{
    public override bool InstancePerEntity => true;

    /// <summary> Buff extension data indexed by buff ID. </summary>
    public readonly Dictionary<int, BuffExtension> buffByType = [];

    public override void UpdateLifeRegen(NPC npc, ref int damage)
    {
        foreach (var type in buffByType.Keys)
            buffByType[type].UpdateLifeRegen(ref damage);
    }
}