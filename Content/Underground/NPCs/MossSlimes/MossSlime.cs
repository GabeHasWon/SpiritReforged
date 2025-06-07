using Terraria.GameContent.Bestiary;

namespace SpiritReforged.Content.Underground.NPCs.MossSlimes;

internal class MossSlime : ModNPC
{
	protected static Dictionary<int, Asset<Texture2D>> FrontSpritesById = [];
	protected static Dictionary<int, Asset<Texture2D>> BackSpritesById = [];

	public override string Texture => "SpiritReforged/Content/Underground/NPCs/MossSlimes/MossSlimeBase";

	protected virtual Vector3 LightColor { get; }
	protected virtual int MossType { get; }

	public override void SetStaticDefaults()
	{
		Main.npcFrameCount[Type] = 2;

		if (Type == ModContent.NPCType<MossSlime>())
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
		string persistentId = ContentSamples.NpcBestiaryCreditIdsByNpcNetIds[ModContent.NPCType<MossSlime>()];
		bestiaryEntry.UIInfoProvider = new CommonEnemyUICollectionInfoProvider(persistentId, false);
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
			return false;

		Texture2D back = BackSpritesById[Type].Value;
		Vector2 position = NPC.Center - screenPos;

		spriteBatch.Draw(back, position, NPC.frame, drawColor, NPC.rotation, NPC.frame.Size() / 2f, NPC.scale, SpriteEffects.None, 0);
		return true;
	}

	public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
	{
		if (Type == ModContent.NPCType<MossSlime>())
			return;

		Texture2D front = FrontSpritesById[Type].Value;
		Vector2 position = NPC.Center - screenPos;

		spriteBatch.Draw(front, position, NPC.frame, drawColor, NPC.rotation, NPC.frame.Size() / 2f, NPC.scale, SpriteEffects.None, 0);
	}
}
