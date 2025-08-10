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

		int bannerType = Item.NPCtoBanner(npc.BannerID());
		int killsToBanner = ItemID.Sets.KillsToBanner[Item.BannerToItem(bannerType)];

		if (bannerType > 0 && NPC.killCount[bannerType] % killsToBanner == 0)
			SoundEngine.PlaySound(SoundEffect, npc.Center);
	}

	public void Unload() { }
}