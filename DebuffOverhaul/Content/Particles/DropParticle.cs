using SpiritReforged.Common.Visuals;
using Terraria.Graphics.Renderers;

namespace SpiritReforged.DebuffOverhaul.Content.Particles;

public class DropParticle(int lifeTime, Color color, Entity anchor = default) : ABasicParticle
{
    public static readonly Asset<Texture2D> Texture = DrawHelpers.RequestLocal<DropParticle>("DropParticle", false);

	private int _timeLeft = lifeTime;
    private readonly int _timeLeftMax = lifeTime;

    private readonly Color _color = color;
    private readonly Entity _entityAnchor = anchor;

    public override void Update(ref ParticleRendererSettings settings)
    {
        base.Update(ref settings);

        Rotation = Velocity.ToRotation() - MathHelper.PiOver2;
        if (--_timeLeft <= 0)
            ShouldBeRemovedFromRenderer = true;
    }

    public override void Draw(ref ParticleRendererSettings settings, SpriteBatch spritebatch)
    {
        var texture = Texture.Value;

        Vector2 entityPosition = _entityAnchor == null ? Vector2.Zero : _entityAnchor.Center + new Vector2(0, _entityAnchor is NPC n ? n.gfxOffY : 0);
        Vector2 position = settings.AnchorPosition + entityPosition + LocalPosition;
        Rectangle source = texture.Frame();

        int fade = _timeLeftMax - 10;
        float fadeOut = _timeLeft < _timeLeftMax / 2 ? Math.Clamp((float)_timeLeft / _timeLeftMax * 4, 0, 1) : Math.Clamp(1f - ((float)_timeLeft - fade) / (_timeLeftMax - fade), 0, 1);
        Vector2 finalScale = Vector2.One * Scale * fadeOut;

        position -= Main.screenPosition; //TEMP

        spritebatch.Draw(texture, position, source, _color, Rotation, source.Size() / 2, finalScale, default, 0);
    }
}