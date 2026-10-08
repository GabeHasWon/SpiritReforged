namespace SpiritReforged.Common.Misc;

internal static class SpiritConditions
{
	// These InBiome properties use an anonymous method to wrap around the Zone checks.
	// VS will mark this as needless, but removing the wrapping delegate will cause issues.
	// This is because otherwise the condition used will capture an invalid LocalPlayer instance, causing a null ref.
	
	public static Condition InSavanna => new("Mods.SpiritReforged.Conditions.InSavanna", () => Main.LocalPlayer.InModBiome<Content.Savanna.Biome.SavannaBiome>());
	public static Condition InSaltFlats => new("Mods.SpiritReforged.Conditions.InSaltFlats", () => Main.LocalPlayer.InModBiome<Content.SaltFlats.Biome.SaltBiome>());
	public static Condition InSpace => new("Mods.SpiritReforged.Conditions.InSpace", () => Main.LocalPlayer.Center.Y / 16 < Main.worldSurface * 0.35f);
}
