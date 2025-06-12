using Terraria.GameContent.UI;

namespace SpiritReforged.Common.EmoteCommon;

public class EmoteNPC : GlobalNPC
{
	public static readonly HashSet<CustomEmote> LoadedEmotes = [];
	public override int? PickEmote(NPC npc, Player closestPlayer, List<int> emoteList, WorldUIAnchor otherAnchor)
	{
		foreach (var emote in LoadedEmotes)
		{
			if (emote.Active)
				emoteList.Add(emote.Type);
		}

		return null;
	}
}