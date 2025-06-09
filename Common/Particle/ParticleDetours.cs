using SpiritReforged.Common.Misc;

namespace SpiritReforged.Common.Particle;

public static class ParticleDetours
{
	public static void Initialize()
	{
		On_Main.DrawProjectiles += AtProjectile;
		On_Main.DrawNPCs += AboveNPC;
		On_Main.DrawInfernoRings += AbovePlayer;
		On_Main.DoDraw_Tiles_NonSolid += BelowSolid;
	}

	private static void AbovePlayer(On_Main.orig_DrawInfernoRings orig, Main self)
	{
		orig(self);
		Main.spriteBatch.End();
		Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, default, default, RasterizerState.CullNone, default, Main.GameViewMatrix.TransformationMatrix);
		ParticleHandler.DrawAllParticles(Main.spriteBatch, ParticleLayer.AbovePlayer);
		Main.spriteBatch.RestartToDefault();
	}

	private static void AboveNPC(On_Main.orig_DrawNPCs orig, Main self, bool behindTiles)
	{
		orig(self, behindTiles);
		ParticleHandler.DrawAllParticles(Main.spriteBatch, ParticleLayer.AboveNPC);
	}

	private static void AtProjectile(On_Main.orig_DrawProjectiles orig, Main self)
	{
		Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, default, default, RasterizerState.CullNone, default, Main.GameViewMatrix.TransformationMatrix);
		ParticleHandler.DrawAllParticles(Main.spriteBatch, ParticleLayer.BelowProjectile);
		Main.spriteBatch.End();
		orig(self);
		ParticleHandler.DrawAllParticles(Main.spriteBatch, ParticleLayer.AboveProjectile);
	}

	private static void BelowSolid(On_Main.orig_DoDraw_Tiles_NonSolid orig, Main self)
	{
		orig(self);
		ParticleHandler.DrawAllParticles(Main.spriteBatch, ParticleLayer.BelowSolids);
	}

	public static void Unload()
	{
		On_Main.DrawProjectiles -= AtProjectile;
		On_Main.DrawNPCs -= AboveNPC;
		On_Main.DrawInfernoRings -= AbovePlayer;
	}
}
