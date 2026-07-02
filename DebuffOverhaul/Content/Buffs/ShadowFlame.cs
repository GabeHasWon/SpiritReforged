using SpiritReforged.Common.Misc;
using SpiritReforged.DebuffOverhaul.Common;

namespace SpiritReforged.DebuffOverhaul.Content.Buffs;

public class ShadowFlame : DoTExtension
{
    public static readonly Asset<Texture2D> ShadowFlameHealth = ModContent.Request<Texture2D>(nameof(DebuffOverhaul) + "/Assets/Textures/ShadowFlameHealthBar");

    public override Settings LocalSettings => new(0.5f, 2000);

    public override void Load() => Handler.Register(this, BuffID.ShadowFlame);

    public override void PostDrawHealthBar(SpriteBatch spriteBatch, HealthBarHook.Options options)
    {
        float progress = (float)NPC.life / NPC.lifeMax;
        float fadeout = MathHelper.Min(BuffTime / 30f, 1);
        float lightness = 1f; //options.Lightness;
        float flameScale = options.Scale * Math.Min(fadeout, progress * 10) * 0.6f;

        Texture2D front = ShadowFlameHealth.Value;
        Rectangle bounds = new(0, 0, Math.Max((int)(front.Width * progress), 0), front.Height);
        Vector2 endPosition = options.Position + new Vector2(front.Width * progress, 8) * options.Scale;

        //Draw outline
        DrawFlame(endPosition + new Vector2(2, 0), flameScale, Color.Red.Additive(150) * lightness);
        DrawFlame(endPosition + new Vector2(0, 2), flameScale, Color.Red.Additive(150) * lightness);
        DrawFlame(endPosition + new Vector2(-2, 0), flameScale, Color.Red.Additive(150) * lightness);
        DrawFlame(endPosition + new Vector2(0, -2), flameScale, Color.Red.Additive(150) * lightness);

        HealthBarHook.DrawSimpleBar(spriteBatch, front, options.Position, bounds, options.Scale, Color.White * fadeout * lightness);

        //Draw base
        for (int i = 0; i < 3; i++)
            DrawFlame(endPosition + Main.rand.NextVector2Unit() * Main.rand.NextFloat(2), flameScale, Color.White.Additive() * lightness);
    }

    private static void DrawFlame(Vector2 position, float scale, Color color)
    {
        const int frames = 4;
        Main.instance.LoadProjectile(ProjectileID.DesertDjinnCurse);

        float unit = Main.GameUpdateCount;
        Texture2D flame = TextureAssets.Projectile[ProjectileID.DesertDjinnCurse].Value;
        Rectangle source = flame.Frame(1, frames, 0, (int)(unit / 3f) % frames, 0, -2);
        Vector2 finalScale = new Vector2(1, 1.2f + (float)Math.Sin(unit / 4f) * 0.2f) * scale;
        Vector2 origin = new(source.Width / 2, source.Height);

        Main.spriteBatch.Draw(flame, position, source, color, 0, origin, finalScale, default, 0);
    }
}