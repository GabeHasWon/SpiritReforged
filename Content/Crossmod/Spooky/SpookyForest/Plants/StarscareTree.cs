using SpiritReforged.Common.ModCompat;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Crossmod.Spooky.SpookyForest.Plants;

public class StarscareTreeLoader : ILoadable
{
	void ILoadable.Load(Mod mod)
	{
		if (ModLoader.HasMod("Spooky"))
		{
			mod.AddContent(new StarscareTree());
			mod.AddContent(new StarscareTreeGreen());
		}
	}

	void ILoadable.Unload() { }
}

[Autoload(false)]
public class StarscareTree : ModTree
{
	public const string Path = "SpiritReforged/Content/Crossmod/Spooky/SpookyForest/Plants/StarscareTree";

	public override TreePaintingSettings TreeShaderSettings => new()
	{
		UseSpecialGroups = true,
		SpecialGroupMinimalHueValue = 11f / 72f,
		SpecialGroupMaximumHueValue = 0.25f,
		SpecialGroupMinimumSaturationValue = 0.88f,
		SpecialGroupMaximumSaturationValue = 1f
	};

	public override void SetStaticDefaults() => GrowsOnTileId = [ModContent.TileType<OrangeSpookyStargrass>()];

	public override int SaplingGrowthType(ref int style)
	{
		style = 0;
		return ModContent.TileType<StarscareSapling>();
	}

	public override int DropWood()
	{
		CrossMod.Spooky.CheckFind("SpookyWoodItem", out ModItem wood);
		return wood.Type;
	}

	public override Asset<Texture2D> GetTexture() => ModContent.Request<Texture2D>(Path);
	public override Asset<Texture2D> GetBranchTextures() => ModContent.Request<Texture2D>(Path + "Branches");
	public override Asset<Texture2D> GetTopTextures() => ModContent.Request<Texture2D>(Path + "Tops");

	public override void SetTreeFoliageSettings(int i, int j, Tile tile, int xoffset, ref int treeFrame, int floorY, ref int topTextureFrameWidth, ref int topTextureFrameHeight) 
	{
		topTextureFrameWidth = 228;
		topTextureFrameHeight = 136;
	}

	public override int TreeLeaf()
	{
		CrossMod.Spooky.CheckFind(Main.rand.NextBool() ? "LeafOrange" : "LeafRed", out ModGore gore);
		return gore.Type;
	}

	public override int CreateDust() => DustID.WoodFurniture;

	public override bool Shake(int x, int y, ref bool createLeaves)
	{
		createLeaves = true;

		if (Main.rand.NextBool(15) && CrossMod.Spooky.CheckFind("CaramelApple", out ModItem apple))
			Item.NewItem(WorldGen.GetItemSource_FromTreeShake(x, y), new Vector2(x, y) * 16, apple.Type);

		return false;
	}
}

public class StarscareSapling : ModTile
{
	public override bool IsLoadingEnabled(Mod mod) => CrossMod.Spooky.Enabled;

	public override void SetStaticDefaults()
	{
		Main.tileFrameImportant[Type] = true;
		Main.tileNoAttach[Type] = true;
		Main.tileLavaDeath[Type] = true;

		TileID.Sets.SwaysInWindBasic[Type] = true;
		TileID.Sets.CommonSapling[Type] = true;
		TileID.Sets.TreeSapling[Type] = true;

		TileObjectData.newTile.Width = 1;
		TileObjectData.newTile.Height = 2;
		TileObjectData.newTile.Origin = new Point16(0, 1);
		TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile, TileObjectData.newTile.Width, 0);
		TileObjectData.newTile.UsesCustomCanPlace = true;
		TileObjectData.newTile.CoordinateHeights = [16, 18];
		TileObjectData.newTile.CoordinateWidth = 16;
		TileObjectData.newTile.CoordinatePadding = 2;
		TileObjectData.newTile.AnchorValidTiles = [ModContent.TileType<OrangeSpookyStargrass>()];
		TileObjectData.newTile.StyleHorizontal = true;
		TileObjectData.newTile.DrawFlipHorizontal = true;
		TileObjectData.newTile.WaterPlacement = LiquidPlacement.NotAllowed;
		TileObjectData.newTile.LavaDeath = true;
		TileObjectData.newTile.RandomStyleRange = 3;
		TileObjectData.addTile(Type);

		AddMapEntry(new Color(200, 200, 200), CreateMapEntryName());

		AdjTiles = [TileID.Saplings];
		DustType = -1;
		HitSound = SoundID.Dig;
	}

	public override void NumDust(int i, int j, bool fail, ref int num) => num = fail ? 1 : 3;

	public override void RandomUpdate(int i, int j)
	{
		if (WorldGen.genRand.NextBool(20))
		{
			bool isPlayerNear = WorldGen.PlayerLOS(i, j);
			bool success = WorldGen.GrowTree(i, j);

			if (success && isPlayerNear)
				WorldGen.TreeGrowFXCheck(i, j);
		}
	}

	public override void SetSpriteEffects(int i, int j, ref SpriteEffects effects)
	{
		if (i % 2 == 1)
			effects = SpriteEffects.FlipHorizontally;
	}
}