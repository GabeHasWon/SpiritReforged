using SpiritReforged.Common.ItemCommon;
using TileHelper.Common;
using TileHelper.Content.Tiles;
using static TileHelper.Autoloader;
using static TileHelper.Common.ILightTile;

namespace SpiritReforged.Content.Underground.Tiles.Furniture;

public class CandleSet : ILoadable
{
	public void Load(Mod mod) => ILoadItem.PostAutoloadItems += LoadWaxFurniture;

	private static void LoadWaxFurniture() => LoadFurnitureSet(typeof(CandleSet).Namespace + ".Candle", AllArgs(DustID.Bone, new(Color.Orange.ToVector3(), true))
		- nameof(BarrelTile)
		- nameof(BenchTile)
		- nameof(ClockTile)
		- nameof(PianoTile),
		AutoContent.ItemType<WaxBlock>(), false //Don't load items until assets are implemented
	);

	public void Unload() { }
}

public class CandleClock : ClockTile/*, ILoadItem*/, ILightTile
{
	public LightingSettings Settings { get; set; } = new LightingSettings(Color.Orange.ToVector3(), true);

	public override void SetStaticDefaults()
	{
		base.SetStaticDefaults();

		Main.tileLighted[Type] = true;
		TileHelperSets.TileGlowmask[Type] = Helpers.RequestGlowmask(this);
	}

	public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
	{
		Tile tile = Main.tile[i, j];

		if (tile.TileFrameY == 0)
		{
			Vector3 light = Settings.Light;
			(r, g, b) = (light.X, light.Y, light.Z);
		}
	}

	/// <inheritdoc/>
	public override void PostDraw(int i, int j, SpriteBatch spriteBatch) => PostDrawGlowmask(spriteBatch, i, j, Settings.Distorted);
}

public class CandlePiano : PianoTile/*, ILoadItem*/, ILightTile
{
	public LightingSettings Settings { get; set; } = new LightingSettings(Color.Orange.ToVector3(), true);

	public override void SetStaticDefaults()
	{
		base.SetStaticDefaults();

		Main.tileLighted[Type] = true;
		TileHelperSets.TileGlowmask[Type] = Helpers.RequestGlowmask(this);
	}

	public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
	{
		Tile tile = Main.tile[i, j];

		if (tile.TileFrameY == 0)
		{
			Vector3 light = Settings.Light;
			(r, g, b) = (light.X, light.Y, light.Z);
		}
	}

	/// <inheritdoc/>
	public override void PostDraw(int i, int j, SpriteBatch spriteBatch) => PostDrawGlowmask(spriteBatch, i, j, Settings.Distorted);
}