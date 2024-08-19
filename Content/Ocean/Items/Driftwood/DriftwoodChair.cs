using SpiritReforged.Common.TileCommon;
using Terraria.DataStructures;
using Terraria.GameContent.ObjectInteractions;

namespace SpiritReforged.Content.Ocean.Items.Driftwood;

public class DriftwoodChair : ModTile
{
	public override void SetStaticDefaults()
	{
		Main.tileFrameImportant[Type] = true;
		Main.tileNoAttach[Type] = true;
		Main.tileLavaDeath[Type] = true;

		TileID.Sets.HasOutlines[Type] = true;
		TileID.Sets.CanBeSatOnForNPCs[Type] = true;
		TileID.Sets.CanBeSatOnForPlayers[Type] = true;
		TileID.Sets.DisableSmartCursor[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style1x2);
		TileObjectData.newTile.Height = 2;
		TileObjectData.newTile.CoordinateHeights = new int[] { 16, 18 };
		TileObjectData.newTile.Direction = TileObjectDirection.PlaceLeft;
		TileObjectData.newTile.StyleWrapLimit = 2; //not really necessary but allows me to add more subtypes of chairs below the example chair texture
		TileObjectData.newTile.StyleMultiplier = 2; //same as above
		TileObjectData.newTile.StyleHorizontal = true;
		TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile);
		TileObjectData.newAlternate.Direction = TileObjectDirection.PlaceRight; //allows me to place example chairs facing the same way as the player
		TileObjectData.addAlternate(1); //facing right will use the second texture style
		TileObjectData.addTile(Type);

		AddToArray(ref TileID.Sets.RoomNeeds.CountsAsChair);
		AddMapEntry(new Color(165, 150, 0), Language.GetText("MapObject.Chair"));
		TileID.Sets.DisableSmartCursor[Type] = true;
		DustType = -1;
		AdjTiles = new int[] { TileID.Chairs };
	}

	public override bool HasSmartInteract(int i, int j, SmartInteractScanSettings settings) => FurnitureHelper.HasSmartInteract(i, j, settings);

	public override void ModifySittingTargetInfo(int i, int j, ref TileRestingInfo info) => FurnitureHelper.ModifySittingTargetInfo(i, j, ref info);

	public override bool RightClick(int i, int j) => FurnitureHelper.RightClick(i, j);

	public override void MouseOver(int i, int j) => FurnitureHelper.MouseOver(i, j, ModContent.ItemType<DriftwoodChairItem>());
}