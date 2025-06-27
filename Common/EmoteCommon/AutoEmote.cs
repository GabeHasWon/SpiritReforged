using SpiritReforged.Common.Visuals;
using Terraria.GameContent.UI;

namespace SpiritReforged.Common.EmoteCommon;

/// <summary> Creates a new emote with an explicit name and texture. </summary>
public sealed class AutoEmote(string name, string texture, int category, Func<bool> activeCondition) : CustomEmote
{
	private class EmoteNPC : GlobalNPC
	{
		public static readonly HashSet<AutoEmote> LoadedEmotes = [];
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

	public override string Name => _name;
	public override string Texture => _texture;
	public override int Category => _category;

	protected override bool CloneNewInstances => true;

	/// <summary> Whether conditions are fulfilled for this emote to appear during NPC interaction. </summary>
	public bool Active => _activeCondition.Invoke();

	private string _name = name;
	private string _texture = texture;
	private int _category = category;
	private Func<bool> _activeCondition = activeCondition;

	/// <summary> Loads a pseudo face emote for <paramref name="npc"/>. </summary>
	public static void LoadFaceEmote(ModNPC npc, Func<bool> condition)
	{
		string name = npc.Name + "Emote";
		AutoEmote emote = new(name, DrawHelpers.RequestLocal(npc.GetType(), name), EmoteID.Category.Town, condition);

		SpiritReforgedMod.Instance.AddContent(emote);
	}

	public override ModEmoteBubble Clone(EmoteBubble newEntity)
	{
		var emote = base.Clone(newEntity) as AutoEmote;
		emote._name = _name;
		emote._texture = _texture;
		emote._category = _category;
		emote._activeCondition = _activeCondition;

		return emote;
	}

	public override void SetStaticDefaults()
	{
		AddToCategory(_category);
		EmoteNPC.LoadedEmotes.Add(this);
	}
}