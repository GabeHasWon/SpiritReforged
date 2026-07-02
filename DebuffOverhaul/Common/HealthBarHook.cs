using Mono.Cecil.Cil;
using MonoMod.Cil;

namespace SpiritReforged.DebuffOverhaul.Common;

public sealed class HealthBarHook : GlobalNPC
{
    public readonly record struct Options(Color Color, Vector2 Position, float Lightness, float Scale);
    public static event Action<Options, Entity> PreDrawHealthBar;
    public static event Action<Options, Entity> PostDrawHealthBar;

    private static Options LastOptions;
    private static Entity LastEntity;

    public override void Load()
    {
        IL_Main.DrawHealthBar += IL_Main_DrawHealthBar;
        On_Main.DrawHealthBar += InsertPostDraw;
    }

    private static void InsertPostDraw(On_Main.orig_DrawHealthBar orig, Main self, float X, float Y, int Health, int MaxHealth, float alpha, float scale, bool noFlip)
    {
        orig(self, X, Y, Health, MaxHealth, alpha, scale, noFlip);

        PostDrawHealthBar?.Invoke(LastOptions, LastEntity);
        LastEntity = null; //Scrub value
    }

    private static void IL_Main_DrawHealthBar(ILContext il)
    {
        ILCursor c = new(il);

        c.GotoNext(MoveType.Before, x => x.MatchLdsfld<Main>(nameof(Main.spriteBatch)));
        
        for (int i = 0; i < 3; i++)
            c.GotoPrev(x => x.MatchLdloc1());

        c.Emit(OpCodes.Ldloc_S, (byte)9);
        c.EmitLdloc1(); //num2 (int)
        c.EmitLdloc2(); //num3 (float)
        c.EmitLdloc3(); //num4 (float)
        c.Emit(OpCodes.Ldarg_S, (byte)5); //alpha (float)
        c.Emit(OpCodes.Ldarg_S, (byte)6); //scale (float)

        c.EmitDelegate<Action<Color, int, float, float, float, float>>(static (color, num2, num3, num4, alpha, scale) =>
        {
            if (num2 < 3)
                num2 = 3;

            Vector2 pos = new(num3 - Main.screenPosition.X, num4 - Main.screenPosition.Y);
            Options options = new(color, pos, alpha, scale);
            LastOptions = options;

            PreDrawHealthBar?.Invoke(options, LastEntity);
        });
    }

    public override bool? DrawHealthBar(NPC npc, byte hbPosition, ref float scale, ref Vector2 position)
    {
        LastEntity = npc;
        return null;
    }

    public static void DrawSimpleBar(SpriteBatch spriteBatch, Texture2D texture, Vector2 topLeft, Rectangle bounds, float scale, Color color)
    {
        const int endLength = 2;

        Vector2 origin = Vector2.Zero;
        Rectangle trimmedBounds = bounds with { Width = bounds.Width - endLength };

        spriteBatch.Draw(texture, topLeft, trimmedBounds, color, 0, origin, scale, default, 0);
        spriteBatch.Draw(texture, topLeft + new Vector2(Math.Max(trimmedBounds.Width * scale, 1), 0), bounds with { X = texture.Width - (endLength + 2), Width = endLength }, color, 0, origin, scale, default, 0);
    }
}