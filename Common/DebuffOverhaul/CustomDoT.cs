namespace SpiritReforged.Common.DebuffOverhaul;

/// <summary> A damage-over-time buff extension that can be registered through mod call. </summary>
public class CustomDoT(float scalability, int damageLimit, Action<SpriteBatch, NPC, Color, Vector2, float, float> onPostDraw = null) : DoTExtension
{
    public override Settings LocalSettings => _settings;

    private readonly Settings _settings = new(scalability, damageLimit);
    private readonly Action<SpriteBatch, NPC, Color, Vector2, float, float> _onPostDraw = onPostDraw;

    public override void PostDrawHealthBar(SpriteBatch spriteBatch, HealthBarHook.Options options) => _onPostDraw?.Invoke(spriteBatch, NPC, options.Color, options.Position, options.Lightness, options.Scale);
}