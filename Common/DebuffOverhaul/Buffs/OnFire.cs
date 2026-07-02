using SpiritReforged.Common.Misc;
using SpiritReforged.Common.NPCCommon;
using SpiritReforged.Content.Particles.Basic;
using Terraria.Audio;
using Terraria.GameContent.Drawing;

namespace SpiritReforged.Common.DebuffOverhaul.Buffs;

public class OnFire : DoTExtension
{
	public static readonly Asset<Texture2D> BurningHealth = ModContent.Request<Texture2D>(VanillaTextures + "FireHealthBar");
    public static readonly Asset<Texture2D> Flame = ModContent.Request<Texture2D>(VanillaTextures + "SmallFlame");

    public override Settings LocalSettings => new(0.2f, 500);

    public override void Load()
    {
        Handler.Register(this, BuffID.OnFire);

        StopGoresHook.Conditions += static (npc) => npc.HasBuff(BuffID.OnFire);
        NPCEvents.HitEffectEvent += FireDeathEffects;
    }

    public static void FireDeathEffects(NPC npc, NPC.HitInfo hit)
    {
        if (!Main.dedServ && npc.life <= 0 && npc.HasBuff(BuffID.OnFire))
        {
            ParticleOrchestrator.SpawnParticlesDirect(ParticleOrchestraType.AshTreeShake, new() { PositionInWorld = npc.Center });
            int amount = npc.width / 5;

            if (npc.collideY || Collision.SolidCollision(new(npc.position.X, npc.position.Y + 4), npc.width, npc.height))
				for (int i = 0; i < amount; i++)
				{
					Vector2 position = npc.Bottom + Main.rand.NextVector2Circular(npc.width / 2 + 5, 2) + new Vector2(0, 2);
					ParticleOrchestrator.SpawnParticlesDirect(ParticleOrchestraType.FlameWaders, new() { PositionInWorld = position });
				}

            for (int i = 0; i < amount; i++)
            {
                var dust = Dust.NewDustDirect(npc.position, npc.width, npc.height, DustID.Torch, 0, -1, Scale: 2);
                dust.noGravity = true;
                dust.fadeIn = 2.2f;

                var dust2 = Dust.NewDustDirect(npc.position, npc.width, npc.height, DustID.Smoke, 0, 0, 150);
                dust2.fadeIn = Main.rand.NextFloat(1, 2);
                dust2.velocity *= 0.5f;
            }

            SoundEngine.PlaySound(SoundID.DD2_WyvernDiveDown with { Pitch = 0.5f, MaxInstances = 3 }, npc.Center);
        }
    }

    protected override void OnApply(bool reApplied)
    {
        base.OnApply(reApplied);

        if (!reApplied)
			for (int i = 0; i < 5; i++)
				Main.ParticleSystem_World_OverPlayers.Add(new EmberParticle(120)
				{
					LocalPosition = NPC.Center,
					Velocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(),
					Scale = new Vector2(1) * Main.rand.NextFloat(0.2f, 0.8f),
					AccelerationPerFrame = new(0, -0.01f)
				});
    }

    public override void PostDrawHealthBar(SpriteBatch spriteBatch, HealthBarHook.Options options)
    {
        float progress = (float)NPC.life / NPC.lifeMax;
        float fadeout = MathHelper.Min(BuffTime / 30f, 1);
        float lightness = 1f; //options.Lightness;
        float flameScale = options.Scale * Math.Min(fadeout, progress * 10) * 0.6f;

        Texture2D front = BurningHealth.Value;
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
        const int frames = 6;

        Texture2D flame = Flame.Value;
        Rectangle source = flame.Frame(1, frames, 0, (int)(Main.timeForVisualEffects / 3f) % frames, 0, -2);
        Vector2 finalScale = new Vector2(1, 1.2f + (float)Math.Sin(Main.timeForVisualEffects / 4f) * 0.2f) * scale;
        Vector2 origin = new(source.Width / 2, source.Height);

        Main.spriteBatch.Draw(flame, position, source, color, 0, origin, finalScale, default, 0);
    }
}