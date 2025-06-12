using Terraria.GameContent.Drawing;

namespace SpiritReforged.Common.TileCommon;

internal class TileEvents : ILoadable
{
	public static event Action<bool, bool, bool> PreDrawTiles;

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

	public void Load(Mod mod) => On_TileDrawing.PreDrawTiles += PreDrawTilesDetour;
	private static void PreDrawTilesDetour(On_TileDrawing.orig_PreDrawTiles orig, TileDrawing self, bool solidLayer, bool forRenderTargets, bool intoRenderTargets)
	{
		PreDrawTiles?.Invoke(solidLayer, forRenderTargets, intoRenderTargets);
		orig(self, solidLayer, forRenderTargets, intoRenderTargets);
	}

	public void Unload() { }
}