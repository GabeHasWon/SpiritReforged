using Terraria.ModLoader.Core;

namespace SpiritReforged.Common.PlayerCommon.Interfaces;

internal interface IOnMineTile
{
	internal class OnMineTilePlayer : ILoadable
	{
		public void Load(Mod mod) => On_Player.PickTile += OnPickTile;
		public void Unload() { }

		private static void OnPickTile(On_Player.orig_PickTile orig, Player self, int x, int y, int pickPower)
		{
			Tile tile = Main.tile[x, y];
			bool hadTile = tile.HasTile;
			int lastType = tile.TileType;

			orig(self, x, y, pickPower);

			if (hadTile)
				Invoke(self, x, y, pickPower, lastType, !tile.HasTile);
		}
	}

	private static readonly HookList<ModPlayer> Hook = PlayerLoader.AddModHook(HookList<ModPlayer>.Create(i => ((IOnMineTile)i).OnMineTile));

	public static void Invoke(Player self, int x, int y, int pickPower, int lastType, bool killed)
	{
		foreach (IOnMineTile g in Hook.Enumerate(self.ModPlayers))
			g.OnMineTile(x, y, pickPower, lastType, killed);
	}

	public void OnMineTile(int x, int y, int pickPower, int priorType, bool killed);
}
