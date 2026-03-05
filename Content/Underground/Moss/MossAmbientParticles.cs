using SpiritReforged.Common.Particle;
using SpiritReforged.Content.Particles;
using SpiritReforged.Content.Underground.Moss.Oganesson;
using SpiritReforged.Content.Underground.Moss.Radon;

namespace SpiritReforged.Content.Underground.Moss;

public class MossTileCounts : ModSystem
{
	public static bool InNeonMoss => ModContent.GetInstance<MossTileCounts>().neonCount >= 200;
	public int neonCount = 0;

	public override void TileCountsAvailable(ReadOnlySpan<int> tileCounts) => neonCount
		= tileCounts[TileID.ArgonMoss] + tileCounts[TileID.KryptonMoss] + tileCounts[TileID.XenonMoss]
		+ tileCounts[TileID.VioletMoss] + tileCounts[TileID.RainbowMoss] + tileCounts[ModContent.TileType<RadonMoss>()]
		+ tileCounts[ModContent.TileType<OganessonMoss>()];
}

internal class MossAmbientParticles : GlobalTile
{
	public override void NearbyEffects(int i, int j, int type, bool closer)
	{
		if (MossTileCounts.InNeonMoss && !Main.gamePaused && closer && Main.rand.NextBool(2400))
		{
			if (type == TileID.XenonMoss)
				SpawnMossAmbientParticles(i, j, new Color(0, 184, 255));
			if (type == TileID.ArgonMoss)
				SpawnMossAmbientParticles(i, j, new Color(255, 92, 160));
			if (type == TileID.KryptonMoss)
				SpawnMossAmbientParticles(i, j, new Color(105, 255, 41));
			if (type == TileID.VioletMoss)
				SpawnMossAmbientParticles(i, j, new Color(210, 97, 255));
			if (type == TileID.RainbowMoss)
				SpawnMossAmbientParticles(i, j, Main.DiscoColor);
			if (type == TileID.LavaMoss)
				SpawnMossAmbientParticles(i, j, new Color(252, 90, 3));
			if (type == ModContent.TileType<RadonMoss>())
				SpawnMossAmbientParticles(i, j, new Color(248, 255, 56));
			if (type == ModContent.TileType<OganessonMoss>())
				SpawnMossAmbientParticles(i, j, new Color(255, 255, 255));
		}
	}

	internal static void SpawnMossAmbientParticles(int i, int j, Color mossColor)
	{
		var startPos = new Vector2(i * 16, j * 16);
		var velocity = new Vector2(Main.rand.NextFloat(-.5f, .5f), Main.rand.NextFloat(-.6f, -0.2f));

		ParticleHandler.SpawnParticle(new GlowParticle(startPos, velocity,
			mossColor * 0.225f, .25f, Main.rand.Next(260, 400), 4, p =>
			{
				p.Velocity = p.Velocity.RotatedBy(Main.rand.NextFloat(-0.005f, 0.005f)) * 0.98f;
				p.Velocity.Y -= Main.rand.NextFloat(0.02f, 0.01f);
				p.Scale = 0.5f + 0.15f * (float)Math.Sin(p.TimeActive * .05f);

				//constantly update helium moss colors
				if (mossColor == Main.DiscoColor)
					p.Color = Main.DiscoColor;
			}));
	}
}