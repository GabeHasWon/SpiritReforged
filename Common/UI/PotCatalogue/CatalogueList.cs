using Terraria.GameContent.UI.Elements;
using Terraria.GameInput;
using Terraria.UI;

namespace SpiritReforged.Common.UI.PotCatalogue;

public class CatalogueList : UIElement
{
	/// <summary> Specifically contains elements added with <see cref="AddEntry"/>. </summary>
	private readonly List<UIElement> _listed = [];
	private UIScrollbar _scrollbar;

	public CatalogueList() => OverflowHidden = true;

	/// <summary> The pixel width of this element, considering scrollbar territory. </summary>
	public float AvailableWidth => GetDimensions().Width - ((_scrollbar is null) ? 0 : _scrollbar.Width.Pixels);

	public void AddScrollbar(UIScrollbar scrollbar)
	{
		_scrollbar = scrollbar;
		_scrollbar.Left.Set(Width.Pixels - _scrollbar.Width.Pixels, 0);
		_scrollbar.Height.Set(Height.Pixels - 12, 0);
		_scrollbar.Top.Set(6, 0);

		Append(_scrollbar);
	}

	public void ClearEntries()
	{
		for (int i = _listed.Count - 1; i >= 0; i--)
		{
			RemoveChild(_listed[i]);
			_listed.RemoveAt(i);
		}
	}

	public void AddEntry(UIElement e)
	{
		_listed.Add(e);
		Append(e);

		RecalculateEntries();
	}

	public void RecalculateEntries()
	{
		const int listPadding = 6;

		float x = 0;
		float y = 0;
		float lastHeight = 0;

		float scrollbarInfluence = (_scrollbar is null) ? 0 : -_scrollbar.GetValue();

		foreach (var e in _listed)
		{
			int eFullWidth = (int)e.Width.Pixels;

			if (x + eFullWidth > (int)AvailableWidth) //Wrap around
			{
				x = 0;
				y += lastHeight + listPadding;
			}

			e.Left.Pixels = x;
			e.Top.Pixels = y + scrollbarInfluence;

			x += eFullWidth + listPadding;
			lastHeight = e.Height.Pixels;
		}

		_scrollbar?.SetView(1f, Math.Max(y - Height.Pixels * .8f, 1)); //Recalculate maximum scrollbar view
		RecalculateChildren();
	}

	public override void ScrollWheel(UIScrollWheelEvent evt)
	{
		base.ScrollWheel(evt);

		if (_scrollbar != null)
			_scrollbar.ViewPosition -= evt.ScrollWheelValue;
	}

	public override void Update(GameTime gameTime)
	{
		base.Update(gameTime);

		if (IsMouseHovering)
		{
			PlayerInput.LockVanillaMouseScroll("ModLoader/UIList");
			RecalculateEntries(); //Only needs to be called for scrollbar interaction
		}
	}
}