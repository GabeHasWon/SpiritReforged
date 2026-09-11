namespace SpiritReforged.Content.Ziggurat.NPCs.SpookyMummy;

internal class SpookyMummyNPC : ModNPC
{
	public override void SetDefaults()
	{
		NPC.lifeMax = 1500;
		NPC.defense = 30;
		NPC.aiStyle = -1;
		NPC.knockBackResist = 0f;
	}
}
