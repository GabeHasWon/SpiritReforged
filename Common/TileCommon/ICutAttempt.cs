using Terraria.DataStructures;

namespace SpiritReforged.Common.TileCommon;

/// <summary> Allows dynamic control over whether this tile should be cut using <see cref="OnCutAttempt"/>. </summary>
public interface ICutAttempt
{
	/// <returns> Whether the tile at the given coordinates should be cut. </returns>
	public bool OnCutAttempt(int i, int j);
}

public class CutAttemptTile : ModSystem
{
	private static readonly Dictionary<Point16, ushort> Cooldowns = [];

	public override void Load() => On_WorldGen.CanCutTile += CheckCuttable;
	private static bool CheckCuttable(On_WorldGen.orig_CanCutTile orig, int x, int y, TileCuttingContext context)
	{
		int type = Main.tile[x, y].TileType;
		if (type >= TileID.Count && TileLoader.GetTile(type) is ICutAttempt cut)
		{
			int i = x;
			int j = y;

			TileExtensions.GetTopLeft(ref i, ref j);
			var key = new Point16(i, j);

			if (Cooldowns.ContainsKey(key))
				return false;

			Cooldowns.TryAdd(key, GetCooldown());
			return cut.OnCutAttempt(i, j);
		}

		return orig(x, y, context);

		ushort GetCooldown()
		{
			if (context == TileCuttingContext.AttackProjectile)
				return 8;

			var p = Main.player[Player.FindClosest(new Vector2(x, y) * 16, 16, 16)];
			return (ushort)(p.HeldItem?.useAnimation ?? 30);
		}
	}

	/// <summary> <inheritdoc cref="ModSystem.PostUpdateItems"/><br/>Increments tile cut cooldowns. </summary>
	public override void PostUpdateItems()
	{
		List<Point16> queued = [];

		foreach (var value in Cooldowns.Keys)
		{
			if (--Cooldowns[value] <= 0)
				queued.Add(value);
		}

		foreach (var value in queued)
			Cooldowns.Remove(value);
	}
}