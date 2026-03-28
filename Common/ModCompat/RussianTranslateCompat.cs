using SpiritReforged.Content.Desert.ScarabBoss.Items;
using SpiritReforged.Content.Desert.Silk;
using SpiritReforged.Content.Forest.Botanist.Items;
using SpiritReforged.Content.Granite.Armor;
using SpiritReforged.Content.Ocean.Items.DriftwoodSet.DriftwoodArmor;
using SpiritReforged.Content.Ocean.Items.Reefhunter.CascadeArmor;
using SpiritReforged.Content.Savanna.Items.DrywoodSet;
using SpiritReforged.Content.Underground.WayfarerSet;
using SpiritReforged.Content.Vanilla.Leather.MarksmanArmor;

namespace SpiritReforged.Common.ModCompat;
internal class RussianTranslateCompat : ModSystem
{
	public override void PostSetupContent()
	{
		var spiritR = Mod;

		if (!CrossMod.RussianTranslate.Enabled)
			return;

		var tru = CrossMod.RussianTranslate.Instance;

		tru.Call("AddFeminineItems", spiritR, new[]
		{
			//Weapons
			"Dragonsong",
			"WoodenClub",
			"BambooHalberd",
			"ToucaneItem",
			"ClawCannon",
			"HuntingRifle",
			"BombCannon",
			"Bowlder",
			//Accessories
			"Ledger",
			"ScryingLens",
			"SleightOfHand"
		});

		tru.Call("AddNeuterItems", spiritR, new[]
		{
			//Accessories
			"ArcaneNecklaceGold",
			"ArcaneNecklacePlatinum",
			"CraneFeather",
			"SafekeeperRing",
			"PearlString",
			"OceanPendant"
		});

		tru.Call("AddPluralItems", spiritR, new[]
		{
			//Weapons
			"SerratedClaws",
			"LandscapingShears",
			//Accessories
			"ExplorerTreadsItem"
		});

		tru.Call("AddArmorSetBonusPreview", ModContent.ItemType<BedouinCowl>(), () =>
				Language.GetTextValue("Mods.SpiritReforged.SetBonuses.Bedouin"));

		tru.Call("AddArmorSetBonusPreview", ModContent.ItemType<SunEarrings>(), () =>
				Language.GetTextValue("Mods.SpiritReforged.SetBonuses.Sundancer"));

		tru.Call("AddArmorSetBonusPreview", ModContent.ItemType<BotanistHat>(), () =>
				Language.GetTextValue("Mods.SpiritReforged.SetBonuses.Botanist"));

		tru.Call("AddArmorSetBonusPreview", ModContent.ItemType<GraniteHead>(), () =>
				Language.GetTextValue("Mods.SpiritReforged.SetBonuses.Granite"));

		tru.Call("AddArmorSetBonusPreview", ModContent.ItemType<DriftwoodHelmet>(), () =>
				Language.GetTextValue("Mods.SpiritReforged.SetBonuses.Driftwood"));

		tru.Call("AddArmorSetBonusPreview", ModContent.ItemType<LeatherHood>(), () =>
				Language.GetTextValue("Mods.SpiritReforged.SetBonuses.Marksman"));

		tru.Call("AddArmorSetBonusPreview", ModContent.ItemType<AncientMarksmanHood>(), () =>
				Language.GetTextValue("Mods.SpiritReforged.SetBonuses.Marksman"));

		tru.Call("AddArmorSetBonusPreview", ModContent.ItemType<WayfarerHead>(), () =>
				Language.GetTextValue("Mods.SpiritReforged.SetBonuses.Wayfarer"));

		tru.Call("AddArmorSetBonusPreview", ModContent.ItemType<DrywoodHelmet>(), () =>
				Language.GetTextValue("Mods.SpiritReforged.SetBonuses.Drywood"));

		tru.Call("AddArmorSetBonusPreview", ModContent.ItemType<CascadeHelmet>(), () =>
				Language.GetTextValue("Mods.SpiritReforged.SetBonuses.Cascade"));
	}
}
