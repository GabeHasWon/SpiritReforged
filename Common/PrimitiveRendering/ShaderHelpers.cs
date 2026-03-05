using Terraria;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SpiritReforged.Common.PrimitiveRendering;

    static partial class ShaderHelpers
    {
       public static bool HasParameter(this Effect effect, string parameterName)
        {
            foreach (EffectParameter parameter in effect.Parameters)
            {
                if (parameter.Name == parameterName)
                {
                    return true;
                }
            }

            return false;
	}

	public static void SetBasicEffectMatrices(ref BasicEffect effect)
	{
		GetWorldViewProjection(out Matrix view, out Matrix projection);

		effect.View = view;
		effect.Projection = projection;
	}

	public static void SetEffectMatrices(ref Effect effect, bool useUiMatrix = false)
	{
		GetWorldViewProjection(out Matrix view, out Matrix projection, useUiMatrix);

		if (effect.HasParameter("WorldViewProjection"))
			effect.Parameters["WorldViewProjection"].SetValue(view * projection);
	}

	public static void GetWorldViewProjection(out Matrix view, out Matrix projection, bool useUiMatrix = false)
	{
		view = Main.GameViewMatrix.TransformationMatrix;
		if (useUiMatrix)
			view = Main.UIScaleMatrix;
		GetProjection(out projection);
	}

	public static void GetProjection(out Matrix projection)
	{
		projection = Matrix.CreateOrthographicOffCenter(0, Main.screenWidth, Main.screenHeight, 0, -1, 1);

		/*
		int width = Main.graphics.GraphicsDevice.Viewport.Width;
		int height = Main.graphics.GraphicsDevice.Viewport.Height;

		view = Matrix.CreateLookAt(Vector3.Zero, Vector3.UnitZ, Vector3.Up) *
					  Matrix.CreateTranslation(width / 2f, height / -2f, 0) * Matrix.CreateRotationZ(MathHelper.Pi) *
					  Matrix.CreateScale(zoom.X, zoom.Y, 1f);

		projection = Matrix.CreateOrthographic(width, height, 0, 1000);
		*/
	}
}
