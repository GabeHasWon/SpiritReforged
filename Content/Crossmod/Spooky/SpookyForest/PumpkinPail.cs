using SpiritReforged.Common.ItemCommon.Backpacks;
using SpiritReforged.Common.UI.BackpackInterface;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Crossmod.Spooky.SpookyForest;

// [AutoloadEquip(EquipType.Back, EquipType.Front)]
internal class PumpkinPailOrange : BackpackItem
{
	protected virtual int TileStyle => 0;
	public override void SetStaticDefaults() => ItemID.Sets.ShimmerTransformToItem[Type] = ModContent.ItemType<PumpkinPailPurple>();

	public override void SetDefaults() 
	{
		Item.DefaultToPlaceableTile(ModContent.TileType<PumpkinPailTile>(), TileStyle);
		Item.Size = new Vector2(28, 32);
		Item.value = Item.buyPrice(0, 0, 2, 0);
		Item.rare = ItemRarityID.Blue;
		Item.maxStack = 1;

		slotCount = 2;
	}

	public override bool ConsumeItem(Player player) => true;

	public override void RightClick(Player player)
	{
		// Attempt to swap this backpack into the backpack slot
		// This code is adjusted to allow consuming the item normally
		if (!BackpackUISlot.CanClickItem(player.GetModPlayer<BackpackPlayer>().backpack))
			return;

		var oldPack = player.GetModPlayer<BackpackPlayer>().backpack;

		player.GetModPlayer<BackpackPlayer>().backpack = Item.Clone();
		Item.SetDefaults(oldPack.type);
		Item.stack++;
	}
}

// [AutoloadEquip(EquipType.Back, EquipType.Front)]
internal class PumpkinPailPurple : PumpkinPailOrange
{
	protected override int TileStyle => 1;
	public override void SetStaticDefaults() => ItemID.Sets.ShimmerTransformToItem[Type] = ModContent.ItemType<PumpkinPailWhite>();
}

// [AutoloadEquip(EquipType.Back, EquipType.Front)]
internal class PumpkinPailWhite : PumpkinPailOrange
{
	protected override int TileStyle => 2;
	public override void SetStaticDefaults() => ItemID.Sets.ShimmerTransformToItem[Type] = ModContent.ItemType<PumpkinPailOrange>();
}

public class PumpkinPailTile : ModTile
{
	public override void SetStaticDefaults()
	{
		Main.tileSolid[Type] = false;
		Main.tileFrameImportant[Type] = true;
		Main.tileNoFail[Type] = true;

		TileID.Sets.HasOutlines[Type] = true;
		TileID.Sets.CanDropFromRightClick[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
		TileObjectData.newTile.CoordinateHeights = [16, 18];
		TileObjectData.newTile.Origin = new(0, 1);
		TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile, 2, 0);
		TileObjectData.newTile.StyleHorizontal = true;
		TileObjectData.addTile(Type);

		AddMapEntry(new Color(130, 34, 12));

		DustType = DustID.Pumpkin;
	}

	public override bool CreateDust(int i, int j, ref int type)
	{
		int style = Main.tile[i, j].TileFrameX / 36;
		type = style switch
		{
			0 => DustID.Pumpkin,
			1 => DustID.PurpleCrystalShard,
			_ => DustID.BubbleBurst_White,
		};

		return true;
	}
}