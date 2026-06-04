using SpiritReforged.Common.MathHelpers;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace SpiritReforged.Common.WorldGeneration.GenConfiguration;

// Copied from my same implementation in New Beginnings.
internal class UISlider<T>(T start, T increment, T min, T max, Color color) : UIElement where T : System.Numerics.INumber<T>
{
    private static Asset<Texture2D> Back = ModContent.Request<Texture2D>("SpiritReforged/Common/WorldGeneration/GenConfiguration/SliderBase");
    private static Asset<Texture2D> Button = ModContent.Request<Texture2D>("SpiritReforged/Common/WorldGeneration/GenConfiguration/SliderButton");

    public T Value { get; private set; } = start;

    public readonly T Start = start;
    public readonly T Increment = increment;
    public readonly T Minimum = min;
    public readonly T Maximum = max;
    public readonly Color Color = color;
	
	public UIImageButton button = null;

    private bool _dragging = false;

    public override void OnInitialize()
	{
		button = new(Button)
		{
			Width = StyleDimension.FromPixels(12),
			Height = StyleDimension.FromPixels(20),
			Top = StyleDimension.FromPixels(-4),
		};

		button.SetVisibility(1f, 0.8f);
		button.OnUpdate += ClickHoldButton;

		Append(button);
		Reset(button);
	}

	private void Reset(UIImageButton button) => SetToFactor(GenericMath.InverseLerp(Minimum, Maximum, Start), button);

	private void ClickHoldButton(UIElement affectedElement)
    {
        if (Main.mouseLeft && affectedElement.ContainsPoint(Main.MouseScreen))
            _dragging = true;
        else if (!Main.mouseLeft)
            _dragging = false;

        if (_dragging)
            DragButton(affectedElement);
    }

    private void DragButton(UIElement sliderButton)
    {
        var bounds = GetDimensions().ToRectangle();
        int diff = Main.mouseX - bounds.Left;
        float factor = Utils.GetLerpValue(0, 1, diff / (float)bounds.Width, true);

        SetToFactor(factor, sliderButton);
    }

    private void SetToFactor(float factor, UIElement button)
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
