using Terraria.GameContent.Bestiary;

namespace SpiritReforged.Content.Underground.NPCs.MossSlimes;

/// <summary>
/// Prototype class used as a parent class for all Moss Slimes, and also for the bestiary entry.
/// </summary>
internal class MossSlime : ModNPC
{
	protected static Dictionary<int, Asset<Texture2D>> FrontSpritesById = [];
	protected static Dictionary<int, Asset<Texture2D>> BackSpritesById = [];

	private static int DummyBestiaryType = -1;

	public override string Texture => "SpiritReforged/Content/Underground/NPCs/MossSlimes/MossSlimeBase";

	protected virtual Vector3 LightColor { get; }
	protected virtual int MossType { get; }

	public override void SetStaticDefaults()
	{
		Main.npcFrameCount[Type] = 2;

		if (Type != ModContent.NPCType<MossSlime>())
		{
			string path = $"SpiritReforged/Content/Underground/NPCs/MossSlimes/{Name}";
			FrontSpritesById.Add(Type, ModContent.Request<Texture2D>(path + "_Front"));
			BackSpritesById.Add(Type, ModContent.Request<Texture2D>(path + "_Back"));
		}
	}

	public override void SetDefaults()
	{
		NPC.CloneDefaults(NPCID.BlackSlime);
		NPC.aiStyle = NPCAIStyleID.Slime;

		AIType = NPCID.BlueSlime;
		AnimationType = NPCID.BlueSlime;
	}

	public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
	{
		// Give all credit to Moss Slime as the "prototypical" slime
		ContentSamples.NpcBestiaryCreditIdsByNpcNetIds[Type] = ContentSamples.NpcBestiaryCreditIdsByNpcNetIds[ModContent.NPCType<MossSlime>()];

		if (Type != ModContent.NPCType<MossSlime>())
			return;

		bestiaryEntry.AddInfo(this, "Caverns");
	}

	public override bool PreAI()
	{
		Lighting.AddLight(NPC.Center, LightColor);
		return true;
	}

	public override void ModifyNPCLoot(NPCLoot npcLoot) => npcLoot.AddCommon(MossType, 1, 5, 10);

	public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
	{
		if (Type == ModContent.NPCType<MossSlime>())
		{
			DummyBestiaryType = (Main.GameUpdateCount % 360) switch
			{
				< 60 => ModContent.NPCType<KryptonMossSlime>(),
				< 120 => ModContent.NPCType<NeonMossSlime>(),
				< 180 => ModContent.NPCType<XenonMossSlime>(),
				_ => ModContent.NPCType<LavaMossSlime>()
			};

			NPC.type = DummyBestiaryType;
			DrawMoss(spriteBatch, screenPos, drawColor, true);
			return NPC.IsABestiaryIconDummy;
		}

		DrawMoss(spriteBatch, screenPos, drawColor, true);
		return true;
	}

	public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
	{
		if (Type == ModContent.NPCType<MossSlime>() || DummyBestiaryType != -1)
		{
			DrawMoss(spriteBatch, screenPos, drawColor, false);
			NPC.type = ModContent.NPCType<MossSlime>();
			DummyBestiaryType = -1;
			return;
		}

		DrawMoss(spriteBatch, screenPos, drawColor, false);
	}

	private void DrawMoss(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor, bool drawBack)
	{
		Texture2D tex = (drawBack ? BackSpritesById[Type] : FrontSpritesById[Type]).Value;
		Vector2 position = NPC.Center - screenPos;

		spriteBatch.Draw(tex, position, NPC.frame, drawColor, NPC.rotation, NPC.frame.Size() / 2f, NPC.scale, SpriteEffects.None, 0);

		if (drawBack)
		{
			MossSlime slime = ModContent.GetModNPC(Type) as MossSlime;
			int heightOff = NPC.frame.Y == 0 ? 0 : 2;
			Main.DrawItemIcon(spriteBatch, ContentSamples.ItemsByType[slime.MossType], position - new Vector2(0, heightOff), drawColor, 32);
		}
	}
}
