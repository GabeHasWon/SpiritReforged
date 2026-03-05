using SpiritReforged.Common.Misc;

namespace SpiritReforged.Common.Particle;

internal class ParticleDetours : ILoadable
{
	public void Load(Mod mod)
	{
		On_Main.DrawProjectiles += AtProjectile;
		On_Main.DrawNPCs += AboveNPC;
		On_Main.DrawInfernoRings += AbovePlayer;
		On_Main.DoDraw_Tiles_NonSolid += BelowSolid;
		On_Main.DoDraw_WallsAndBlacks += BelowWall;
	}

	private static void AbovePlayer(On_Main.orig_DrawInfernoRings orig, Main self)
	{
		orig(self);
		
		if (ParticleHandler.Particles.Length != 0) //Avoid restarting the SpriteBatch if there's nothing to draw
		{
			Main.spriteBatch.End();
			Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, default, default, RasterizerState.CullNone, default, Main.GameViewMatrix.TransformationMatrix);
			ParticleHandler.DrawAllParticles(Main.spriteBatch, ParticleLayer.AbovePlayer);
			Main.spriteBatch.RestartToDefault();
		}
	}

	private static void AboveNPC(On_Main.orig_DrawNPCs orig, Main self, bool behindTiles)
	{
		orig(self, behindTiles);
		ParticleHandler.DrawAllParticles(Main.spriteBatch, ParticleLayer.AboveNPC);
	}

	private static void AtProjectile(On_Main.orig_DrawProjectiles orig, Main self)
	{
		if (ParticleHandler.Particles.Length == 0)
		{
			orig(self);
			return;
		}

		Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, default, default, RasterizerState.CullNone, default, Main.GameViewMatrix.TransformationMatrix);
		ParticleHandler.DrawAllParticles(Main.spriteBatch, ParticleLayer.BelowProjectile);
		Main.spriteBatch.End();

		orig(self);

		ParticleHandler.DrawAllParticles(Main.spriteBatch, ParticleLayer.AboveProjectile);
	}

	private static void BelowSolid(On_Main.orig_DoDraw_Tiles_NonSolid orig, Main self)
	{
		orig(self);
		ParticleHandler.DrawAllParticles(Main.spriteBatch, ParticleLayer.BelowSolid);
	}

	private static void BelowWall(On_Main.orig_DoDraw_WallsAndBlacks orig, Main self)
	{
		orig(self);
		ParticleHandler.DrawAllParticles(Main.spriteBatch, ParticleLayer.BelowWall);
	}

	public void Unload() { }
}
