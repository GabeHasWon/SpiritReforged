using SpiritReforged.Common;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Content.SaltFlats.Tiles;

namespace SpiritReforged.Content.SaltFlats.Items;

[AutoloadEquip(EquipType.Head)]
public class MahakalaMaskBlue : ModItem
{
	public class MahakalaPlayer : ModPlayer
	{
		public bool hasMask;

		public override void ResetEffects() => hasMask = false;

		public override void ModifyHitByNPC(NPC npc, ref Player.HurtModifiers modifiers)
		{
			if (hasMask && SpiritSets.IsCorrupt[npc.type])
				modifiers.IncomingDamageMultiplier *= 0.8f;
		}

		public override void SetStaticDefaults() => TileLootSystem.RegisterLoot(static (loot) => loot.AddOneFromOptions(ModContent.ItemType<MahakalaMaskBlue>(), ModContent.ItemType<MahakalaMaskRed>(), 12), ModContent.TileType<StoneStupas>());
	}

	public override void SetDefaults()
	{
		Item.Size = new(20);
		Item.defense = 2;
		Item.rare = ItemRarityID.Blue;
		Item.value = Item.sellPrice(silver: 50);
	}

	public override void UpdateEquip(Player player) => player.GetModPlayer<MahakalaPlayer>().hasMask = true;
	public override void AddRecipes() => CreateRecipe().AddRecipeGroup(RecipeGroupID.Wood, 10).AddRecipeGroup("Salt", 10).AddIngredient(ItemID.Sapphire).AddTile(TileID.Anvils).Register();
}

[AutoloadEquip(EquipType.Head)]
public class MahakalaMaskRed : MahakalaMaskBlue
{
	public override void AddRecipes() => CreateRecipe().AddRecipeGroup(RecipeGroupID.Wood, 10).AddRecipeGroup("Salt", 10).AddIngredient(ItemID.Ruby).AddTile(TileID.Anvils).Register();
}