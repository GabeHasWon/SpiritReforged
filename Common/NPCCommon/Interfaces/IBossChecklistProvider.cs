using SpiritReforged.Common.ModCompat;

namespace SpiritReforged.Common.NPCCommon.Interfaces;

#nullable enable

public enum ChecklistType : byte
{
	Boss,
	Miniboss,
	Event
}

public readonly record struct LocalizableFunc
{
	public readonly LocalizedText? Text;
	public readonly Func<LocalizedText>? Func;

	public LocalizableFunc(LocalizedText? Text, Func<LocalizedText>? Func)
	{
		if (Text is null && Func is null)
			throw new InvalidOperationException("One of Text or Func should be non-null.");

		this.Text = Text;
		this.Func = Func;
	}
}

/// <summary>
/// Information used per boss/entity for Boss Checklist.
/// </summary>
/// <param name="Progression">Placement in progression. Reference https://github.com/JavidPack/BossChecklist/wiki/Boss-Progression-Values</param>
/// <param name="IsDowned"></param>
/// <param name="SpawnInfo"></param>
/// <param name="Collectibles"></param>
/// <param name="DisplayNameOverride"></param>
public readonly record struct BossChecklistData(float Progression, Func<bool> IsDowned, LocalizableFunc SpawnInfo, List<int> Collectibles, List<int> SpawnItems,
	LocalizedText? DisplayNameOverride = null, ChecklistType Type = ChecklistType.Boss);

/// <summary>
/// Allows a boss or miniboss to register itself as a Boss Checklist entity.
/// </summary>
internal interface IBossChecklistProvider
{
	public class BossChecklistProviderLoader : ModSystem
	{
		public override void PostSetupContent()
		{
			if (!CrossMod.BossChecklist.Enabled)
				return;

			for (int i = NPCID.Count; i < NPCLoader.NPCCount; ++i)
			{
				ModNPC npc = ModContent.GetModNPC(i);

				if (npc is not IBossChecklistProvider provider)
					continue;

				var checklist = (Mod)CrossMod.BossChecklist;
				BossChecklistData data = provider.ChecklistData();

				Dictionary<string, object> extraData = new()
				{
					{ "displayName", data.DisplayNameOverride ?? npc.DisplayName },
					{ "spawnInfo", (object)data.SpawnInfo.Text! ?? data.SpawnInfo.Func! }, // Provide either the text or the func - both need to be boxed to work for some reason
					{ "collectibles", data.Collectibles }
				};

				if (provider.PreDrawPortrait is { } action)
					extraData.Add("customPortrait", action);

				if (data.SpawnItems.Count > 0)
					extraData.Add("spawnItems", data.SpawnItems);

				AddChecklistItem(provider, checklist, data, extraData);
			}
		}

		public static void AddChecklistItem(IBossChecklistProvider provider, Mod checklist, BossChecklistData data, Dictionary<string, object> extraData)
		{
			string callType = data.Type switch
			{
				ChecklistType.Boss => "LogBoss",
				ChecklistType.Miniboss => "LogMiniboss",
				ChecklistType.Event => "LogEvent",
				_ => throw new ArgumentException("BossChecklistData.Type is not one of the three valid options.")
			};

			checklist.Call(callType, SpiritReforgedMod.Instance, provider.Name, data.Progression, data.IsDowned, provider.Types, extraData);
		}
	}

	/// <summary>
	/// Mirrors <see cref="ModType.Name"/>, used for autoloading.
	/// </summary>
	public string Name { get; }

	/// <summary>
	/// Mirrors <see cref="ModNPC.Type"/>, used for autoloading.
	/// </summary>
	public int Type { get; }

	/// <summary>
	/// If this NPC has multiple types associated with it (such as the Twins), use this to set multiple NPCs.
	/// </summary>
	public List<int> Types => [Type];

	/// <summary>
	/// Used to override portrait drawing. By default, does not override it.
	/// </summary>
	public Action<SpriteBatch, Rectangle, Color>? PreDrawPortrait => null;

	public BossChecklistData ChecklistData();
}
