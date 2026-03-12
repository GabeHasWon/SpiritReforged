using Terraria.GameContent.Bestiary;

namespace SpiritReforged.Common.NPCCommon;

public class NPCEvents : GlobalNPC
{
	public delegate void NPCDelegate(NPC npc);
	public delegate void SpawnRateDelegate(Player player, ref int spawnRate, ref int maxSpawns);
	public delegate void SetBestiaryDelegate(NPC npc, BestiaryDatabase database, BestiaryEntry bestiaryEntry);
	public delegate void PlatformCollisionDelegate(NPC npc, ref bool fall);
	public delegate bool ModifyCollisionParametersDelegate(NPC npc, ref Vector2 collisionTopLeft, ref int collisionWidth, ref int collisionHeight);

	public static event NPCDelegate OnNPCLoot;
	public static event SpawnRateDelegate OnEditSpawnRate;
	public static event SetBestiaryDelegate OnSetBestiary;
	public static event PlatformCollisionDelegate OnPlatformCollision;
	public static event ModifyCollisionParametersDelegate ModifyCollisionParameters;

	public override void Load()
	{
		On_NPC.NPCLoot += NPCLoot;
		On_NPC.Collision_DecideFallThroughPlatforms += DecideToFall;
		On_NPC.GetTileCollisionParameters += ModifyTileCollisionBox;
	}

	private static void NPCLoot(On_NPC.orig_NPCLoot orig, NPC self)
	{
		orig(self);
		OnNPCLoot?.Invoke(self);
	}

	private static bool DecideToFall(On_NPC.orig_Collision_DecideFallThroughPlatforms orig, NPC self)
	{
		bool value = orig(self);
		OnPlatformCollision?.Invoke(self, ref value);
		return value;
	}

	private void ModifyTileCollisionBox(On_NPC.orig_GetTileCollisionParameters orig, NPC self, out Vector2 cPosition, out int cWidth, out int cHeight)
	{
		orig(self, out cPosition, out cWidth, out cHeight);
		foreach (ModifyCollisionParametersDelegate modifyDelegate in ModifyCollisionParameters.GetInvocationList())
			if (modifyDelegate(self, ref cPosition, ref cWidth, ref cHeight))
				return;
	}

	public override void EditSpawnRate(Player player, ref int spawnRate, ref int maxSpawns) => OnEditSpawnRate?.Invoke(player, ref spawnRate, ref maxSpawns);
	public override void SetBestiary(NPC npc, BestiaryDatabase database, BestiaryEntry bestiaryEntry) => OnSetBestiary?.Invoke(npc, database, bestiaryEntry);
}