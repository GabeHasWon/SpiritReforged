using SpiritReforged.Common.Misc;
using Terraria.DataStructures;
using Terraria.ModLoader.IO;

namespace SpiritReforged.Common.NPCCommon.Abstract;

public abstract class WorldNPC : ModNPC
{
	/// <summary> Whether this NPC has spawned today. Commonly checked in <see cref="SpawnChance"/> to prevent multiple spawns in one day. </summary>
	public bool SpawnedToday => WorldNPCFlags.SpawnedToday[Name];

	/// <summary><inheritdoc/><para/>
	/// Automatically registers this NPC for <see cref="WorldNPCFlags.SpawnedToday"/>.<br/>
	/// Additionally sets <see cref="NPCID.Sets.ActsLikeTownNPC"/> and <see cref="NPCID.Sets.NoTownNPCHappiness[Type]"/> to true.
	/// </summary>
	public override void SetStaticDefaults()
	{
		NPCID.Sets.ActsLikeTownNPC[Type] = true;
		NPCID.Sets.NoTownNPCHappiness[Type] = true;

		WorldNPCFlags.SpawnedToday.TryAdd(Name, false);
	}

	public override void SetDefaults()
	{
		NPC.CloneDefaults(NPCID.SkeletonMerchant);
		NPC.HitSound = SoundID.NPCHit1;
		NPC.DeathSound = SoundID.NPCDeath1;
		NPC.Size = new Vector2(30, 40);

		AnimationType = NPCID.Guide;
	}

	public override bool CanChat() => true; //Must be specified because this isn't a town NPC

	/// <summary><inheritdoc/><para/>
	/// Automatically sets <see cref="WorldNPCFlags.SpawnedToday"/>.
	/// </summary>
	public override void OnSpawn(IEntitySource source) => WorldNPCFlags.SpawnedToday[Name] = true;
}

internal class WorldNPCFlags : ModSystem
{
	public static readonly Dictionary<string, bool> SpawnedToday = [];

	public override void Load() => TimeUtils.JustTurnedDay += ResetAll;
	private static void ResetAll()
	{
		foreach (string name in SpawnedToday.Keys)
			SpawnedToday[name] = false;
	}

	public override void SaveWorldData(TagCompound tag)
	{
		foreach (var pair in SpawnedToday)
			tag[pair.Key] = pair.Value;
	}

	public override void LoadWorldData(TagCompound tag)
	{
		foreach (string name in SpawnedToday.Keys)
			SpawnedToday[name] = tag.GetBool(name);
	}
}