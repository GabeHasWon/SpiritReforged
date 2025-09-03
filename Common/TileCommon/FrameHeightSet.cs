using Terraria.GameContent.Drawing;

namespace SpiritReforged.Common.TileCommon;

/// <summary> Allows the frame height of basic tiles to be modified. See <see cref="SpiritSets.FrameHeight"/>. </summary>
internal class FrameHeightSet : ILoadable
{
	public void Load(Mod mod) => On_TileDrawing.GetTileDrawData += ModifyHeight;

	private static void ModifyHeight(On_TileDrawing.orig_GetTileDrawData orig, TileDrawing self, int x, int y, Tile tileCache, ushort typeCache, ref short tileFrameX, ref short tileFrameY, out int tileWidth, out int tileHeight, out int tileTop, out int halfBrickHeight, out int addFrX, out int addFrY, out SpriteEffects tileSpriteEffect, out Texture2D glowTexture, out Rectangle glowSourceRect, out Color glowColor)
	{
		orig(self, x, y, tileCache, typeCache, ref tileFrameX, ref tileFrameY, out tileWidth, out tileHeight, out tileTop, out halfBrickHeight, out addFrX, out addFrY, out tileSpriteEffect, out glowTexture, out glowSourceRect, out glowColor);

		int height = SpiritSets.FrameHeight[typeCache];
		if (height != -1)
		{
			tileHeight = height;
		}
	}

	public void Unload() { }
}