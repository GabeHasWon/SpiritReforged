using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.TileCommon.PresetTiles;
using Terraria.DataStructures;

namespace SpiritReforged.Content.SaltFlats.Tiles;

public sealed class StoneReliquary : ChestTile, ICustomContainer
{
	public override void AddObjectData()
	{
		TileObjectData.newTile.CopyFrom(TileObjectData.Style3x4);
		TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop, TileObjectData.newTile.Width, 0);
		TileObjectData.newTile.StyleHorizontal = true;
		TileObjectData.newTile.DrawYOffset = 2;
		TileObjectData.newTile.HookCheckIfCanPlace = new PlacementHook(Chest.FindEmptyChest, -1, 0, true);
		TileObjectData.newTile.HookPostPlaceMyPlayer = new PlacementHook(Chest.AfterPlacement_Hook, -1, 0, false);
		TileObjectData.newTile.AnchorInvalidTiles = [127];
		TileObjectData.addTile(Type);
	}
}