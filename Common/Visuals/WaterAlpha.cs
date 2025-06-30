using Terraria.GameContent.Drawing;
using Terraria.GameContent.Liquid;
using Terraria.Graphics;

namespace SpiritReforged.Common.Visuals;

/// <summary> Modifies liquid alpha in high light levels. </summary>
public class WaterAlpha : ILoadable
{
	public delegate bool WaterColorDelegate(int x, int y, ref VertexColors colors, bool isPartial);

	public static event WaterColorDelegate OnWaterColor;
	private static bool IsLiquid;

	public void Load(Mod mod)
	{
		On_LiquidRenderer.DrawNormalLiquids += CheckLiquid;
		On_TileDrawing.DrawPartialLiquid += DrawPartialLiquid;
		On_Lighting.GetCornerColors += ModifyWater;
	}

	private static void CheckLiquid(On_LiquidRenderer.orig_DrawNormalLiquids orig, LiquidRenderer self, SpriteBatch batch, Vector2 off, int style, float alpha, bool bg)
	{
		IsLiquid = true;
		orig(self, batch, off, style, alpha, bg);
		IsLiquid = false;
	}

	private static void DrawPartialLiquid(On_TileDrawing.orig_DrawPartialLiquid orig, TileDrawing self, bool behindBlocks, Tile tileCache, ref Vector2 position, ref Rectangle liquidSize, int liquidType, ref VertexColors colors)
	{
		ModifyColors((int)position.X, (int)position.Y, ref colors, true);
		orig(self, behindBlocks, tileCache, ref position, ref liquidSize, liquidType, ref colors);
	}

	private static void ModifyWater(On_Lighting.orig_GetCornerColors orig, int centerX, int centerY, out VertexColors vertices, float scale)
	{
		orig(centerX, centerY, out vertices, scale);

		if (IsLiquid)
			ModifyColors(centerX, centerY, ref vertices);
	}

	private static void ModifyColors(int x, int y, ref VertexColors colors, bool isPartial = false)
	{
		float totalStrength = Main.LocalPlayer.ZoneBeach ? 1f : .75f;

		if (isPartial)
		{
			//Convert from drawing to world coords
			x += (int)(Main.screenPosition.X - Main.offScreenRange);
			y += (int)(Main.screenPosition.Y - Main.offScreenRange);

			//Convert from world to tile coords
			x /= 16;
			y /= 16;
		}

		if (OnWaterColor?.Invoke(x, y, ref colors, isPartial) ?? false)
			return;

		Clamp(ref colors.TopLeftColor, x, y);
		Clamp(ref colors.TopRightColor, x + 1, y);
		Clamp(ref colors.BottomLeftColor, x, y + 1);
		Clamp(ref colors.BottomRightColor, x + 1, y + 1);

		byte GetAlpha(int x, int y)
		{
			float strength = .72f * totalStrength;
			float waveStr = .35f * totalStrength;

			float waveUnit = (float)((1f + Math.Sin(Main.timeForVisualEffects / 100f + (x + y) / 3)) / 2f);
			float brightness = MathHelper.Clamp(Lighting.Brightness(x, y) * (1f - waveUnit * waveStr) - (1f - strength), 0, 1);

			return (byte)((1f - brightness) * 255f);
		}

		void Clamp(ref Color color, int x, int y) => color.A = Math.Min(color.A, GetAlpha(x, y));
	}

	public void Unload() { }
}
