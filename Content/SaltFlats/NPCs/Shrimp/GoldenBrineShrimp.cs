using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.NPCCommon;
using SpiritReforged.Content.SaltFlats.Biome;
using SpiritReforged.Content.Savanna.Biome;
using Terraria.GameContent.Bestiary;

namespace SpiritReforged.Content.SaltFlats.NPCs.Shrimp;

[AutoloadCritter]
public class GoldenBrineShrimp : BrineShrimp, IGoldCritter
{
	public override void AddRecipes() => Recipe.Create(ItemID.GoldenDelight, 1).AddIngredient(this.AutoItemType()).Register();
	public override void CreateItemDefaults() => ItemEvents.CreateItemDefaults(this.AutoItemType(), item => item.value = Item.sellPrice(0, 5, 0, 0));

	public override void SetDefaults()
	{
		NPC.width = 8;
		NPC.height = 8;
		NPC.damage = 0;
		NPC.defense = 0;
		NPC.lifeMax = 5;
		NPC.HitSound = SoundID.NPCHit1;
		NPC.DeathSound = SoundID.NPCDeath1;
		NPC.knockBackResist = .35f;
		NPC.aiStyle = -1;
		NPC.noGravity = true;
		NPC.npcSlots = 0;
		NPC.dontCountMe = true;
		SpawnModBiomes = [ModContent.GetInstance<SavannaBiome>().Type];
	}

	public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry) => bestiaryEntry.AddInfo(this, "");

	public override void HitEffect(NPC.HitInfo hit)
	{
		if (Main.dedServ)
			return;

		for (int i = 0; i < 3; i++)
		{
			short type = Main.rand.NextBool() ? DustID.Gold : DustID.GoldCoin;
			Dust.NewDust(NPC.position, NPC.width, NPC.height, type, 2f * hit.HitDirection, -2f, 0, default, Main.rand.NextFloat(0.75f, 0.95f));
		}
	}
	
	public override float SpawnChance(NPCSpawnInfo spawnInfo) => spawnInfo.Player.InModBiome<SaltBiome>() && spawnInfo.Water ? spawnInfo.PlayerInTown ? 0.008f : 0.002f : 0f;
}