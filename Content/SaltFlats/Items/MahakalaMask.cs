using SpiritReforged.Common.PlayerCommon;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Content.SaltFlats.Tiles;

namespace SpiritReforged.Content.SaltFlats.Items;

[AutoloadEquip(EquipType.Head)]
public class MahakalaMaskBlue : ModItem
{
	public static readonly HashSet<int> EvilNPCTypes = [NPCID.CorruptBunny, NPCID.CorruptGoldfish, NPCID.Corruptor, NPCID.CorruptPenguin, NPCID.CorruptSlime, 
		NPCID.BigMimicCorruption, NPCID.DesertGhoulCorruption, NPCID.PigronCorruption, NPCID.SandsharkCorrupt, NPCID.Crimera, NPCID.Crimslime, NPCID.CrimsonAxe, 
		NPCID.CursedHammer, NPCID.CrimsonBunny, NPCID.CrimsonGoldfish, NPCID.CrimsonPenguin, NPCID.BigCrimera, NPCID.BigCrimslime, NPCID.BigMimicCrimson, 
		NPCID.DesertGhoulCrimson, NPCID.LittleCrimera, NPCID.PigronCrimson, NPCID.SandsharkCrimson];

	public override void Load() => PlayerEvents.OnModifyHitByNPC += ReduceCorruptionDamage;
	private static void ReduceCorruptionDamage(Player player, NPC npc, ref Player.HurtModifiers modifiers)
	{
		if (EvilNPCTypes.Contains(npc.type))
			modifiers.IncomingDamageMultiplier *= 0.9f;
	}

	public override void SetStaticDefaults() => TileLootSystem.RegisterLoot(static (loot) => loot.AddCommon(ModContent.ItemType<MahakalaMaskBlue>(), 10), ModContent.TileType<StoneStupas>());
	public override void SetDefaults()
	{
		Item.Size = new(20);
		Item.defense = 2;
		Item.rare = ItemRarityID.Blue;
		Item.value = Item.sellPrice(silver: 50);
	}

	public override void AddRecipes() => CreateRecipe().AddRecipeGroup(RecipeGroupID.Wood, 10).AddRecipeGroup("Salt", 10).AddIngredient(ItemID.Sapphire).AddTile(TileID.Anvils).Register();
}

[AutoloadEquip(EquipType.Head)]
public class MahakalaMaskRed : MahakalaMaskBlue
{
	public override void SetStaticDefaults() => TileLootSystem.RegisterLoot(static (loot) => loot.AddCommon(ModContent.ItemType<MahakalaMaskRed>(), 10), ModContent.TileType<StoneStupas>());
	public override void AddRecipes() => CreateRecipe().AddRecipeGroup(RecipeGroupID.Wood, 10).AddRecipeGroup("Salt", 10).AddIngredient(ItemID.Ruby).AddTile(TileID.Anvils).Register();
}