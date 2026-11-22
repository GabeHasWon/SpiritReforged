using SpiritReforged.Common.MathHelpers;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace SpiritReforged.Common.UI.Elements;

#nullable enable

// Copied from my same implementation in New Beginnings.
internal class UISlider<T> : UIElement where T : global::System.Numerics.INumber<T>
{
    private static Asset<Texture2D> Back = ModContent.Request<Texture2D>("SpiritReforged/Common/UI/Elements/SliderBase");
    private static Asset<Texture2D> Button = ModContent.Request<Texture2D>("SpiritReforged/Common/UI/Elements/SliderButton");

    public T Value { get; private set; }

    public readonly T Start;
	public readonly T Increment;
	public readonly T Minimum;
	public readonly T Maximum;
	public readonly Color Color;

	public UIImageButton button = null!;

    private bool _dragging = false;

	/// <summary>
	/// Creates a slider with the start point, step, minimum, maximum and color.
	/// </summary>
	public UISlider(T start, T increment, T min, T max, Color color)
	{
		Value = start;
		Start = start;
		Increment = increment;
		Minimum = min;
		Maximum = max;
		Color = color;

		button = new(Button)
		{
			Width = StyleDimension.FromPixels(12),
			Height = StyleDimension.FromPixels(20),
			Top = StyleDimension.FromPixels(-4),
		};

		button.SetVisibility(1f, 0.8f);
		button.OnUpdate += ClickHoldButton;

		Append(button);
		Reset();
	}

	private void Reset() => SetToFactor(GenericMath.InverseLerp(Minimum, Maximum, Start));

	private void ClickHoldButton(UIElement affectedElement)
    {
        if (Main.mouseLeft && affectedElement.ContainsPoint(Main.MouseScreen))
            _dragging = true;
        else if (!Main.mouseLeft)
            _dragging = false;

        if (_dragging)
            DragButton();
    }

    private void DragButton()
    {
        var bounds = GetDimensions().ToRectangle();
        int diff = Main.mouseX - bounds.Left;
        float factor = Utils.GetLerpValue(0, 1, diff / (float)bounds.Width, true);

        SetToFactor(factor);
    }

    public void SetToFactor(float factor)
    {
        button.HAlign = factor;
        double prop = double.CreateSaturating(Increment) / (double.CreateSaturating(Maximum) - double.CreateSaturating(Minimum));
        factor = (float)((int)(factor / prop) * prop);

        Value = GenericMath.Lerp(Minimum, Maximum, factor);
        Recalculate();
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
		var bounds = GetDimensions().ToRectangle();
        var topLeft = bounds.Location.ToVector2();
        var middleScale = new Vector2((bounds.Width - 12) / 2f, 1);

        spriteBatch.Draw(Back.Value, topLeft, new Rectangle(0, 0, 6, 12), Color);

        for (int i = 0; i < 3; ++i)
            spriteBatch.Draw(Back.Value, topLeft + new Vector2(6, 0), new Rectangle(8, 0, 2, 12), Color, 0f, Vector2.Zero, middleScale, SpriteEffects.None, 0);

        spriteBatch.Draw(Back.Value, topLeft + new Vector2(bounds.Width - 6, 0), new Rectangle(12, 0, 6, 12), Color, 0f, Vector2.Zero, Vector2.One, SpriteEffects.None, 0);
    }
}
