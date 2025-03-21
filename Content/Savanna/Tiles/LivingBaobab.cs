﻿using SpiritReforged.Common.TileCommon;

namespace SpiritReforged.Content.Savanna.Tiles;

internal class LivingBaobab : ModTile
{
	public override void SetStaticDefaults()
	{
		Main.tileSolid[Type] = true;
		Main.tileBlockLight[Type] = true;
		Main.tileMerge[ModContent.TileType<LivingBaobabLeaf>()][Type] = true;

		TileID.Sets.ChecksForMerge[Type] = true;
		TileID.Sets.IsSkippedForNPCSpawningGroundTypeCheck[Type] = true;

		RegisterItemDrop(ModContent.ItemType<Items.Drywood.Drywood>());
		AddMapEntry(new Color(142, 125, 106));
		this.Merge(TileID.Sand, TileID.Dirt, ModContent.TileType<SavannaDirt>());

		HitSound = SoundID.Dig;
		DustType = DustID.t_PearlWood;
	}

	public override void ModifyFrameMerge(int i, int j, ref int up, ref int down, ref int left, ref int right, ref int upLeft, ref int upRight, ref int downLeft, ref int downRight)
		=> WorldGen.TileMergeAttempt(-2, ModContent.TileType<LivingBaobabLeaf>(), ref up, ref down, ref left, ref right, ref upLeft, ref upRight, ref downLeft, ref downRight);
}
