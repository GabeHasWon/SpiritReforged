namespace SpiritReforged.Content.Ocean.Items.KoiTotem;

public class AncientKoiTotem : ModItem
{
	public override void SetStaticDefaults() => ItemID.Sets.ShimmerTransformToItem[Type] = ModContent.ItemType<KoiTotem>();

	public override void SetDefaults()
	{
		Item.DefaultToPlaceableTile(ModContent.TileType<AncientKoiTotemTile>());
		Item.value = Item.sellPrice(gold: 1);
		Item.rare = ItemRarityID.Blue;
	}
}

public class AncientKoiTotemTile : ModTile
{
	public override void SetStaticDefaults()
	{
		Main.tileFrameImportant[Type] = true;
		Main.tileNoAttach[Type] = true;
		Main.tileLavaDeath[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
		TileObjectData.newTile.Origin = new(0, 1);
		TileObjectData.newTile.CoordinateHeights = [16, 18];
		TileObjectData.newTile.Direction = TileObjectDirection.PlaceRight;
		TileObjectData.newTile.StyleWrapLimit = 2; 
		TileObjectData.newTile.StyleMultiplier = 2; 
		TileObjectData.newTile.StyleHorizontal = true;
		TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile);
		TileObjectData.newAlternate.Direction = TileObjectDirection.PlaceLeft; 
		TileObjectData.addAlternate(1); 
		TileObjectData.addTile(Type);

		DustType = DustID.Ash;
		AddMapEntry(new Color(107, 90, 64), Language.GetText("Mods.SpiritReforged.Tiles.KoiTotemTile.MapEntry"));
	}

	public override void NearbyEffects(int i, int j, bool closer) => ModContent.GetInstance<KoiTotemTile>().NearbyEffects(i, j, closer);
}