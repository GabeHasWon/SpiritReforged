using SpiritReforged.Common.ModCompat;
using SpiritReforged.Common.NPCCommon;
using SpiritReforged.Content.Vanilla.Food;
using Terraria.Audio;
using Terraria.GameContent.Bestiary;

namespace SpiritReforged.Content.Ocean.NPCs.OceanSlime;

[AutoloadBanner]
public class OceanSlime : ModNPC
{
	private const int NumDamagePhases = 3;
	private int DamagePhase => (int)(NumDamagePhases - (float)NPC.life / (NPC.lifeMax / NumDamagePhases));

	public override void SetStaticDefaults()
	{
		Main.npcFrameCount[Type] = 2;
		NPCHelper.ImmuneTo(this, BuffID.Poisoned, BuffID.Venom);
		NPCID.Sets.ShimmerTransformToNPC[Type] = NPCID.ShimmerSlime;

		MoRHelper.AddElement(NPC, MoRHelper.Water);
		MoRHelper.AddNPCToElementList(Type, MoRHelper.NPCType_Slime);
	}

	public override void SetDefaults()
	{
		NPC.width = 22;
		NPC.height = 26;
		NPC.damage = 17;
		NPC.defense = 5;
		NPC.lifeMax = 45;
		NPC.value = 12f;
		NPC.knockBackResist = .45f;
		NPC.aiStyle = 1;

		AIType = NPCID.BlueSlime;
		AnimationType = NPCID.BlueSlime;
	}

	public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry) => bestiaryEntry.AddInfo(this, "Ocean");

	public override void HitEffect(NPC.HitInfo hit)
	{
		if (Main.dedServ)
			return;

		SoundEngine.PlaySound(new SoundStyle("SpiritReforged/Assets/SFX/NPCHit/HardNaturalHit") with { PitchVariance = 0.4f, Pitch = 0.1f, Volume = 1.1f, MaxInstances = 2 }, NPC.Center);

		for (int k = 0; k < 6; k++)
		{
			Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.DynastyWood, hit.HitDirection, -.5f, 0, Color.White, 0.7f);
			Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.DynastyWood, hit.HitDirection, -.5f, 0, default, .34f);
		}

		if (Main.rand.NextBool(10))
			Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("Coconut3").Type, Main.rand.NextFloat(.4f, .7f));

		if (Main.rand.NextBool(16))
			Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, 386, Main.rand.NextFloat(.4f, .7f));

		if (NPC.life <= 0)
		{
			SoundEngine.PlaySound(new SoundStyle("SpiritReforged/Assets/SFX/NPCDeath/Squish") with { Volume = 1.21f }, NPC.Center);
			SoundEngine.PlaySound(SoundID.NPCDeath1 with { Volume = .1f }, NPC.Center);

			for (int k = 0; k < 6; k++)
				Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity * 0.5f, 386, Main.rand.NextFloat(.3f, .8f)); //Plantera leaf gore (no internal ID)

			Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("Coconut1").Type, 1f);
			Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("Coconut2").Type, 1f);
		}
	}

	public override void FindFrame(int frameHeight)
	{
		const int fullWidth = 50; //Excluding padding

		NPC.frame.X = fullWidth * DamagePhase;
		NPC.frame.Width = fullWidth - 2;
		NPC.frame.Height = 34;
	}

	public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
	{
		drawColor = NPC.GetNPCColorTintedByBuffs(drawColor);
		var effects = NPC.direction == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

		spriteBatch.Draw(TextureAssets.Npc[Type].Value, NPC.Center - screenPos + new Vector2(0, NPC.gfxOffY - 2), NPC.frame, drawColor, NPC.rotation, NPC.frame.Size() / 2, NPC.scale, effects, 0);
		return false;
	}

	public override void ModifyNPCLoot(NPCLoot npcLoot)
	{
		npcLoot.AddCommon(ItemID.Gel, 1, 1, 3);
		npcLoot.AddCommon<CoconutMilk>();
		npcLoot.AddCommon(ItemID.SlimeStaff, 10000);
	}
}