using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Content.Savanna.Tiles;
using TileHelper.Common;
using TileHelper.Content.Tiles;
using static TileHelper.Autoloader;

namespace SpiritReforged.Content.Underground.Tiles.Furniture;

public class CandleSet : ILoadable
{
	public void Load(Mod mod) => ILoadItem.PostAutoloadItems += LoadWaxFurniture;

	private static void LoadWaxFurniture() => LoadFurnitureSet(typeof(CandleSet).Namespace + ".Candle", AllArgs(DustID.Bone, Color.Orange.ToVector3(), distortGlow: true)
		- new BarrelTile()
		- new BenchTile(),
		AutoContent.ItemType<Drywood>());

	public void Unload() { }
}