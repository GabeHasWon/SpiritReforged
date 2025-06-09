using SpiritReforged.Common.Particle;
using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.PrimitiveRendering.PrimitiveShape;
using SpiritReforged.Common.PrimitiveRendering;
using static Terraria.GameContent.Animations.IL_Actions.Sprites;

namespace SpiritReforged.Content.Particles;

public class ImpactLinePrim(Vector2 position, Vector2 velocity, Color color, Vector2 scale, int timeLeft, float acceleration, Entity attatchedEntity = null) : ImpactLine(position, velocity, color, scale, timeLeft, acceleration, attatchedEntity)
{
	public override void CustomDraw(SpriteBatch spriteBatch)
	{
		float progress = EaseFunction.EaseSine.Ease(Progress);
		var scale = new Vector2(0.5f, progress) * _scaleMod;

		Color uColor = Color;
		if (UseLightColor)
			uColor = Color.MultiplyRGBA(Lighting.GetColor(Position.ToTileCoordinates()));

		Effect blurEffect = AssetLoader.LoadedShaders["BlurLine"];
		var blurLine = new SquarePrimitive()
		{
			Position = Position - Main.screenPosition,
			Height = 72 * scale.Y,
			Length = 36 * scale.X,
			Rotation = Rotation,
			Color = uColor * (progress / 5 + 0.8f)
		};
		PrimitiveRenderer.DrawPrimitiveShape(blurLine, blurEffect);
	}
}
