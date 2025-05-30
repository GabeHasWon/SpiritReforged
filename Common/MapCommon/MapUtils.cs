using Terraria.Map;

namespace SpiritReforged.Common.MapCommon;

public class MapUtils : ILoadable
{
	public struct PublicOverlayContext(Vector2 mapPosition, Vector2 mapOffset, Rectangle? clippingRect, float mapScale, float drawScale)
	{
		public Vector2 mapPosition = mapPosition;
		public Vector2 mapOffset = mapOffset;
		public Rectangle? clippingRect = clippingRect;
		public float mapScale = mapScale;
		public float drawScale = drawScale;
	}

	public static PublicOverlayContext Context { get; private set; }

	public void Load(Mod mod) => On_MapIconOverlay.Draw += ExposeDrawParams;
	private static void ExposeDrawParams(On_MapIconOverlay.orig_Draw orig, MapIconOverlay self, Vector2 mapPosition, Vector2 mapOffset, Rectangle? clippingRect, float mapScale, float drawScale, ref string text)
	{
		Context = new(mapPosition, mapOffset, clippingRect, mapScale, drawScale);
		orig(self, mapPosition, mapOffset, clippingRect, mapScale, drawScale, ref text);
	}

	/// <summary> Translates the given tile position to the map. Automatically accounts for minimap and fullscreen drawing. </summary>
	public static Vector2 TranslateToMap(Vector2 position, PublicOverlayContext c = default) => (position - c.mapPosition) * c.mapScale + c.mapOffset;

	public void Unload() { }
}
