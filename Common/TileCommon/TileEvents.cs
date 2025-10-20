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

	/// <summary> Return false to prevent orig and end the enumeration. </summary>
	/// <param name="i"> The X coordinate. </param>
	/// <param name="j"> The Y coordinate. </param>
	/// <param name="type"> The tile type being placed. </param>
	/// <param name="style"> The tile style being placed. </param>
	public delegate bool PlacePotDelegate(int i, int j, ushort type, int style);

	public static event PreDrawDelegate OnPreDrawTiles;
	public static event TileDelegate OnPlaceTile;
	public static event KillTileDelegate OnKillTile;
	public static event TileDelegate OnRandomUpdate;
	public static event NearbyDelegate OnNearby;
	public static event PlacePotDelegate OnPlacePot;

	/// <summary> Exposes <see cref="TileLoader.TileFrame"/> resetFrame from the last invocation.<br/>
	/// A common use case for this is in <see cref="ModTile.PostTileFrame"/>, where it's not normally available. </summary>
	public static bool ResetFrame { get; private set; }
	private static Hook TileFrameHook = null;

	/// <summary> Subscribes to <see cref="OnPreDrawTiles"/> and conditionally invokes <paramref name="action"/> according to <paramref name="inSolidLayer"/>. </summary>
	/// <returns> The delegate created for the operation. </returns>
	public static PreDrawDelegate AddPreDrawAction(bool inSolidLayer, Action action)
	{
		PreDrawDelegate value;
		OnPreDrawTiles += value = (solidLayer, forRenderTargets, intoRenderTargets) =>
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

		return value;
	}

	/// <summary> Subscribes to <see cref="OnPlaceTile"/> and conditionally invokes <paramref name="action"/> according to <paramref name="tileType"/>. </summary>
	public static void AddPlaceTileAction(int tileType, TileDelegate action) => OnPlaceTile += (i, j, type) =>
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
		On_WorldGen.PlacePot += PlacePotDetour;

		var type = typeof(Mod).Assembly.GetType("Terraria.ModLoader.TileLoader");
		MethodInfo info = type.GetMethod("TileFrame", BindingFlags.Static | BindingFlags.Public, [typeof(int), typeof(int), typeof(int), typeof(bool).MakeByRefType(), typeof(bool).MakeByRefType()]);
		TileFrameHook = new Hook(info, HookTileFrame, true);
	}

	private static void PreDrawTilesDetour(On_TileDrawing.orig_PreDrawTiles orig, TileDrawing self, bool solidLayer, bool forRenderTargets, bool intoRenderTargets)
	{
		OnPreDrawTiles?.Invoke(solidLayer, forRenderTargets, intoRenderTargets);
		orig(self, solidLayer, forRenderTargets, intoRenderTargets);
	}

	private static bool PlaceTileDetour(On_WorldGen.orig_PlaceTile orig, int i, int j, int Type, bool mute, bool forced, int plr, int style)
	{
		bool value = orig(i, j, Type, mute, forced, plr, style);
		OnPlaceTile?.Invoke(i, j, Type);

		return value;
	}

	private static bool PlacePotDetour(On_WorldGen.orig_PlacePot orig, int x, int y, ushort type, int style)
	{
		if (OnPlacePot != null)
		{
			var enumerator = OnPlacePot.GetInvocationList().GetEnumerator();
			while (enumerator.MoveNext())
			{
				if (enumerator.Current is PlacePotDelegate dele && !dele.Invoke(x, y, type, style))
					return true;
			}
		}

		return orig(x, y, type, style);
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