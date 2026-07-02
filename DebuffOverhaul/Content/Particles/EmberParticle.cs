using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Visuals;
using Terraria.Graphics.Renderers;

namespace SpiritReforged.DebuffOverhaul.Content.Particles;

public class EmberParticle(int lifeTime) : ABasicParticle
{
    public static readonly Asset<Texture2D> Texture = DrawHelpers.RequestLocal<EmberParticle>("EmberParticle", false);

    private int _timeLeft = lifeTime;
    private readonly int _totalTimeLeft = lifeTime;

    public override void Update(ref ParticleRendererSettings settings)
    {
        base.Update(ref settings);

        if (--_timeLeft <= 0)
            ShouldBeRemovedFromRenderer = true;
    }

    public override void Draw(ref ParticleRendererSettings settings, SpriteBatch spritebatch)
    {
        var texture = Texture.Value;
        Vector2 position = settings.AnchorPosition + LocalPosition;
        float opacity = (float)_timeLeft / _totalTimeLeft;

        spritebatch.Draw(texture, position, null, Color.Red.Additive(), (float)Math.PI / 2f + Rotation, texture.Size() / 2, Vector2.One * Scale * opacity, default, 0);
        spritebatch.Draw(texture, position, null, Color.Goldenrod.Additive(), (float)Math.PI / 2f + Rotation, texture.Size() / 2, Vector2.One * Scale * 0.85f * opacity, default, 0);
        spritebatch.Draw(texture, position, null, Color.White.Additive(), (float)Math.PI / 2f + Rotation, texture.Size() / 2, Vector2.One * Scale * 0.5f * opacity, default, 0);
    }
}