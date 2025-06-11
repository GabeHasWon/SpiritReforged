namespace SpiritReforged.Content.Forest.Misc.Bonsai;

public class SakuraBonsaiItem : ModItem
{
	public virtual int Style => 0;

	public override void SetDefaults()
	{
		Item.DefaultToPlaceableTile(ModContent.TileType<BonsaiTrees>(), Style);
		Item.value = Item.buyPrice(silver: 50);
	}
	public override void AddRecipes() => CreateRecipe().AddIngredient(ModContent.ItemType<BonsaiPot>()).AddIngredient(ItemID.Acorn).Register();
}

public class WillowBonsaiItem : SakuraBonsaiItem
{
	public override int Style => 1;
}

public class PurityBonsaiItem : SakuraBonsaiItem
{
	public override int Style => 2;
}

public class RubyBonsaiItem : SakuraBonsaiItem
{
	public override int Style => 3;
	public override void AddRecipes() => CreateRecipe().AddIngredient(ModContent.ItemType<BonsaiPot>()).AddIngredient(ItemID.GemTreeRubySeed).Register();
}

public class DiamondBonsaiItem : SakuraBonsaiItem
{
	public override int Style => 4;
	public override void AddRecipes() => CreateRecipe().AddIngredient(ModContent.ItemType<BonsaiPot>()).AddIngredient(ItemID.GemTreeDiamondSeed).Register();
}

public class EmeraldBonsaiItem : SakuraBonsaiItem
{
	public override int Style => 5;
	public override void AddRecipes() => CreateRecipe().AddIngredient(ModContent.ItemType<BonsaiPot>()).AddIngredient(ItemID.GemTreeEmeraldSeed).Register();
}

public class SapphireBonsaiItem : SakuraBonsaiItem
{
	public override int Style => 6;
	public override void AddRecipes() => CreateRecipe().AddIngredient(ModContent.ItemType<BonsaiPot>()).AddIngredient(ItemID.GemTreeSapphireSeed).Register();
}

public class TopazBonsaiItem : SakuraBonsaiItem
{
	public override int Style => 7;
	public override void AddRecipes() => CreateRecipe().AddIngredient(ModContent.ItemType<BonsaiPot>()).AddIngredient(ItemID.GemTreeTopazSeed).Register();
}

public class AmethystBonsaiItem : SakuraBonsaiItem
{
	public override int Style => 8;
	public override void AddRecipes() => CreateRecipe().AddIngredient(ModContent.ItemType<BonsaiPot>()).AddIngredient(ItemID.GemTreeAmethystSeed).Register();
}