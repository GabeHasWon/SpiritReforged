using System.Diagnostics;
using Terraria.UI;

namespace SpiritReforged.Common.UI.Misc;

#nullable enable

public class UIScrollingImage : UIElement
{
	public delegate bool CustomDraw(UIScrollingImage self, Vector2 position, float timer, ref float opacity);

	public readonly Asset<Texture2D> Border;
	public readonly Asset<Texture2D> Scrolling;
	public readonly Asset<Texture2D>? AltBorder;

	private readonly float ScrollSpeed;
	private readonly CustomDraw? PreDraw;

	private float _timer = 0;

	public UIScrollingImage(Asset<Texture2D> border, Asset<Texture2D> scrolling, float scrollSpeed, Asset<Texture2D>? altBorder = null, CustomDraw? preDraw = null)
	{
		Border = border;
		Scrolling = scrolling;
		ScrollSpeed = scrollSpeed;
		AltBorder = altBorder;
		PreDraw = preDraw;

		if (PreDraw is not null) // Alt must not be null when able to display alt
			Debug.Assert(AltBorder is not null);

		OverrideSamplerState = SamplerState.PointWrap;
	}

	protected override void DrawSelf(SpriteBatch spriteBatch)
	{
		_timer += ScrollSpeed;

		Vector2 pos = GetDimensions().Position().Floor();
		var src = new Rectangle((int)_timer, 0, 76, 76);
		float opacity = 1f;

		spriteBatch.Draw(Scrolling.Value, pos + new Vector2(2, 2), src, Color.White);

		if (PreDraw?.Invoke(this, pos, _timer, ref opacity) is true)
			spriteBatch.Draw(Border.Value, pos, Color.White * opacity);
	}
}