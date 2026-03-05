using SpiritReforged.Content.Savanna.Biome;
using SpiritReforged.Content.Savanna.Items.Fishing;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Savanna;

public class SavannaPlayer : ModPlayer
{
	public override void CatchFish(FishingAttempt attempt, ref int itemDrop, ref int npcSpawn, ref AdvancedPopupRequest sonar, ref Vector2 sonarPosition)
	{
		if (Player.InModBiome<SavannaBiome>())
		{
			if (attempt.crate)
				itemDrop = Main.hardMode ? ModContent.ItemType<SavannaCrateHardmode>() : ModContent.ItemType<SavannaCrate>();

			if (attempt.common && Main.rand.NextBool(5))
				itemDrop = Mod.Find<ModItem>("KillifishItem").Type;

			if (attempt.common && Main.rand.NextBool(5))
				itemDrop = Mod.Find<ModItem>("GarItem").Type;
		}
	}
}