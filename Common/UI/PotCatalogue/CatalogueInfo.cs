using Terraria.UI;

namespace SpiritReforged.Common.UI.PotCatalogue;

/// <summary> Used to build info (name, description, item drops, etc.) and be sorted by <see cref="CatalogueList"/>. </summary>
public class CatalogueInfo : UIElement
{
	/// <returns> Whether a divider should be drawn. </returns>
	public delegate bool DrawDelegate(SpriteBatch spriteBatch, Rectangle bounds);
	public event DrawDelegate Action;

	public CatalogueInfo() => OverflowHidden = true;

	protected override void DrawSelf(SpriteBatch spriteBatch)
	{
		base.DrawSelf(spriteBatch);

		var bounds = GetDimensions().ToRectangle();
		var source = new Rectangle(0, 0, (int)Width.Pixels - 8, 4);

		if (Action?.Invoke(spriteBatch, bounds) is true)
			spriteBatch.Draw(CatalogueUI.Divider.Value, bounds.Bottom(), source, Color.White, 0, source.Size() / 2, 1, default, 0);
	}
}