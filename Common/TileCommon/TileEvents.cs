using MonoMod.RuntimeDetour;
using System.Reflection;
using Terraria.GameContent.Drawing;

namespace SpiritReforged.Common.TileCommon;

internal class TileEvents : ILoadable
{
	private delegate bool TileFrameDelegate(int i, int j, int type, ref bool resetFrame, ref bool noBreak);

	public static event Action<bool, bool, bool> PreDrawTiles;

	/// <summary> Exposes <see cref="TileLoader.TileFrame"/> resetFrame from the last invocation.<br/>
	/// A common use case for this is in <see cref="ModTile.PostTileFrame"/>, where it's not normally available. </summary>
	public static bool ResetFrame { get; private set; }
	private static Hook TileFrameHook = null;

	/// <summary> Subscribes to <see cref="PreDrawTiles"/> and conditionally invokes <paramref name="action"/> according to <paramref name="inSolidLayer"/>. </summary>
	public static void PreDrawAction(bool inSolidLayer, Action action) => PreDrawTiles += (solidLayer, forRenderTargets, intoRenderTargets) =>
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

	public void Load(Mod mod)
	{
		On_TileDrawing.PreDrawTiles += PreDrawTilesDetour;

		var type = typeof(Mod).Assembly.GetType("Terraria.ModLoader.TileLoader");
		MethodInfo info = type.GetMethod("TileFrame", BindingFlags.Static | BindingFlags.Public, [typeof(int), typeof(int), typeof(int), typeof(bool).MakeByRefType(), typeof(bool).MakeByRefType()]);
		TileFrameHook = new Hook(info, HookTileFrame, true);
	}

	private static void PreDrawTilesDetour(On_TileDrawing.orig_PreDrawTiles orig, TileDrawing self, bool solidLayer, bool forRenderTargets, bool intoRenderTargets)
	{
		PreDrawTiles?.Invoke(solidLayer, forRenderTargets, intoRenderTargets);
		orig(self, solidLayer, forRenderTargets, intoRenderTargets);
	}

	private static bool HookTileFrame(TileFrameDelegate orig, int i, int j, int type, ref bool resetFrame, ref bool noBreak)
	{
		ResetFrame = resetFrame;
		return orig(i, j, type, ref resetFrame, ref noBreak);
	}

	public void Unload()
	{
		TileFrameHook?.Undo();
		TileFrameHook = null;
	}
}