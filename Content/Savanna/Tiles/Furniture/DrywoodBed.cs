﻿using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.TileCommon.PresetTiles;

namespace SpiritReforged.Content.Savanna.Tiles.Furniture;

public class DrywoodBed : BedTile
{
	public override int CoreMaterial => AutoContent.ItemType<Drywood>();
}
