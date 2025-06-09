using SpiritReforged.Content.Forest.Cloud.Items;
using SpiritReforged.Content.Forest.Misc.Remedy;
using SpiritReforged.Content.Savanna.Items.Gar;

namespace SpiritReforged.Content.Underground.Tiles.Potion;

internal static class PotionColorDatabase
{
	internal static readonly Dictionary<int, Color> NaturalBrewColors = new()
	{
		{ ItemID.GravitationPotion, Color.Purple },
		{ ItemID.FeatherfallPotion, new Color(34, 194, 246) },
		{ ItemID.BattlePotion, new Color(127, 96, 180) },
		{ ItemID.CalmingPotion, new Color(102, 101, 201) },
		{ ItemID.EndurancePotion, new Color(185, 185, 170) },
		{ ItemID.TrapsightPotion, new Color(250, 105, 30) },
		{ ItemID.HunterPotion, new Color(250, 120, 34) },
		{ ItemID.ShinePotion, new Color(222, 230, 10) },
		{ ItemID.MiningPotion, new Color(105, 170, 170) },
		{ ItemID.SpelunkerPotion, new Color(225, 185, 22) },
		{ ItemID.SwiftnessPotion, Color.LightSeaGreen },
		{ ItemID.WrathPotion, new Color(216, 73, 63) },
		{ ItemID.ObsidianSkinPotion, new Color(90, 72, 168) },
		{ ModContent.ItemType<DoubleJumpPotion>(), new Color(147, 132, 207) },
		{ ItemID.LuckPotion, new Color(41, 60, 70) },
		{ ItemID.IronskinPotion, new Color(230, 222, 10) },
		{ ItemID.LifeforcePotion, new Color(250, 64, 188) }
	};

	internal static readonly Dictionary<int, Color> DecorativeBrewColors = new()
	{
		{ ItemID.AmmoReservationPotion, new Color(217, 216, 167) },
		{ ItemID.ArcheryPotion, new Color(209, 145, 67) },
		{ ItemID.BiomeSightPotion, new Color(247, 118, 168) },
		{ ItemID.BuilderPotion, new Color(128, 105, 79) },
		{ ItemID.CratePotion, new Color(199, 157, 107) },
		{ ItemID.FishingPotion, new Color(104, 240, 149) },
		{ ItemID.FlipperPotion, new Color(91, 181, 245) },
		{ ItemID.GillsPotion, new Color(63, 106, 204) },
		{ ItemID.LuckPotionGreater, new Color(237, 49, 172) },
		{ ItemID.LuckPotionLesser, new Color(242, 246, 255) },
		{ ItemID.HeartreachPotion, new Color(255, 0, 120) },
		{ ItemID.InfernoPotion, new Color(255, 196, 85) },
		{ ItemID.InvisibilityPotion, Color.White * 0.15f },
		{ ItemID.MagicPowerPotion, new Color(121, 4, 181) },
		{ ItemID.ManaRegenerationPotion, new Color(255, 140, 248) },
		{ ItemID.NightOwlPotion, new Color(126, 235, 113) },
		{ ItemID.RagePotion, new Color(242, 221, 86) },
		{ ItemID.RegenerationPotion, new Color(194, 60, 187) },
		{ ItemID.SonarPotion, new Color(113, 224, 51) },
		{ ItemID.SummoningPotion, new Color(183, 242, 95) },
		{ ItemID.ThornsPotion, new Color(192, 232, 163) },
		{ ItemID.TitanPotion, new Color(80, 161, 86) },
		{ ItemID.WarmthPotion, new Color(255, 236, 185) },
		{ ItemID.WaterWalkingPotion, new Color(92, 134, 240) },
		{ ItemID.StinkPotion, new Color(114, 158, 19) },
		{ ItemID.LovePotion, Color.Red },
		{ ItemID.TeleportationPotion, new Color(147, 0, 240) },
		{ ItemID.RecallPotion, new Color(10, 177, 209) },
		{ ItemID.GenderChangePotion, new Color(188, 183, 222) },
		{ ItemID.WormholePotion, new Color(37, 201, 252) },
		{ ModContent.ItemType<RemedyPotion>(), new Color(184, 255, 195) },
		{ ModContent.ItemType<QuenchPotion>(), new Color(129, 255, 245) },
		{ ModContent.ItemType<DoubleJumpPotion>(), new Color(147, 132, 207) },
	};

	public static void RegisterColor(int item, Color color, bool decorative) 
	{
		if (decorative)
			DecorativeBrewColors.Add(item, color);
		else
			NaturalBrewColors.Add(item, color);
	}

	public static bool ParseNewPotion(params object[] args)
	{
		if (args.Length < 3)
			throw new ArgumentException("AddPotionVat requires int, Color, bool arguments!");

		int value;

		if (args[0] is int intVal)
			value = intVal;
		else if (args[0] is short shortVal)
			value = shortVal;
		else if (args[0] is ushort ushortVal)
			value = ushortVal;
		else
			throw new ArgumentException("AddPotionVat parameter 0 should be an int, short or ushort!");

		if (args[1] is not Color color)
			throw new ArgumentException("AddPotionVat parameter 1 should be a Color!");

		if (args[2] is not bool decor)
			throw new ArgumentException("AddPotionVat parameter 2 should be a bool!");

		RegisterColor(value, color, decor);
		return true;
	}
}
