using SpiritReforged.Common.ItemCommon;
using TileHelper;
using TileHelper.Common;
using TileHelper.Content.Tiles;
using static TileHelper.Common.ILightTile;

namespace SpiritReforged.Content.Underground.Tiles.Furniture;

public class CandleSet : ILoadable
{
	public void Load(Mod mod) => ILoadItem.PostAutoloadItems += LoadWax;

	private static void LoadWax() => Autoloader.LoadFurnitureSet(typeof(CandleSet).Namespace + ".Candle", Autoloader.AllArgs(DustID.Bone, new(Color.Orange.ToVector3(), true))
		- nameof(BarrelTile)
		- nameof(BenchTile)
		- nameof(ClockTile)
		- nameof(PianoTile),
		AutoContent.ItemType<WaxBlock>()
	);

	public void Unload() { }
}

public class CandleClock : ClockTile, ILoadItem, ILightTile
{
	public LightingSettings Settings { get; set; } = new LightingSettings(Color.Orange.ToVector3(), true);

	void ILoadItem.AddItemRecipes(ModItem modItem) => DataStructures.Recipes["ClockTile"].Invoke(modItem, AutoContent.ItemType<WaxBlock>());

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

public class CandlePiano : PianoTile, ILoadItem, ILightTile
{
	public LightingSettings Settings { get; set; } = new LightingSettings(Color.Orange.ToVector3(), true);

	void ILoadItem.AddItemRecipes(ModItem modItem) => DataStructures.Recipes["PianoTile"].Invoke(modItem, AutoContent.ItemType<WaxBlock>());

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