using Terraria.Graphics.Renderers;

namespace SpiritReforged.Common.Particle;

internal sealed class TerrariaParticles : ModSystem
{
	public static readonly ParticleRenderer OverInventory = new();

	public override void Load() => On_Main.DrawInventory += PostDrawInventory;
	private static void PostDrawInventory(On_Main.orig_DrawInventory orig, Main self)
	{
		orig(self);
		OverInventory.Draw(Main.spriteBatch);
	}

	public override void PostUpdateDusts() => OverInventory.Update();
}