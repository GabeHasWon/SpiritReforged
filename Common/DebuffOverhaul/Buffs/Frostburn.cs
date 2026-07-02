using SpiritReforged.Common.Misc;

namespace SpiritReforged.Common.DebuffOverhaul.Buffs;

public class Frostburn : DoTExtension
{
    public static readonly Asset<Texture2D> FrozenHealth = ModContent.Request<Texture2D>(VanillaTextures + "FrostHealthBar");
	public static readonly Asset<Texture2D> Snowflake = ModContent.Request<Texture2D>(VanillaTextures + "Snowflake");

	public override Settings LocalSettings => new(0.25f, 500);

    public override void Load() => Handler.Register(this, BuffID.Frostburn);

    public override void PostDrawHealthBar(SpriteBatch spriteBatch, HealthBarHook.Options options)
    {
        float progress = (float)NPC.life / NPC.lifeMax;
        float fadeout = MathHelper.Min(BuffTime / 30f, 1);
        float lightness = options.Lightness;

        Texture2D front = FrozenHealth.Value;
        Texture2D snowflake = Snowflake.Value;

        Rectangle bounds = new(0, 0, Math.Max((int)(front.Width * progress), 0), front.Height);
        Vector2 endPosition = options.Position + new Vector2(front.Width * progress, front.Height / 2) * options.Scale;

        DrawSnowflake(new Vector2(2, 0), Color.Blue.Additive());
        DrawSnowflake(new Vector2(0, 2), Color.Blue.Additive());
        DrawSnowflake(new Vector2(-2, 0), Color.Blue.Additive());
        DrawSnowflake(new Vector2(0, -2), Color.Blue.Additive());

        HealthBarHook.DrawSimpleBar(spriteBatch, front, options.Position, bounds, options.Scale, Color.White * fadeout * lightness);

        DrawSnowflake(Vector2.Zero, Color.White);

        void DrawSnowflake(Vector2 offset, Color baseColor)
        {
            spriteBatch.Draw(snowflake, endPosition + offset, null, baseColor * lightness * fadeout, 0, snowflake.Size() / 2, options.Scale * Math.Min(fadeout, progress * 10), default, 0);
        }
    }
}