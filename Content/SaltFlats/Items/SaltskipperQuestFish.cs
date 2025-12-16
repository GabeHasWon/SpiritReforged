using SpiritReforged.Content.SaltFlats.Biome;
using Terraria.DataStructures;

namespace SpiritReforged.Content.SaltFlats.Items;

public class SaltskipperQuestFish : ModItem
{
	public static LocalizedText DescriptionText { get; private set; }
	public static LocalizedText CatchLocationText { get; private set; }

	public override void SetStaticDefaults()
	{
		Item.ResearchUnlockCount = 2;
		ItemID.Sets.CanBePlacedOnWeaponRacks[Type] = true; 

		DescriptionText = this.GetLocalization("Description");
		CatchLocationText = this.GetLocalization("CatchLocation");
	}

	public override void SetDefaults() => Item.DefaultToQuestFish();

	public override bool IsQuestFish() => true; 

	public override bool IsAnglerQuestAvailable() => true;

	public override void AnglerQuestChat(ref string description, ref string catchLocation)
	{
		description = DescriptionText.Value;
		catchLocation = CatchLocationText.Value;
	}
}

public sealed class SaltQuestFishPlayer : ModPlayer
{
	public override void CatchFish(FishingAttempt attempt, ref int itemDrop, ref int npcSpawn, ref AdvancedPopupRequest sonar, ref Vector2 sonarPosition)
	{
		int saltskipper = ModContent.ItemType<SaltskipperQuestFish>();
		if (Player.InModBiome<SaltBiome>() && attempt.questFish == saltskipper && attempt.uncommon)
			itemDrop = saltskipper;
	}
}
