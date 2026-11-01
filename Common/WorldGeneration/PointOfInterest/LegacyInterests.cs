using SpiritReforged.Common.ModCompat;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.WorldGeneration.Chests;
using SpiritReforged.Content.SaltFlats.Tiles.Salt;
using SpiritReforged.Content.Savanna.Tiles;
using System.Numerics;

namespace SpiritReforged.Common.WorldGeneration.PointOfInterest;

//public enum InterestType : byte
//{
//	Curiosity,
//	Spooky_NoseCult,
//	Spooky_KrampusWorkshop,
//	Spooky_RottenDepths,
//	Spooky_FetidFarms,
//}

// Most legacy interests in this file will not be MappedInterests as they are already defined elsewhere, and largely don't use "if tile @ position == type".
// 

public class FloatingIslandInterest : InterestDefinition
{
	internal override InterestType LegacyInterestType => InterestType.FloatingIsland;

	public override bool IsInterestAt(int x, int y, HashSet<Type> placedInterests)
	{
		if (TileMethods.TryGetChestID(x, y, out VanillaChestID chestType))
		{
			if (chestType == VanillaChestID.Sky)
				return true;
		}

		return false;
	}
}

public class EnchantedSwordInterest : InterestDefinition
{
	internal override InterestType LegacyInterestType => InterestType.EnchantedSword;

	public override bool IsInterestAt(int x, int y, HashSet<Type> placedInterests)
	{
		Tile tile = Main.tile[x, y];
		return tile.TileType == TileID.LargePiles2 && tile.TileFrameX == 920 && tile.TileFrameY == 0;
	}
}

public class ButterflyShrineInterest : InterestDefinition
{
	internal override InterestType LegacyInterestType => InterestType.ButterflyShrine;
	internal override bool MappedInterest => false;
}

public class ShimmerInterest : InterestDefinition
{
	internal override InterestType LegacyInterestType => InterestType.Shimmer;
	internal override bool MappedInterest => false;
}

public class SavannaInterest : InterestDefinition
{
	internal override InterestType LegacyInterestType => InterestType.Savanna;

	public override bool IsInterestAt(int x, int y, HashSet<Type> placedInterests)
	{
		Tile tile = Main.tile[x, y];
		return tile.TileType == ModContent.TileType<SavannaGrass>() && !placedInterests.Contains(typeof(SavannaInterest));
	}
}

public class HiveInterest : InterestDefinition
{
	internal override InterestType LegacyInterestType => InterestType.Hive;

	public override bool IsInterestAt(int x, int y, HashSet<Type> placedInterests)
	{
		Tile tile = Main.tile[x, y];
		return tile.TileType == TileID.Larva && tile.TileFrameX == 0 && tile.TileFrameY == 0;
	}
}

public class FablesBunkerInterest : InterestDefinition
{
	internal override InterestType LegacyInterestType => InterestType.WulfrumBunker;

	public override bool IsInterestAt(int x, int y, HashSet<Type> placedInterests)
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
	internal override bool MappedInterest => false;
}

public class SaltFlatsInterest : InterestDefinition
{
	internal override InterestType LegacyInterestType => InterestType.SaltFlat;

	public override bool IsInterestAt(int x, int y, HashSet<Type> placedInterests)
	{
		Tile tile = Main.tile[x, y];
		return tile.TileType == ModContent.TileType<SaltBlockReflective>() && !placedInterests.Contains(typeof(SaltFlatsInterest));
	}
}

public class ZigguratInterest : InterestDefinition
{
	internal override InterestType LegacyInterestType => InterestType.Ziggurat;
	internal override bool MappedInterest => false;
}