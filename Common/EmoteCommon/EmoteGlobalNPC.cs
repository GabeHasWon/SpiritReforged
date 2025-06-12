using SpiritReforged.Content.Visuals.Emotes;
using Terraria.GameContent.UI;
using Terraria.GameContent.UI.Elements;

namespace SpiritReforged.Common.EmoteCommon;
public class EmoteGlobalNPC : GlobalNPC
{
	public override int? PickEmote(NPC npc, Player closestPlayer, List<int> emoteList, WorldUIAnchor otherAnchor)
	{
		if (Main.rand.NextBool(2))
			emoteList.Add(ModContent.EmoteBubbleType<CartorgrapherEmote>());

		return base.PickEmote(npc, closestPlayer, emoteList, otherAnchor);
	}
}