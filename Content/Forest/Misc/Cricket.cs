using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.NPCCommon.Abstract;
using Terraria;
using Terraria.GameContent.Bestiary;

namespace SpiritReforged.Content.Forest.Misc;

[AutoloadCritter]
public class Cricket : ModNPC, ISubstitute
{
	public virtual int[] TypesToReplace => [NPCID.Grasshopper];

	public override void SetStaticDefaults()
	{
		CreateItemDefaults();
		Main.npcFrameCount[Type] = 2;
	}

	public override void SetDefaults()
	{
		NPC.CloneDefaults(NPCID.Grasshopper);
		AnimationType = NPCID.Grasshopper;
	}

	public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry) => bestiaryEntry.AddInfo(this, "Surface");

	public virtual void CreateItemDefaults() =>
		ItemEvents.CreateItemDefaults(
		this.AutoItemType(),
		static item =>
		{
			item.value = Item.sellPrice(0, 0, 0, 45);
			item.bait = 10;
		}
	);

	public bool CanSubstitute(Player player) => !Main.dayTime;

	public override void HitEffect(NPC.HitInfo hit)
	{
		if (!Main.dedServ)
		{
			bool dead = NPC.life <= 0;

			for (int i = 0; i < (dead ? 12 : 7); i++)
				Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.BrownMoss, NPC.velocity.X * 0.3f, NPC.velocity.Y * 0.3f, Scale: Main.rand.NextFloat(0.5f, 0.8f));
		}
	}
}