using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.TileCommon;

namespace SpiritReforged.Content.Desert.Tiles.Amber;

/// <summary> A placeable amber tile that also generates naturally. </summary>
public class PolishedAmber : ShiningAmber, IAutoloadTileItem
{
	public override void SetStaticDefaults()
	{
		base.SetStaticDefaults();
		this.AutoItem().ResearchUnlockCount = 100;
	}
}