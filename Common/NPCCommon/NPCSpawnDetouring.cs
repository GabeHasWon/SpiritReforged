using MonoMod.Cil;
using SpiritReforged.Content.SaltFlats.Biome;

namespace SpiritReforged.Common.NPCCommon;

/// <summary>
/// Used to discount the Salt Flats as Desert/Snow for spawning purposes.
/// </summary>
internal class NPCSpawnDetouring : ILoadable
{
	private static BitsByte? OldZone1 = null;
	private static BitsByte? OldZone2 = null;

	public void Load(Mod mod) => IL_Main.DoUpdateInWorld += DetourSpawnNPC;

	private void DetourSpawnNPC(ILContext il)
	{
		ILCursor c = new(il);

		if (!c.TryGotoNext(x => x.MatchLdsfld<Main>(nameof(Main.remixWorld))))
			return;

		c.EmitDelegate(PreSpawnNPC);

		if (!c.TryGotoNext(x => x.MatchLdsfld<Main>(nameof(Main.remixWorld))))
			return;

		c.EmitDelegate(PostSpawnNPC);
	}

	public static void PostSpawnNPC()
	{
		Player plr = Main.LocalPlayer;

		if (OldZone1 is not null)
			plr.zone1 = OldZone1.Value;

		if (OldZone2 is not null)
			plr.zone2 = OldZone2.Value;
	}

	public static void PreSpawnNPC()
	{
		Player plr = Main.LocalPlayer;
		OldZone1 = plr.zone1;
		OldZone2 = plr.zone2;

		if (plr.InModBiome<SaltBiome>())
		{
			plr.ZoneDesert = false;
			plr.ZoneSnow = false;
		}
	}

	public void Unload()
	{
	}
}
