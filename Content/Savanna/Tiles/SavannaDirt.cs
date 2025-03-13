using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.TileCommon.CheckItemUse;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Savanna.Tiles;

public class SavannaDirt : ModTile, IAutoloadTileItem, ICheckItemUse
{
	public void AddItemRecipes(ModItem item)
	{
		item.CreateRecipe().AddIngredient(ItemID.SandBlock).AddIngredient(ItemID.MudBlock).Register();
		Recipe.Create(ItemID.Dirt2Echo, 4).AddIngredient(item.Type).AddTile(TileID.WorkBenches).AddCondition(Condition.InGraveyard).Register();
	}

	public override void SetStaticDefaults()
	{
		Main.tileSolid[Type] = true;
		Main.tileBlockLight[Type] = true;
		Main.tileMerge[TileID.Sand][Type] = true;

		TileID.Sets.ChecksForMerge[Type] = true;

		this.Merge(TileID.Stone, TileID.Dirt, TileID.Mud, TileID.ClayBlock);
		AddMapEntry(new Color(138, 79, 45));
		//NoiseTile.AddTile(this);
		MineResist = .5f;
	}

	public override void ModifyFrameMerge(int i, int j, ref int up, ref int down, ref int left, ref int right, ref int upLeft, ref int upRight, ref int downLeft, ref int downRight)
		=> WorldGen.TileMergeAttempt(-2, TileID.Sand, ref up, ref down, ref left, ref right, ref upLeft, ref upRight, ref downLeft, ref downRight);

	public override void PostTileFrame(int i, int j, int up, int down, int left, int right, int upLeft, int upRight, int downLeft, int downRight)
	{
		const int loop = 4; //Number of horizontal noise frames
		var t = Main.tile[i, j];

		if (Main.rand.NextBool(30) && t.TileFrameX is 18 or 36 or 54 && t.TileFrameY is 18) //Plain center frames
		{
			Point16 result = new(126, 216);
			int random = Main.rand.Next(8);

			t.TileFrameX = (short)(result.X + 18 * (random % loop));
			t.TileFrameY = (short)(result.Y + 18 * (random / loop));
		}
	}

	public bool? CheckItemUse(int type, int i, int j)
	{
		if (type == ItemID.StaffofRegrowth) //Staff of Regrowth functionality
		{
			WorldGen.PlaceTile(i, j, ModContent.TileType<SavannaGrass>(), forced: true);
			NetMessage.SendTileSquare(-1, i, j);
			return true;
		}

		return null;
	}
}