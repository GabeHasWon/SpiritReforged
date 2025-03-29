using Terraria.DataStructures;
using Terraria.GameContent.Metadata;
using SpiritReforged.Common.TileCommon.Tree;
using System.Linq;
using System.Reflection;

namespace SpiritReforged.Common.TileCommon.PresetTiles;

/// <summary> Simplifies building a sapling tile by automatically setting common data. See <see cref="SaplingTile{T}"/> for <see cref="CustomTree"/>s. </summary>
public abstract class SaplingTile : ModTile
{
	public override void SetStaticDefaults()
	{
		Main.tileFrameImportant[Type] = true;
		Main.tileNoAttach[Type] = true;
		Main.tileLavaDeath[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style1x2);
		TileObjectData.newTile.Width = 1;
		TileObjectData.newTile.Height = 2;
		TileObjectData.newTile.Origin = new Point16(0, 1);
		TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile, TileObjectData.newTile.Width, 0);
		TileObjectData.newTile.UsesCustomCanPlace = true;
		TileObjectData.newTile.CoordinateHeights = [16, 18];
		TileObjectData.newTile.CoordinateWidth = 16;
		TileObjectData.newTile.CoordinatePadding = 2;
		//TileObjectData.newTile.AnchorValidTiles = AnchorTiles;
		TileObjectData.newTile.StyleHorizontal = true;
		TileObjectData.newTile.DrawFlipHorizontal = true;
		TileObjectData.newTile.WaterPlacement = LiquidPlacement.NotAllowed;
		TileObjectData.newTile.LavaDeath = true;
		TileObjectData.newTile.RandomStyleRange = 3;
		TileObjectData.newTile.StyleMultiplier = 3;
		PreAddObjectData();
		TileObjectData.addTile(Type);

		TileID.Sets.TreeSapling[Type] = true; //Will break on tile update if this is true
		TileID.Sets.CommonSapling[Type] = true;
		TileID.Sets.SwaysInWindBasic[Type] = true;
		TileMaterials.SetForTileId(Type, TileMaterials._materialsByName["Plant"]);

		AddMapEntry(new Color(170, 120, 100), Language.GetText("MapObject.Sapling"));

		DustType = DustID.WoodFurniture;
		AdjTiles = [TileID.Saplings];
	}

	/// <summary> Called before <see cref="TileObjectData.addTile"/> is automatically called in <see cref="SetStaticDefaults"/>.<br/>
	/// Use this to modify object data without needing to override SetStaticDefaults.<para/>
	/// <see cref="TileObjectData.AnchorValidTiles"/> must be set here for the sapling to work. </summary>
	public abstract void PreAddObjectData();
	public override void NumDust(int i, int j, bool fail, ref int num) => num = fail ? 1 : 3;

	public override void RandomUpdate(int i, int j)
	{
		if (Main.rand.NextBool(8) && WorldGen.GrowTree(i, j) && WorldGen.PlayerLOS(i, j))
			WorldGen.TreeGrowFXCheck(i, j);
	}

	public override void SetSpriteEffects(int i, int j, ref SpriteEffects effects)
	{
		if (i % 2 == 0)
			effects = SpriteEffects.FlipHorizontally;
	}
}

/// <typeparam name="T"> The type of <see cref="CustomTree"/> that this sapling grows into. </typeparam>
public abstract class SaplingTile<T> : SaplingTile where T : CustomTree
{
	public abstract int[] AnchorTypes { get; }

	public override void SetStaticDefaults()
	{
		base.SetStaticDefaults();
		CustomSapling.SaplingToCustomTree.Add(Type, ModContent.GetInstance<T>().Type);
	}
	/// <summary> Called before <see cref="TileObjectData.addTile"/> is automatically called in <see cref="SetStaticDefaults"/>.<br/>
	/// Use this to modify object data without needing to override SetStaticDefaults.</summary>
	public override void PreAddObjectData() => TileObjectData.newTile.AnchorValidTiles = AnchorTypes;
	public override void RandomUpdate(int i, int j)
	{
		if (Main.rand.NextBool(8))
			CustomTree.GrowTree<T>(i, j);
	}
}

public static class CustomSapling
{
	/// <summary> Stores custom saplings and their associated tree types. </summary>
	public static readonly Dictionary<ushort, ushort> SaplingToCustomTree = [];
	/// <summary> Stores tile anchors for custom trees. </summary>
	public static readonly HashSet<ushort> CustomAnchorTypes = [];

	/// <summary> Autoloads <see cref="CustomModTree"/>s for each <see cref="SaplingTile{T}"/> in the mod. Ensure that this is called after all required tiles are loaded. </summary>
	public static void Autoload(Mod mod)
	{
		var saplings = mod.GetContent<SaplingTile>().ToArray();

		for (int i = saplings.Length - 1; i >= 0; i--)
		{
			var c = saplings[i];

			//Use reflection because we can't infer generic type here
			if (c.GetType().GetProperty("AnchorTypes", BindingFlags.Instance | BindingFlags.Public)?.GetValue(c) is int[] anchors)
			{
				mod.AddContent(new CustomModTree(c.Type, anchors));

				foreach (int type in anchors)
					CustomAnchorTypes.Add((ushort)type);
			}
		}
	}
}