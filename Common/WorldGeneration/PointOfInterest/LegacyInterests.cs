using SpiritReforged.Common.ModCompat;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.WorldGeneration.Chests;
using SpiritReforged.Content.Crossmod.Spooky.SpookyForest;
using SpiritReforged.Content.Crossmod.Spooky.Tiles;
using SpiritReforged.Content.Desert.DragonFossil;
using SpiritReforged.Content.Desert.Tiles;
using SpiritReforged.Content.Forest.Botanist.Tiles;
using SpiritReforged.Content.Forest.Cartography.Pins;
using SpiritReforged.Content.Forest.Graveyard;
using SpiritReforged.Content.Ocean.Items.Blunderbuss;
using SpiritReforged.Content.Ocean.Items.Pearl;
using SpiritReforged.Content.SaltFlats.Tiles.Salt;
using SpiritReforged.Content.Savanna.Tiles;
using Terraria.DataStructures;

namespace SpiritReforged.Common.WorldGeneration.PointOfInterest;

#nullable enable

public class LegacyCuriositiesInterest : InterestDefinition 
{
	private static HashSet<int> CuriosityTypes = [];

	internal override InterestType LegacyInterestType => InterestType.Curiosity;

	public override void SetStaticDefaults()
	{
		string key = "Mods.SpiritReforged.NPCs.Cartographer.Dialogue.Map.Curiosity.";
		DiscoveryText = [Language.GetText(key + "0"), Language.GetText(key + "1"), Language.GetText(key + "2")];
		CuriosityTypes = [ModContent.TileType<BlunderbussTile>(), ModContent.TileType<PearlStringTile>(), ModContent.TileType<SkeletonHand>(), ModContent.TileType<Scarecrow>()];

		if (CrossMod.Spooky.Enabled)
		{
			CuriosityTypes.Add(ModContent.TileType<PumpkinPailTile>());
			CuriosityTypes.Add(ModContent.TileType<ShovelTile>());
			CuriosityTypes.Add(ModContent.TileType<SmackOLanternTile>());
		}
	}

	public override bool IsInterestAt(int x, int y, HashSet<string> placedInterests)
	{
		Tile tile = Main.tile[x, y];

		if (CuriosityTypes.Contains(tile.TileType) && TileObjectData.IsTopLeft(x, y))
			return true;

		if (tile.TileType == ModContent.TileType<AmberFossil>() && TileEntity.ByPosition.TryGetValue(new Point16(x, y), out TileEntity? entity)
			&& entity is FossilEntity fossil && fossil.itemType == ModContent.ItemType<TinyDragon>())
			return true;

		return false;
	}
}

public class FloatingIslandInterest : InterestDefinition
{
	internal override InterestType LegacyInterestType => InterestType.FloatingIsland;
	public override int[] PinTypes => [ModContent.ItemType<PinSky>()];

	public override void SetStaticDefaults()
	{
		string key = "Mods.SpiritReforged.NPCs.Cartographer.Dialogue.Map.FloatingIsland.";
		DiscoveryText = [Language.GetText(key + "0"), Language.GetText(key + "1"), Language.GetText(key + "2")];
	}

	public override bool IsInterestAt(int x, int y, HashSet<string> placedInterests)
	{
		if (TileMethods.TryGetChestID(x, y, out VanillaChestID chestType) && chestType == VanillaChestID.Sky)
			return true;

		return false;
	}
}

public class EnchantedSwordInterest : InterestDefinition
{
	internal override InterestType LegacyInterestType => InterestType.EnchantedSword;
	public override int[] PinTypes => [ModContent.ItemType<PinSword>()];

	public override void SetStaticDefaults()
	{
		string key = "Mods.SpiritReforged.NPCs.Cartographer.Dialogue.Map.EnchantedSword.";
		DiscoveryText = [Language.GetText(key + "0"), Language.GetText(key + "1"), Language.GetText(key + "2")];
	}

	public override bool IsInterestAt(int x, int y, HashSet<string> placedInterests)
	{
		Tile tile = Main.tile[x, y];
		return tile.TileType == TileID.LargePiles2 && tile.TileFrameX == 920 && tile.TileFrameY == 0;
	}
}

public class ButterflyShrineInterest : InterestDefinition
{
	internal override InterestType LegacyInterestType => InterestType.ButterflyShrine;
	public override bool MappedInterest => false;
	public override int[] PinTypes => [ModContent.ItemType<PinButterfly>()];

	public override void SetStaticDefaults()
	{
		string key = "Mods.SpiritReforged.NPCs.Cartographer.Dialogue.Map.ButterflyShrine.";
		DiscoveryText = [Language.GetText(key + "0"), Language.GetText(key + "1"), Language.GetText(key + "2")];
	}
}

public class ShimmerInterest : InterestDefinition
{
	internal override InterestType LegacyInterestType => InterestType.Shimmer;
	public override bool MappedInterest => false;
	public override int[] PinTypes => [ModContent.ItemType<PinFaeling>()];

	public override void SetStaticDefaults()
	{
		string key = "Mods.SpiritReforged.NPCs.Cartographer.Dialogue.Map.Shimmer.";
		DiscoveryText = [Language.GetText(key + "0"), Language.GetText(key + "1"), Language.GetText(key + "2")];
	}
}

public class SavannaInterest : InterestDefinition
{
	internal override InterestType LegacyInterestType => InterestType.Savanna;
	public override int[] PinTypes => [ModContent.ItemType<PinSavanna>()];

	public override void SetStaticDefaults()
	{
		string key = "Mods.SpiritReforged.NPCs.Cartographer.Dialogue.Map.Savanna.";
		DiscoveryText = [Language.GetText(key + "0"), Language.GetText(key + "1"), Language.GetText(key + "2")];
	}

	public override bool IsInterestAt(int x, int y, HashSet<string> placedInterests)
	{
		Tile tile = Main.tile[x, y];
		return tile.TileType == ModContent.TileType<SavannaGrass>() && !placedInterests.Contains(FullName);
	}
}

public class HiveInterest : InterestDefinition
{
	internal override InterestType LegacyInterestType => InterestType.Hive;
	public override int[] PinTypes => [ModContent.ItemType<PinHive>()];

	public override void SetStaticDefaults()
	{
		string key = "Mods.SpiritReforged.NPCs.Cartographer.Dialogue.Map.Hive.";
		DiscoveryText = [Language.GetText(key + "0"), Language.GetText(key + "1"), Language.GetText(key + "2")];
	}

	public override bool IsInterestAt(int x, int y, HashSet<string> placedInterests)
	{
		Tile tile = Main.tile[x, y];
		return tile.TileType == TileID.Larva && tile.TileFrameX == 0 && tile.TileFrameY == 0;
	}
}

public class FablesBunkerInterest : InterestDefinition
{
	internal override InterestType LegacyInterestType => InterestType.WulfrumBunker;
	public override int[] PinTypes => [ModContent.ItemType<PinWulfrum>()];

	public override bool IsAvailable() => CrossMod.Fables.Enabled;

	public override void SetStaticDefaults()
	{
		string key = "Mods.SpiritReforged.NPCs.Cartographer.Dialogue.Map.WulfrumBunker.";
		DiscoveryText = [Language.GetText(key + "0"), Language.GetText(key + "1"), Language.GetText(key + "2")];
	}

	public override bool IsInterestAt(int x, int y, HashSet<string> placedInterests)
	{
		if (!CrossMod.Fables.CheckFind("WulfrumVault", out ModTile fablesVault))
			return false;

		Tile tile = Main.tile[x, y];
		return fablesVault.Type == tile.TileType && TileObjectData.IsTopLeft(x, y);
	}
}

public class BloodAltarInterest : InterestDefinition
{
	internal override InterestType LegacyInterestType => InterestType.BloodAltar;
	public override bool MappedInterest => false;
	public override int[] PinTypes => [ModContent.ItemType<PinBlood>()];

	public override bool IsAvailable() => CrossMod.Thorium.Enabled;

	public override void SetStaticDefaults()
	{
		string key = "Mods.SpiritReforged.NPCs.Cartographer.Dialogue.Map.BloodAltar.";
		DiscoveryText = [Language.GetText(key + "0"), Language.GetText(key + "1"), Language.GetText(key + "2")];
	}
}

public class SaltFlatsInterest : InterestDefinition
{
	internal override InterestType LegacyInterestType => InterestType.SaltFlat;
	public override int[] PinTypes => [ModContent.ItemType<PinSaltFlat>()];

	public override void SetStaticDefaults()
	{
		string key = "Mods.SpiritReforged.NPCs.Cartographer.Dialogue.Map.SaltFlat.";
		DiscoveryText = [Language.GetText(key + "0"), Language.GetText(key + "1"), Language.GetText(key + "2")];
	}

	public override bool IsInterestAt(int x, int y, HashSet<string> placedInterests)
	{
		Tile tile = Main.tile[x, y];
		return tile.TileType == ModContent.TileType<SaltBlockReflective>() && !placedInterests.Contains(FullName);
	}
}

public class ZigguratInterest : InterestDefinition
{
	internal override InterestType LegacyInterestType => InterestType.Ziggurat;
	public override bool MappedInterest => false;
	public override int[] PinTypes => [ModContent.ItemType<PinZiggurat>()];

	public override void SetStaticDefaults()
	{
		string key = "Mods.SpiritReforged.NPCs.Cartographer.Dialogue.Map.Ziggurat.";
		DiscoveryText = [Language.GetText(key + "0"), Language.GetText(key + "1"), Language.GetText(key + "2")];
	}
}

public class SpookyNoseCultInterest : InterestDefinition
{
	internal override InterestType LegacyInterestType => InterestType.Spooky_NoseCult;
	public override bool MappedInterest => false;
	public override int[] PinTypes => [ModContent.ItemType<PinSpookyNose>()];

	public override bool IsAvailable() => CrossMod.Spooky.Enabled;

	public override void SetStaticDefaults()
	{
		string key = "Mods.SpiritReforged.NPCs.Cartographer.Dialogue.Map.Spooky_NoseCult.";
		DiscoveryText = [Language.GetText(key + "0"), Language.GetText(key + "1"), Language.GetText(key + "2")];
	}
}

public class SpookyKrampusWorkshopInterest : InterestDefinition
{
	internal override InterestType LegacyInterestType => InterestType.Spooky_KrampusWorkshop;
	public override bool MappedInterest => false;
	public override int[] PinTypes => [ModContent.ItemType<PinSpookyKrampus>()];

	public override bool IsAvailable() => CrossMod.Spooky.Enabled;

	public override void SetStaticDefaults()
	{
		string key = "Mods.SpiritReforged.NPCs.Cartographer.Dialogue.Map.Spooky_KrampusWorkshop.";
		DiscoveryText = [Language.GetText(key + "0"), Language.GetText(key + "1"), Language.GetText(key + "2")];
	}
}

public class SpookyRottenDepthsInterest : InterestDefinition
{
	internal override InterestType LegacyInterestType => InterestType.Spooky_RottenDepths;
	public override bool MappedInterest => false;
	public override int[] PinTypes => [ModContent.ItemType<PinSpookyRotten>()];

	public override bool IsAvailable() => CrossMod.Spooky.Enabled;

	public override void SetStaticDefaults()
	{
		string key = "Mods.SpiritReforged.NPCs.Cartographer.Dialogue.Map.Spooky_RottenDepths.";
		DiscoveryText = [Language.GetText(key + "0"), Language.GetText(key + "1"), Language.GetText(key + "2")];
	}
}

public class SpookyFetidFarmsInterest : InterestDefinition
{
	internal override InterestType LegacyInterestType => InterestType.Spooky_FetidFarms;
	public override bool MappedInterest => false;
	public override int[] PinTypes => [ModContent.ItemType<PinSpookyFarms>()];

	public override bool IsAvailable() => CrossMod.Spooky.Enabled;

	public override void SetStaticDefaults()
	{
		string key = "Mods.SpiritReforged.NPCs.Cartographer.Dialogue.Map.Spooky_FetidFarms.";
		DiscoveryText = [Language.GetText(key + "0"), Language.GetText(key + "1"), Language.GetText(key + "2")];
	}
}