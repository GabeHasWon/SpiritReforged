using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.NPCCommon.Abstract;

namespace SpiritReforged.Content.Forest.Misc;

[AutoloadCritter]
public class Cricket : ModNPC, ISubstitute
{
	public virtual int[] TypesToReplace => [NPCID.Grasshopper];

	public override void SetStaticDefaults() => Main.npcFrameCount[Type] = 2;

	public override void SetDefaults()
	{
		NPC.CloneDefaults(NPCID.Grasshopper);
		AnimationType = NPCID.Grasshopper;
	}

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
}