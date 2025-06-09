using SpiritReforged.Common.ModCompat;
using SpiritReforged.Common.NPCCommon.Abstract;
using Terraria.GameContent.Bestiary;

namespace SpiritReforged.Content.Ocean.NPCs.ZombieVariants;

public class DiverZombie : ReplaceNPC
{
	public override int[] TypesToReplace => [NPCID.Zombie, NPCID.BaldZombie,
		NPCID.PincushionZombie, NPCID.SwampZombie, NPCID.TwiggyZombie];

	public override void StaticDefaults()
	{
		Main.npcFrameCount[Type] = 4;
		NPCID.Sets.Zombies[Type] = true;
		NPCID.Sets.ShimmerTransformToNPC[NPC.type] = NPCID.Skeleton;

		MoRHelper.AddNPCToElementList(Type, MoRHelper.NPCType_Undead);
		MoRHelper.AddNPCToElementList(Type, MoRHelper.NPCType_Humanoid);
		MoRHelper.AddNPCToElementList(Type, MoRHelper.NPCType_Wet);
	}

	public override void SetDefaults()
	{
		NPC.width = 28;
		NPC.height = 42;
		NPC.damage = 16;
		NPC.defense = 6;
		NPC.lifeMax = 40;
		NPC.HitSound = SoundID.NPCHit1;
		NPC.DeathSound = SoundID.NPCDeath2;
		NPC.value = 40f;
		NPC.knockBackResist = .45f;
		NPC.aiStyle = 3;
		AIType = NPCID.Zombie;
		Banner = Item.NPCtoBanner(NPCID.Zombie);
		BannerItem = Item.BannerToItem(Banner);
	}

	public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry) => bestiaryEntry.AddInfo(this, "NightTime Ocean Moon");

	public override void HitEffect(NPC.HitInfo hit)
	{
		for (int k = 0; k < 20; k++)
		{
			Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Blood, 2.5f * hit.HitDirection, -2.5f, 0, Color.White, 0.78f);
			Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Blood, 2.5f * hit.HitDirection, -2.5f, 0, default, .54f);
		}

		if (NPC.life <= 0 && Main.netMode != NetmodeID.Server)
			for (int i = 1; i < 4; ++i)
				Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("DiverZombie" + i).Type, 1f);
	}

	private int frameTimer;
	private int frame;

	public override void AI()
	{
		NPC.spriteDirection = NPC.direction;
		frameTimer++;
		if (NPC.wet)
		{
			NPC.noGravity = true;
			NPC.velocity.Y *= .9f;
			NPC.velocity.Y -= .09f;
			NPC.velocity.X *= .95f;
			NPC.rotation = NPC.velocity.X * .1f;
			if (frameTimer >= 50)
			{
				frame++;
				frameTimer = 0;
			}

			if (frame is > 3 or < 2)
				frame = 2;
		}
		else
		{
			NPC.noGravity = false;
			if (NPC.velocity.Y != 0)
				frame = 2;
			else
			{
				if (frameTimer >= 12)
				{
					frame++;
					frameTimer = 0;
				}

				if (frame > 2)
					frame = 0;
			}
		}
	}

	public override void FindFrame(int frameHeight)
	{
		if (NPC.IsABestiaryIconDummy)
			if (++frameTimer >= 10)
			{
				frameTimer = 0;
				frame = ++frame % 3;
			}

		NPC.frame.Y = frameHeight * frame;
	}

	public override void ModifyNPCLoot(NPCLoot npcLoot)
	{
		npcLoot.AddCommon(ItemID.Shackle, 50);
		npcLoot.AddCommon(ItemID.ZombieArm, 250);
		npcLoot.AddCommon(ItemID.Flipper, 100);
		npcLoot.AddOneFromOptions(65, ModContent.ItemType<Items.Vanity.DiverSet.DiverLegs>(),
			ModContent.ItemType<Items.Vanity.DiverSet.DiverHead>(), ModContent.ItemType<Items.Vanity.DiverSet.DiverBody>());
	}

	public override bool CanSpawn(Player player) => player.ZoneBeach;
}