using SpiritReforged.Common.Particle;
using SpiritReforged.Content.Particles.Basic;

namespace SpiritReforged.Common.DebuffOverhaul.Buffs;

public class Bleeding : DoTExtension
{
    public override Settings LocalSettings => new(1f, 800);

    public override void Load() => Handler.Register(this, BuffID.Bleeding);

    public override void PostDrawHealthBar(SpriteBatch spriteBatch, HealthBarHook.Options options)
    {
        Texture2D front = TextureAssets.Hb1.Value;
        float progress = (float)NPC.life / NPC.lifeMax;
        float fadeout = MathHelper.Min(BuffTime / 30f, 1);
        float lightness = options.Lightness * 2;
        Rectangle bounds = new(0, 0, (int)(front.Width * progress), front.Height);
        Color color = Color.Red;

        HealthBarHook.DrawSimpleBar(spriteBatch, front, options.Position, bounds, options.Scale, color * fadeout * lightness);

        Vector2 endPosition = options.Position + new Vector2(front.Width * progress, front.Height / 2) * options.Scale;
        Texture2D bubble = BubbleParticle.Texture.Value;
        Rectangle source = bubble.Frame(1, 7, 0, (int)(Main.timeForVisualEffects / 3f) % 7, 0, -2);

        spriteBatch.Draw(bubble, endPosition, source, color * lightness, 0, source.Size() / 2, options.Scale, default, 0);

        if ((int)Main.timeForVisualEffects % 25 == 0 && fadeout == 1)
            TerrariaParticles.OverHealthBars.Add(new DropParticle(40, color * lightness, NPC)
            {
                LocalPosition = endPosition + Main.screenPosition - NPC.Center,
                Scale = new Vector2(0.7f) * options.Scale,
                AccelerationPerFrame = new(0, 0.02f)
            });
    }
}