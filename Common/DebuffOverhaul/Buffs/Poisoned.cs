using SpiritReforged.Common.Particle;
using SpiritReforged.Content.Particles.Basic;

namespace SpiritReforged.Common.DebuffOverhaul.Buffs;

public class Poisoned : DoTExtension
{
    public override BuffSettings Settings => new(Category.Poison);

    public override void Load() => BuffHandler.Register(this, BuffID.Poisoned);

    public override void PostDrawHealthBar(SpriteBatch spriteBatch, NPC npc, HealthBarHook.Options options)
    {
        Texture2D front = TextureAssets.Hb1.Value;
        float progress = (float)npc.life / npc.lifeMax;
        float fadeout = MathHelper.Min(BuffTime / 30f, 1);
        float lightness = options.Lightness * 2;
        Rectangle bounds = new(0, 0, (int)(front.Width * progress), front.Height);
        Color color = new(0.6f, 1f, 0.2f);

        HealthBarHook.DrawSimpleBar(spriteBatch, front, options.Position, bounds, options.Scale, color * fadeout * lightness);

        Vector2 endPosition = options.Position + new Vector2(front.Width * progress, front.Height / 2) * options.Scale;
        Texture2D bubble = BubbleParticle.Texture.Value;
        Rectangle source = bubble.Frame(1, 7, 0, (int)(Main.timeForVisualEffects / 3f) % 7, 0, -2);

        spriteBatch.Draw(bubble, endPosition, source, color * lightness, 0, source.Size() / 2, options.Scale, default, 0);

        if ((int)Main.timeForVisualEffects % 18 == 0 && fadeout == 1)
			TerrariaParticles.OverHealthBars.Add(new BubbleParticle(40, color * lightness, npc)
			{
				LocalPosition = endPosition + Main.screenPosition - npc.Center,
				Scale = new Vector2(0.8f) * options.Scale,
				AccelerationPerFrame = new(Main.rand.NextFloat(-0.01f, 0.01f), -0.02f)
			});
    }
}