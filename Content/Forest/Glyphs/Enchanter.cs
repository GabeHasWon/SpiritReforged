using SpiritReforged.Common.NPCCommon.Abstract;
using SpiritReforged.Common.NPCCommon.Interfaces;
using SpiritReforged.Common.UI.Enchantment;
using SpiritReforged.Common.UI.System;
using SpiritReforged.Content.Forest.Cartography;
using Terraria.GameContent.Bestiary;

namespace SpiritReforged.Content.Forest.Glyphs;

[AutoloadHead]
public class Enchanter : WorldNPC, ITravelNPC
{
	public static readonly Dictionary<int, int> ValueByType = [];
	//public override void Load() => AutoEmote.LoadFaceEmote(this, static () => NPC.AnyNPCs(ModContent.NPCType<Enchanter>()));

	public override void SetStaticDefaults()
	{
		base.SetStaticDefaults();

		Main.npcFrameCount[Type] = 4;

		NPCID.Sets.ExtraFramesCount[Type] = 9;
		NPCID.Sets.AttackFrameCount[Type] = 4;
		NPCID.Sets.DangerDetectRange[Type] = 600;
		NPCID.Sets.AttackType[Type] = -1;
		NPCID.Sets.AttackTime[Type] = 20;
		NPCID.Sets.HatOffsetY[Type] = 2;
		NPCID.Sets.IsTownChild[Type] = true;

		NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, new NPCID.Sets.NPCBestiaryDrawModifiers()
		{ Velocity = 1f });
	}

	public override void SetDefaults()
	{
		NPC.CloneDefaults(NPCID.SkeletonMerchant);
		NPC.HitSound = SoundID.NPCHit1;
		NPC.DeathSound = SoundID.NPCDeath1;
		NPC.Size = new Vector2(30, 40);
	}

	public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry) => bestiaryEntry.AddInfo(this, "Surface");

	public override string GetChat() => Language.GetTextValue("Mods.SpiritReforged.NPCs.Cartographer.Dialogue." + Main.rand.Next(5));

	public override List<string> SetNPCNameList()
	{
		List<string> names = [];

		for (int i = 0; i < 6; ++i)
			names.Add(Language.GetTextValue("Mods.SpiritReforged.NPCs.Cartographer.Names." + i));

		return names;
	}

	public override void SetChatButtons(ref string button, ref string button2) => button = "Enchant";

	public override void OnChatButtonClicked(bool firstButton, ref string shopName)
	{
		if (firstButton)
		{
			Main.playerInventory = true;
			UISystem.SetActive<EnchanterUI>();
		}
	}

	public override void HitEffect(NPC.HitInfo hit)
	{
		if (Main.dedServ)
			return;

		if (NPC.life <= 0)
			for (int i = 1; i < 7; i++)
			{
				int goreType = Mod.Find<ModGore>(nameof(Cartographer) + i).Type;
				Gore.NewGore(NPC.GetSource_Death(), Main.rand.NextVector2FromRectangle(NPC.getRect()), NPC.velocity, goreType);
			}

		for (int d = 0; d < 8; d++)
			Dust.NewDustPerfect(Main.rand.NextVector2FromRectangle(NPC.getRect()), DustID.Blood, Main.rand.NextVector2Unit() * 1.5f, 0, default, Main.rand.NextFloat(1f, 1.5f));
	}

	public override void FindFrame(int frameHeight)
	{
		if (Main.dedServ)
			return;

		Texture2D texture = TextureAssets.Npc[Type].Value;

		NPC.frameCounter = (NPC.frameCounter + 0.15f) % Main.npcFrameCount[Type];
		NPC.frame = texture.Frame(1, Main.npcFrameCount[Type], 0, (int)NPC.frameCounter, 0, -2);
	}

	public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor) => base.PreDraw(spriteBatch, screenPos, drawColor);

	public bool CanSpawnTraveler() => true;
}