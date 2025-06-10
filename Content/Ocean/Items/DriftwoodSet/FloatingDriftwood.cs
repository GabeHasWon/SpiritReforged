using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.ItemCommon.FloatingItem;
using SpiritReforged.Content.Ocean.Tiles;

namespace SpiritReforged.Content.Ocean.Items.DriftwoodSet;

/// <summary> The naturally spawning variant of our driftwood decoration items, and the player's source of driftwood blocks. </summary>
public class FloatingDriftwood : FloatingItem
{
	public override float SpawnWeight => base.Weight * 1.2f;
	public override float Weight => base.Weight * 0.9f;
	public override float Bouyancy => base.Bouyancy * 1.05f;
	public override string Texture => base.Texture.Replace("Floating", string.Empty);

	public override void SetStaticDefaults() => VariantGlobalItem.AddVariants(Type, 3);
	public override void SetDefaults()
	{
		Item.width = 30;
		Item.height = 18;
		Item.rare = ItemRarityID.White;
		Item.maxStack = 1;
	}

	public override bool OnPickup(Player player)
	{
		int stack = VariantGlobalItem.GetVariant(Item) switch
		{
			1 => 20,
			2 => 25,
			_ => 10
		};

		player.QuickSpawnItem(player.GetSource_OpenItem(Item.type, "Pickup"), AutoContent.ItemType<Driftwood>(), stack);
		return false;
	}
}
