namespace SpiritReforged.Common.DebuffOverhaul;

/// <summary> A damage-over-time buff extension that can be registered through mod call. </summary>
public class CustomDoT(float scalability, int damageLimit, bool stackable, Action scalingBehaviour = null, Action<SpriteBatch, NPC, Color, Vector2, float, float> onPostDraw = null) : DoTExtension
{
    public override BuffSettings Settings => _settings;

    private readonly BuffSettings _settings = new(scalability, damageLimit, stackable, scalingBehaviour);
    private readonly Action<SpriteBatch, NPC, Color, Vector2, float, float> _onPostDraw = onPostDraw;

    public override void PostDrawHealthBar(SpriteBatch spriteBatch, NPC npc, HealthBarHook.Options options) => _onPostDraw?.Invoke(spriteBatch, npc, options.Color, options.Position, options.Lightness, options.Scale);
}