using MonoMod.RuntimeDetour;
using System.Reflection;
using Terraria.GameContent.Drawing;

namespace SpiritReforged.Common.TileCommon;

public class TileEvents : GlobalTile
{
	public delegate void TileDelegate(int i, int j, int type);

	public delegate void PreDrawDelegate(bool solidLayer, bool forRenderTarget, bool intoRenderTargets);
	public delegate void KillTileDelegate(int i, int j, int type, ref bool fail, ref bool effectOnly);
	public delegate bool TileFrameDelegate(int i, int j, int type, ref bool resetFrame, ref bool noBreak);
	public delegate void NearbyDelegate(int i, int j, int type, bool closer);

	public static event PreDrawDelegate PreDrawTiles;
	public static event TileDelegate PlaceTile;
	public static event KillTileDelegate OnKillTile;
	public static event TileDelegate OnRandomUpdate;
	public static event NearbyDelegate OnNearby;

	/// <summary> Exposes <see cref="TileLoader.TileFrame"/> resetFrame from the last invocation.<br/>
	/// A common use case for this is in <see cref="ModTile.PostTileFrame"/>, where it's not normally available. </summary>
	public static bool ResetFrame { get; private set; }
	private static Hook TileFrameHook = null;

	/// <summary> Subscribes to <see cref="PreDrawTiles"/> and conditionally invokes <paramref name="action"/> according to <paramref name="inSolidLayer"/>. </summary>
	public static void AddPreDrawAction(bool inSolidLayer, Action action) => PreDrawTiles += (solidLayer, forRenderTargets, intoRenderTargets) =>
	{
		bool flag = intoRenderTargets || Lighting.UpdateEveryFrame;

		if (flag)
		{
			if (inSolidLayer && solidLayer)
				action.Invoke();
			else if (!inSolidLayer && !solidLayer)
				action.Invoke();
		}
	};

	/// <summary> Subscribes to <see cref="PlaceTile"/> and conditionally invokes <paramref name="action"/> according to <paramref name="tileType"/>. </summary>
	public static void AddPlaceTileAction(int tileType, TileDelegate action) => PlaceTile += (i, j, type) =>
	{
		if (type == tileType)
			action.Invoke(i, j, type);
	};

	/// <summary> Subscribes to <see cref="OnKillTile"/> and conditionally invokes <paramref name="action"/> according to <paramref name="tileType"/>. </summary>
	public static void AddKillTileAction(int tileType, KillTileDelegate action) => OnKillTile += (int i, int j, int type, ref bool fail, ref bool effectOnly) =>
	{
		if (type == tileType)
			action.Invoke(i, j, type, ref fail, ref effectOnly);
	};

	#region custom hooks
	public override void Load()
	{
		On_TileDrawing.PreDrawTiles += PreDrawTilesDetour;
		On_WorldGen.PlaceTile += PlaceTileDetour;

		var type = typeof(Mod).Assembly.GetType("Terraria.ModLoader.TileLoader");
		MethodInfo info = type.GetMethod("TileFrame", BindingFlags.Static | BindingFlags.Public, [typeof(int), typeof(int), typeof(int), typeof(bool).MakeByRefType(), typeof(bool).MakeByRefType()]);
		TileFrameHook = new Hook(info, HookTileFrame, true);
	}

	private static void PreDrawTilesDetour(On_TileDrawing.orig_PreDrawTiles orig, TileDrawing self, bool solidLayer, bool forRenderTargets, bool intoRenderTargets)
	{
		PreDrawTiles?.Invoke(solidLayer, forRenderTargets, intoRenderTargets);
		orig(self, solidLayer, forRenderTargets, intoRenderTargets);
	}

	private static bool PlaceTileDetour(On_WorldGen.orig_PlaceTile orig, int i, int j, int Type, bool mute, bool forced, int plr, int style)
	{
		bool value = orig(i, j, Type, mute, forced, plr, style);
		PlaceTile?.Invoke(i, j, Type);

		return value;
	}

	private static bool HookTileFrame(TileFrameDelegate orig, int i, int j, int type, ref bool resetFrame, ref bool noBreak)
	{
		ResetFrame = resetFrame;
		return orig(i, j, type, ref resetFrame, ref noBreak);
	}
	#endregion

	public override void KillTile(int i, int j, int type, ref bool fail, ref bool effectOnly, ref bool noItem) => OnKillTile?.Invoke(i, j, type, ref fail, ref effectOnly);
	public override void RandomUpdate(int i, int j, int type) => OnRandomUpdate?.Invoke(i, j, type);
	public override void NearbyEffects(int i, int j, int type, bool closer) => OnNearby?.Invoke(i, j, type, closer);

	public override void Unload()
	{
		TileFrameHook?.Undo();
		TileFrameHook = null;
	}
}