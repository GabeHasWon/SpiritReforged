using SpiritReforged.Content.Forest.Cloud.Items;
using SpiritReforged.Content.Ocean.Items.JellyfishStaff;
using SpiritReforged.Content.Savanna.Tiles;
using SpiritReforged.Content.Underground.Items.ExplorerTreads;
using Terraria.DataStructures;

namespace SpiritReforged.Common.ModCompat;

internal class ThoriumGlobalNPC : GlobalNPC
{
	public override bool IsLoadingEnabled(Mod mod) => CrossMod.Thorium.Enabled;

	public override void ModifyNPCLoot(NPC npc, NPCLoot npcLoot)
	{
		if (CrossMod.Thorium.TryFind("RagingMinotaur", out ModNPC minotaur) && npc.type == minotaur.Type)
			npcLoot.AddCommon(ItemID.Leather, 1, 5, 10);

		if (CrossMod.Thorium.TryFind("ManofWar", out ModNPC bigJellyfish) && npc.type == bigJellyfish.Type)
			npcLoot.AddCommon(ModContent.ItemType<JellyfishStaff>(), 50);
	}

	public override void ModifyShop(NPCShop shop)
	{
		if (CrossMod.Thorium.TryFind("Cobbler", out ModNPC cobbler) && shop.NpcType == cobbler.Type)
			shop.Add(ModContent.ItemType<ExplorerTreadsItem>(), Condition.DownedEyeOfCthulhu);

		if (CrossMod.Thorium.TryFind("Druid", out ModNPC druid) && shop.NpcType == druid.Type)
			shop.Add(ModContent.ItemType<CloudstalkSeed>(), Condition.DownedEyeOfCthulhu);
	}
}

internal class ThoriumGlobalTile : GlobalTile
{
	private static int LivingLeafCache = -1;

	public override bool IsLoadingEnabled(Mod mod) => CrossMod.Thorium.Enabled;
	private static bool TryGetLivingLeaf(out int type)
	{
		if (LivingLeafCache != -1)
		{
			type = LivingLeafCache;
			return true;
		}

		else if (CrossMod.Thorium.TryFind("LivingLeaf", out ModItem livingLeaf))
		{
			type = LivingLeafCache = livingLeaf.Type;
			return true;
		}

		type = 0;
		return false;
	}

	public override void KillTile(int i, int j, int type, ref bool fail, ref bool effectOnly, ref bool noItem)
	{
		if (effectOnly || fail)
			return;

		if (type == ModContent.TileType<LivingBaobabLeaf>() && Main.rand.NextBool(3) && TryGetLivingLeaf(out int itemType))
			Item.NewItem(new EntitySource_TileBreak(i, j), new Vector2(i, j).ToWorldCoordinates(), itemType);
	}
}