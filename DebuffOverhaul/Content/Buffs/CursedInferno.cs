using SpiritReforged.Common.Misc;
using SpiritReforged.DebuffOverhaul.Common;

namespace SpiritReforged.DebuffOverhaul.Content.Buffs;

public class CursedInferno : DoTExtension
{
    public static readonly Asset<Texture2D> CursedHealth = ModContent.Request<Texture2D>(nameof(DebuffOverhaul) + "/Assets/Textures/CursedHealthBar");

    public override Settings LocalSettings => new(0.3f, 1500);

    public override void Load() => Handler.Register(this, BuffID.CursedInferno);

    public override void PostDrawHealthBar(SpriteBatch spriteBatch, HealthBarHook.Options options)
    {
        float progress = (float)NPC.life / NPC.lifeMax;
        float fadeout = MathHelper.Min(BuffTime / 30f, 1);
        float lightness = 1f; //options.Lightness;
        float flameScale = options.Scale * Math.Min(fadeout, progress * 10) * 0.8f;

        Texture2D front = CursedHealth.Value;
        Rectangle bounds = new(0, 0, Math.Max((int)(front.Width * progress), 0), front.Height);
        Vector2 endPosition = options.Position + new Vector2(front.Width * progress, front.Height / 2) * options.Scale;

        HealthBarHook.DrawSimpleBar(spriteBatch, front, options.Position, bounds, options.Scale, Color.White * fadeout * lightness);

        //Draw base
        for (int i = 0; i < 3; i++)
            DrawFlame(endPosition + Main.rand.NextVector2Unit() * Main.rand.NextFloat(2), flameScale, Color.Lerp(Color.Green, Color.White, i / 2f).Additive() * lightness);
    }

    private static void DrawFlame(Vector2 position, float scale, Color color)
    {
        Main.instance.LoadProjectile(ProjectileID.CursedFlameFriendly);

        Texture2D flame = TextureAssets.Projectile[ProjectileID.CursedFlameFriendly].Value;
        Rectangle source = flame.Frame();
        float finalScale = scale + (float)Math.Sin(Main.timeForVisualEffects / 4f) * 0.2f;
        Vector2 origin = source.Size() / 2;
        float rotation = Main.GameUpdateCount * 0.2f;

        Main.spriteBatch.Draw(flame, position, source, color, rotation, origin, finalScale, default, 0);
    }
}