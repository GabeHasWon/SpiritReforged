using ILLogger;
using MonoMod.Cil;
using SpiritReforged.Common.TileCommon.TileSway;
using System.Linq;
using Terraria.DataStructures;
using Terraria.GameContent.Drawing;
using static SpiritReforged.Common.TileCommon.DrawOrderAttribute;

namespace SpiritReforged.Common.TileCommon;

/// <summary>
/// Defines the draw orders for a given tile.<br/>
/// This attribute is not inherited.
/// </summary>
/// <param name="layers"></param>
[AttributeUsage(AttributeTargets.Class)]
internal class DrawOrderAttribute(params Layer[] layers) : Attribute
{
	public Layer[] Layers { get; private set; }  = layers;

	public enum Layer : byte //Draws above the given layer
	{
		NonSolid,
		Solid,
		OverPlayers,
		Default
	}
}

internal class DrawOrderSystem : ModSystem
{
	internal static event Action DrawTilesSolidEvent;
	internal static event Action DrawTilesNonSolidEvent;
	internal static event Action PostDrawPlayersEvent;

	/// <summary> Stores tile types and defined layer pairs on load. </summary>
	private static readonly Dictionary<int, Layer[]> DrawOrderTypes = []; 

	/// <summary> Stores drawing coordinates so our detours know where to draw. </summary>
	internal static readonly HashSet<Point16> SpecialDrawPoints = [];

	/// <summary> Used in conjunction with <see cref="DrawOrderAttribute"/> to tell whether a tile is drawing as a result of the attribute and on what layer. </summary>
	internal static Layer Order = Layer.Default;

	public static bool TryGetLayers(int type, out Layer[] layers)
	{
		if (DrawOrderTypes.TryGetValue(type, out Layer[] value))
		{
			layers = value;
			return true;
		}

		layers = null;
		return false;
	}

	public override void Load()
	{
		static void Draw(Layer layer)
		{
			Order = layer;

			foreach (var pt in SpecialDrawPoints)
			{
				int type = Framing.GetTileSafely(pt).TileType;

				if (DrawOrderTypes.TryGetValue(type, out var value) && value.Contains(Order))
					TileLoader.PreDraw(pt.X, pt.Y, type, Main.spriteBatch);
			}

			Order = Layer.Default;
		}

		#region detours/il
		On_TileDrawing.PreDrawTiles += ClearDrawPoints;

		IL_Main.DoDraw_Tiles_Solid += static (ILContext il) =>
		{
			var c = new ILCursor(il);
			for (int i = 0; i < 2; i++)
			{
				if (!c.TryGotoNext(x => x.MatchCallvirt<SpriteBatch>("End")))
				{
					SpiritReforgedMod.Instance.LogIL("Draw Order Solids", $"Method 'SpriteBatch.End' index {i} not found.");
					return;
				}
			}

			c.EmitDelegate(() => DrawTilesSolidEvent?.Invoke()); //Emit a delegate so we can draw just before the spritebatch ends
		};

		On_Main.DoDraw_Tiles_NonSolid += static (On_Main.orig_DoDraw_Tiles_NonSolid orig, Main self) =>
		{
			orig(self);
			DrawTilesNonSolidEvent?.Invoke();
		};

		On_Main.DrawPlayers_AfterProjectiles += static (On_Main.orig_DrawPlayers_AfterProjectiles orig, Main self) =>
		{
			orig(self);

			Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
			PostDrawPlayersEvent?.Invoke();
			Main.spriteBatch.End();
		};

		PostDrawPlayersEvent += () => Draw(Layer.OverPlayers);
		DrawTilesNonSolidEvent += () => Draw(Layer.NonSolid);
		DrawTilesSolidEvent += () => Draw(Layer.Solid);
		#endregion
	}

	private static void ClearDrawPoints(On_TileDrawing.orig_PreDrawTiles orig, TileDrawing self, bool solidLayer, bool forRenderTargets, bool intoRenderTargets)
	{
		orig(self, solidLayer, forRenderTargets, intoRenderTargets);

		bool flag = intoRenderTargets || Lighting.UpdateEveryFrame;
		if (!solidLayer && flag) //Does not clear solid points when expected and may cause issues when using them
			SpecialDrawPoints.Clear();
	}

	public override void PostSetupContent()
	{
		var modTiles = ModContent.GetContent<ModTile>();
		foreach (var tile in modTiles)
		{
			var tag = (DrawOrderAttribute)Attribute.GetCustomAttribute(tile.GetType(), typeof(DrawOrderAttribute), false);

			if (tag is not null)
				DrawOrderTypes.Add(tile.Type, tag.Layers);
			else if (tile is ISwayTile sway && sway.Style == -1) //If no layers are defined for this ISwayTile, automatically add a valid layer for sway drawing
				DrawOrderTypes.Add(tile.Type, [Layer.NonSolid]);
		}
	}
}

internal class DrawOrderGlobalTile : GlobalTile
{
	public override bool PreDraw(int i, int j, int type, SpriteBatch spriteBatch)
	{
		if (!DrawOrderSystem.TryGetLayers(type, out var value))
			return true;

		if (DrawOrderSystem.Order == Layer.Default)
		{
			DrawOrderSystem.SpecialDrawPoints.Add(new Point16(i, j));
			return value.Contains(Layer.Default);
		}

		return true;
	}
}
