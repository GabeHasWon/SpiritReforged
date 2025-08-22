using SpiritReforged.Common.PlayerCommon;
using System.Linq;
using Terraria.DataStructures;
using Terraria.WorldBuilding;

namespace SpiritReforged.Common.WorldGeneration.Microbiomes.Biomes;

public class OasisBiome : Microbiome
{
	public static bool InOasis(Player p)
	{
		const string flagType = "Oasis";

		if (p.CheckFlag(flagType) is bool flag)
			return flag;

		//Preface with basic relevant checks so linq isn't constantly running in the background
		bool result = p.ZoneDesert && p.ZoneOverworldHeight && MicrobiomeSystem.Microbiomes.Any(x => x is OasisBiome o && o.Rectangle.Contains(p.Center.ToTileCoordinates()));
		p.SetFlag(flagType, result); //Cache the result to avoid checking against this logic more than once per tick

		return result;
	}

	public static readonly Point16 Size = new(80, 40);
	public Rectangle Rectangle => new(Position.X - Size.X / 2, Position.Y - Size.Y / 2, Size.X, Size.Y);

	#region detours
	public override void Load()
	{
		PlayerEvents.OnPostUpdateEquips += HealInSprings;
		On_WorldGen.PlaceOasis += MapOasis;
	}

	private static void HealInSprings(Player player)
	{
		if (player.wet && InOasis(player))
			player.AddBuff(BuffID.Regeneration, 180);
	}

	private static bool MapOasis(On_WorldGen.orig_PlaceOasis orig, int X, int Y)
	{
		bool value = orig(X, Y);

		if (value)
		{
			int index = GenVars.numOasis - 1;
			if (index < GenVars.oasisPosition.Length)
				Create<OasisBiome>(new(GenVars.oasisPosition[index]), false);
		}

		return value;
	}
	#endregion

	protected override void OnPlace(Point16 point) { }
}