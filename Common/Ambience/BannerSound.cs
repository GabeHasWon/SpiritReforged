using SpiritReforged.Common.NPCCommon;
using Terraria.Audio;

namespace SpiritReforged.Common.Ambience;

internal class BannerSound : ILoadable
{
	public static readonly SoundStyle SoundEffect = new("SpiritReforged/Assets/SFX/Ambient/Banner");

	public void Load(Mod mod) => NPCEvents.OnNPCLoot += PlayerBannerSound;
	private static void PlayerBannerSound(NPC npc)
	{
		if (Main.dedServ || npc.ExcludedFromDeathTally())
			return;

		int bannerItem = Item.NPCtoBanner(npc.BannerID());

		if (bannerItem > 0 && NPC.killCount[bannerItem] % ItemID.Sets.KillsToBanner[bannerItem] == 0)
			SoundEngine.PlaySound(SoundEffect, npc.Center);
	}

	public void Unload() { }
}