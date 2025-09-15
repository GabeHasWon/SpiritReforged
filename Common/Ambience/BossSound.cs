using SpiritReforged.Common.NPCCommon;
using Terraria.Audio;

namespace SpiritReforged.Common.Ambience;

internal class BossSound : ILoadable
{
	public static readonly SoundStyle SoundEffect = new("SpiritReforged/Assets/SFX/Ambient/DownedBoss");

	public void Load(Mod mod) => NPCEvents.OnNPCLoot += PlayDownedSound;
	private static void PlayDownedSound(NPC npc)
	{
		if (Main.dedServ || !npc.boss || npc.type >= NPCID.Count)
			return; //Only play for vanilla bosses

		SoundEngine.PlaySound(SoundEffect, npc.Center);
	}

	public void Unload() { }
}