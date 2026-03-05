using SpiritReforged.Common.ItemCommon;

namespace SpiritReforged.Common.TileCommon.PresetTiles;

public abstract class PlanterBoxTile : ModTile, IAutoloadTileItem
{
	public virtual void SetItemDefaults(ModItem item) => item.Item.value = Item.buyPrice(silver: 1);

	public override void SetStaticDefaults()
	{
		Main.tileFrameImportant[Type] = true;
		Main.tileSolidTop[Type] = true;
		Main.tileSolid[Type] = true;
		Main.tileNoAttach[Type] = true;
		Main.tileTable[Type] = true;

		AddMapEntry(new Color(185, 150, 110));
		DustType = DustID.WoodFurniture;

		this.AutoItem().ResearchUnlockCount = 25;
		PlanterBoxMerge.PlanterTypes.Add(Type);
	}

	public override bool TileFrame(int i, int j, ref bool resetFrame, ref bool noBreak)
	{
		PlanterBoxMerge.Frame(i, j);
		return false;
	}

	public override void RandomUpdate(int i, int j) //Random, wild plant growth
	{
		if (!WorldGen.InWorld(i, j - 1) || !Main.rand.NextBool(3))
			return;

		var t = Main.tile[i, j - 1];

		if (t.TileType == TileID.Plants)
		{
			t.TileType = TileID.Plants2;

			if (Main.netMode != NetmodeID.SinglePlayer)
				NetMessage.SendTileSquare(-1, i, j - 1);
		}
		else if (!t.HasTile)
		{
			t.HasTile = true;
			t.TileType = TileID.Plants;
			t.TileFrameX = GetFrame();
			t.TileFrameY = 0;

			if (Main.netMode != NetmodeID.SinglePlayer)
				NetMessage.SendTileSquare(-1, i, j - 1);
		}

		static short GetFrame()
		{
			int value = Main.rand.Next(6, 45);
			return (short)(((value == 8) ? 9 : value) * 18);
		}
	}
}

/// <summary> Allows <see cref="PlanterBoxTile"/> and vanilla planter boxes to merge. </summary>
public class PlanterBoxMerge : GlobalTile
{
	/// <summary> Includes modded tile types that use <see cref="TileID.PlanterBox"/> or <see cref="TileID.ClayPot"/> behaviour.<para/>
	/// Types are expected to be added during <see cref="ModType.SetStaticDefaults"/>. </summary>
	public static readonly HashSet<int> PlanterTypes = [];

	public override void Load()
	{
		On_WorldGen.CanCutTile += StopCut;
		On_WorldGen.PlaceAlch += ForcePlaceAlch;
	}

	/// <summary> Prevent planted herbs (presumably) from being cut above custom planters, like vanilla does. </summary>
	private static bool StopCut(On_WorldGen.orig_CanCutTile orig, int x, int y, TileCuttingContext context)
	{
		if (Main.tile[x, y + 1] != null && PlanterTypes.Contains(Main.tile[x, y + 1].TileType))
			return false; //Skips orig

		return orig(x, y, context);
	}

	/// <summary> Allow herbs to be placed on custom planters. </summary>
	private static bool ForcePlaceAlch(On_WorldGen.orig_PlaceAlch orig, int x, int y, int style)
	{
		if (Main.tile[x, y + 1] != null && PlanterTypes.Contains(Main.tile[x, y + 1].TileType))
		{
			var tile = Main.tile[x, y];

			tile.HasTile = true;
			tile.TileType = TileID.ImmatureHerbs;
			tile.TileFrameX = (short)(18 * style);
			tile.TileFrameY = 0;

			return true;
		}

		return orig(x, y, style);
	}

	public override bool TileFrame(int i, int j, int type, ref bool resetFrame, ref bool noBreak)
	{
		if ((type is TileID.Plants or TileID.Plants2 || Main.tileAlch[type]) && PlanterTypes.Contains(Framing.GetTileSafely(i, j + 1).TileType))
			noBreak = true; //Prevent vanilla herbs from breaking on custom planters
		else if (type == TileID.PlanterBox)
		{
			Frame(i, j);
			return false;
		}

		return true;
	}

	/// <summary> Mimics <see cref="TileID.PlanterBox"/> framing. </summary>
	public static void Frame(int i, int j)
	{
		var tile = Main.tile[i, j];

		if (Exists(i + 1, j) && Exists(i - 1, j))
			tile.TileFrameX = 18;
		else if (Exists(i - 1, j))
			tile.TileFrameX = 36;
		else if (Exists(i + 1, j))
			tile.TileFrameX = 0;
		else
			tile.TileFrameX = 54;

		static bool Exists(int i, int j) //Checks whether a planter box exists at the given coordinates
		{
			var t = Framing.GetTileSafely(i, j);
			return t.HasTile && (t.TileType == TileID.PlanterBox || TileLoader.GetTile(t.TileType) is PlanterBoxTile);
		}
	}
}