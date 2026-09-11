using Terraria.GameContent.Personalities;

namespace SpiritReforged.Common.NPCCommon;

internal class SkyShoppingBiome : IShoppingBiome, ILoadable
{
	string IShoppingBiome.NameKey => "Sky";

	bool IShoppingBiome.IsInBiome(Player player) => player.ZoneSkyHeight;
	void ILoadable.Load(Mod mod) { }
	void ILoadable.Unload() { }
}
