using SpiritReforged.Common.TileCommon;

namespace SpiritReforged.Content.Forest.Graveyard;

/// <summary> Passively spawns fireflies around graveyard light sources. </summary>
[Autoload(Side = ModSide.Client)]
public class FireflyCloud : ILoadable
{
	public void Load(Mod mod) => TileEvents.OnNearby += SpawnWind;
	public void Unload() { }

	private static void SpawnWind(int i, int j, int type, bool visual)
	{
		if (!visual || Main.dayTime || Main.gamePaused || !Main.LocalPlayer.ZoneGraveyard)
			return;

		if (Main.rand.NextBool(25) && Main.tile[i, j].WallType == WallID.None && Lighting.Brightness(i, j) > 1)
		{
			Vector2 position = new Vector2(i, j).ToWorldCoordinates() + Main.rand.NextVector2Unit() * Main.rand.NextFloat(50f);
			var dust = Dust.NewDustPerfect(position, DustID.Firefly, Scale: Main.rand.NextFloat() + 1);
			dust.velocity *= 0.3f;
		}
	}
}