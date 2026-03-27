using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.TileCommon.PresetTiles;
using System.Linq;
using Terraria.DataStructures;

namespace SpiritReforged.Content.SaltFlats.Tiles;

public sealed class StoneReliquary : ChestTile, ICustomContainer
{
	public override void StaticDefaults()
	{
		base.StaticDefaults();
		DustType = DustID.Stone;
	}

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

		TileID.Sets.BasicChest[Type] = false;
		TileID.Sets.AvoidedByNPCs[Type] = true;
		TileID.Sets.InteractibleByNPCs[Type] = true;

		AddMapEntry(new Color(99, 99, 99), Language.GetText("Mods.SpiritReforged.Items.StoneReliquaryItem.DisplayName"));
	}

	//public override LocalizedText DefaultContainerName(int frameX, int frameY) => Language.GetText("Mods.SpiritReforged.Items.StoneReliquaryItem.DisplayName");

	public override void AnimateIndividualTile(int type, int i, int j, ref int frameXOffset, ref int frameYOffset)
	{
		const int fullFrameHeight = 72;

		if (HasChest(i, j, out Chest chest))
			frameYOffset = fullFrameHeight * chest.frame;
	}

	//Prevents this multitile chest from being destroyed when full
	public override bool CanKillTile(int i, int j, ref bool blockDamaged)
	{
		if (HasChest(i, j, out Chest chest))
			return chest.item.All(x => x is not Item item || item.type == ItemID.None || item.stack == 0);

		return true;
	}

	public static bool HasChest(int i, int j, out Chest chest)
	{
		TileExtensions.GetTopLeft(ref i, ref j);
		if (Chest.FindChest(i, j) is int search && search != -1)
		{
			chest = Main.chest[search];
			return true;
		}

		chest = null;
		return false;
	}
}