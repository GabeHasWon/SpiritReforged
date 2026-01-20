using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.WorldGeneration;
using SpiritReforged.Content.Ocean.Items;

namespace SpiritReforged.Content.Ocean.Hydrothermal.Tiles;

public class Gravel : ModTile, IAutoloadTileItem
{
	public void AddItemRecipes(ModItem item)
	{
		item.CreateRecipe(10).AddIngredient(ModContent.ItemType<MineralSlag>(), 1).AddTile(TileID.WorkBenches).Register();
		Recipe.Create(ItemID.Cave8Echo, 4).AddIngredient(item.Type).AddTile(TileID.WorkBenches).AddCondition(Condition.InGraveyard).Register();
	}

	public override void SetStaticDefaults()
	{
		Main.tileSolid[Type] = true;
		Main.tileMergeDirt[Type] = true;
		Main.tileBlockLight[Type] = true;

		Main.tileMerge[TileID.Sand][Type] = true; //Ensure sand tries to merge back with gravel
		Main.tileMerge[TileID.HardenedSand][Type] = true;
		TileID.Sets.ChecksForMerge[Type] = true;
		TileID.Sets.CanBeDugByShovel[Type] = true;

		AddMapEntry(new Color(120, 120, 120));
		DustType = DustID.Asphalt;
		MineResist = .5f;
		HitSound = SoundID.Tink;

		Mod.Find<ModItem>(Name + "Item").Item.ResearchUnlockCount = 100;
	}

	public override void RandomUpdate(int i, int j)
	{
		int type = ModContent.TileType<HydrothermalVent>();
		var data = TileObjectData.GetTileData(type, 0);

		if (Main.rand.NextBool(90) && !WorldGen.PlayerLOS(i, j) && WorldMethods.Submerged(i, j - 4, 2, 4))
		{
			if (WorldGen.PlaceTile(i, j - 1, type, true, style: Main.rand.Next(data.RandomStyleRange)))
				NetMessage.SendTileSquare(-1, i, j - data.Height, data.Width, data.Height, TileChangeType.None);
		}
	}

	public override void ModifyFrameMerge(int i, int j, ref int up, ref int down, ref int left, ref int right, ref int upLeft, ref int upRight, ref int downLeft, ref int downRight)
	{
		static void Disallow(ref int side)
		{
			if (side == ModContent.TileType<HydrothermalVent>())
				side = -1;
		}

		WorldGen.TileMergeAttempt(-2, TileID.Sand, ref up, ref down, ref left, ref right, ref upLeft, ref upRight, ref downLeft, ref downRight);
		WorldGen.TileMergeAttempt(-2, TileID.HardenedSand, ref up, ref down, ref left, ref right, ref upLeft, ref upRight, ref downLeft, ref downRight);

		//Only immediately merge with vents on the top
		Disallow(ref down);
		Disallow(ref left);
		Disallow(ref right);
		Disallow(ref upLeft);
		Disallow(ref upRight);
		Disallow(ref downLeft);
		Disallow(ref downRight);

		int vType = ModContent.TileType<HydrothermalVent>();
		WorldGen.TileMergeAttempt(Type, vType, ref up, ref down, ref left, ref right, ref upLeft, ref upRight, ref downLeft, ref downRight);
	}
}