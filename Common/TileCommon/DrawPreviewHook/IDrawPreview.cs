namespace SpiritReforged.Common.TileCommon.DrawPreviewHook;

[Obsolete("Use PreDrawPlacementPreview instead")]
public interface IDrawPreview
{
	public void DrawPreview(SpriteBatch spriteBatch, Terraria.DataStructures.TileObjectPreviewData data, Vector2 position);
}
