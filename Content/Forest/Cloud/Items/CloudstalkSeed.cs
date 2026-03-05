namespace SpiritReforged.Content.Forest.Cloud.Items;

public class CloudstalkSeed : ModItem
{
	public override void SetStaticDefaults() => Item.ResearchUnlockCount = 25;

	public override void SetDefaults()
	{
		Item.DefaultToPlaceableTile(ModContent.TileType<CloudstalkTile>());
		Item.width = 22;
		Item.height = 18;
		Item.shopCustomPrice = 1000;
		Item.value = 0;
	}
}