using SpiritReforged.Common.ModCompat;
using SpiritReforged.Common.NPCCommon.Abstract;
using SpiritReforged.Content.Vanilla.Food;
using Terraria.GameContent.Bestiary;

namespace SpiritReforged.Content.Desert.NPCs.ZombieVariants;

public class VeiledZombie : ModNPC, ISubstitute
{
	public int[] TypesToReplace => [NPCID.Zombie, NPCID.BaldZombie, NPCID.FemaleZombie];
	private float _frameCounter;

	public override void SetStaticDefaults()
	{
		Main.npcFrameCount[Type] = Main.npcFrameCount[NPCID.Zombie];
		NPCID.Sets.Zombies[Type] = true;
		NPCID.Sets.ShimmerTransformToNPC[Type] = NPCID.Skeleton;

		MoRHelper.AddNPCToElementList(Type, MoRHelper.NPCType_Undead);
		MoRHelper.AddNPCToElementList(Type, MoRHelper.NPCType_Humanoid);
	}

	public override void SetDefaults()
	{
		NPC.width = 32;
		NPC.height = 42;
		NPC.damage = 14;
		NPC.defense = 8;
		NPC.lifeMax = 45;
		NPC.HitSound = SoundID.NPCHit1;
		NPC.DeathSound = SoundID.NPCDeath2;
		NPC.value = 90f;
		NPC.knockBackResist = .6f;
		NPC.aiStyle = NPCAIStyleID.Fighter;
		AIType = NPCID.Zombie;
		AnimationType = NPCID.Zombie;
		Banner = Item.NPCtoBanner(NPCID.Zombie);
		BannerItem = Item.BannerToItem(Banner);
	}

	public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry) => bestiaryEntry.AddInfo(this, "NightTime Desert");

	public override void HitEffect(NPC.HitInfo hit)
	{
		for (int k = 0; k < 20; k++)
		{
			Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Blood, 2.5f * hit.HitDirection, -2.5f, 0, Color.White, 0.78f);
			Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Blood, 2.5f * hit.HitDirection, -2.5f, 0, default, .54f);
		}

		if (NPC.life <= 0 && Main.netMode != NetmodeID.Server)
			for (int i = 1; i < 4; ++i)
				Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("VeiledZombie" + i).Type, 1f);
	}

	public override void FindFrame(int frameHeight)
	{
		if (NPC.IsABestiaryIconDummy)
		{
			_frameCounter += .1f;
			_frameCounter %= Main.npcFrameCount[Type];

			NPC.frame.Y = frameHeight * (int)_frameCounter;
		}
	}

	public override void ModifyNPCLoot(NPCLoot npcLoot)
	{
		npcLoot.AddCommon(ItemID.Shackle, 50);
		npcLoot.AddCommon(ItemID.ZombieArm, 250);
	}

	public bool CanSubstitute(Player player) => player.ZoneDesert;
}