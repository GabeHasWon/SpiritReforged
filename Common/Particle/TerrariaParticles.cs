using Terraria.Graphics.Renderers;

namespace SpiritReforged.Common.Particle;

public sealed class TerrariaParticles : ModSystem
{
	public static readonly ParticleRenderer OverInventory = new();
	public static readonly ParticleRenderer OverHealthBars = new();

	public override void Load()
	{
		On_Main.UpdateParticleSystems += UpdateParticles;
		On_Main.DrawInventory += PostDrawInventory;
		On_Main.DrawInterface_14_EntityHealthBars += PostDrawHealthBars;
	}

	private static void UpdateParticles(On_Main.orig_UpdateParticleSystems orig, Main self)
	{
		OverInventory.Update();
		OverHealthBars.Update();

		orig(self);
	}

	private static void PostDrawInventory(On_Main.orig_DrawInventory orig, Main self)
	{
		orig(self);
		OverInventory.Draw(Main.spriteBatch);
	}

	private static void PostDrawHealthBars(On_Main.orig_DrawInterface_14_EntityHealthBars orig, Main self)
	{
		orig(self);
		OverHealthBars.Draw(Main.spriteBatch);
	}
}