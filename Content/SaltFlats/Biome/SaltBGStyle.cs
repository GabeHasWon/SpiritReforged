using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System.Runtime.CompilerServices;
using System.Reflection;

namespace SpiritReforged.Content.SaltFlats.Biome;

public class SaltBGStyle : ModSurfaceBackgroundStyle
{
	private const string Path = "Assets/Textures/Backgrounds/";

	private static ILHook ILDrawFarBG = null;
	private static int OldBgTopY = 0;
	private static float CloudMovementOffsetX = 0;

	public override void Load()
	{
		var info = typeof(SurfaceBackgroundStylesLoader).GetMethod(nameof(SurfaceBackgroundStylesLoader.DrawFarTexture));

		ILDrawFarBG = new(info, il =>
		{
			ILCursor c = new(il);

			c.EmitDelegate(static () =>
			{
				CloudMovementOffsetX += 0.2f * Main.windSpeedCurrent;
				CloudMovementOffsetX %= 2048; // Needs to be looped, otherwise the textures run out
			});

			if (!c.TryGotoNext(MoveType.After, x => x.MatchCallvirt<Main>(nameof(Main.LoadBackground))))
				return;

			const BindingFlags InstanceFlags = BindingFlags.NonPublic | BindingFlags.Instance;
			const BindingFlags StaticFlags = BindingFlags.NonPublic | BindingFlags.Static;

			c.Emit(OpCodes.Ldloc_S, (byte)1);

			c.Emit(OpCodes.Ldsfld, typeof(Main).GetField(nameof(Main.instance)));
			c.Emit(OpCodes.Ldfld, typeof(Main).GetField("bgLoops", InstanceFlags));

			c.Emit(OpCodes.Ldsfld, typeof(Main).GetField(nameof(Main.instance)));
			c.Emit(OpCodes.Ldfld, typeof(Main).GetField("bgStartX", InstanceFlags));

			c.Emit(OpCodes.Ldsfld, typeof(Main).GetField("bgWidthScaled", StaticFlags));
			c.Emit(OpCodes.Ldsfld, typeof(Main).GetField("ColorOfSurfaceBackgroundsModified", StaticFlags));
			c.Emit(OpCodes.Ldsfld, typeof(Main).GetField("bgScale", StaticFlags));
			c.EmitDelegate(DrawBefore);
		});
	}

	private static void DrawBefore(ModSurfaceBackgroundStyle current, int bgLoops, int bgStartX, int bgWidthScaled, Color color, float bgScale)
	{
		ref int bgTopY = ref GetBGTopY(Main.instance);

		if (current is not SaltBGStyle)
		{
			if (OldBgTopY != -1)
			{
				bgTopY = OldBgTopY;
				OldBgTopY = -1;
			}

			return;
		}

		OldBgTopY = bgTopY;

		float screenCenterY = Main.screenPosition.Y + Main.screenHeight / 2f;
		float dif = SaltFlatsSystem.SurfaceHeight * 16 - screenCenterY;
		bgTopY = (int)(dif - dif * 0.8f);

		int textureSlot = current.ChooseFarTexture();
		if (textureSlot >= 0 && textureSlot < TextureAssets.Background.Length)
		{
			Main.instance.LoadBackground(textureSlot);
			Texture2D tex = TextureAssets.Background[BackgroundTextureLoader.GetBackgroundSlot(SpiritReforgedMod.Instance, Path + "SaltBackgroundMid")].Value;

			for (int i = -2; i < bgLoops + 1; i++)
			{
				Vector2 pos = new(bgStartX + bgWidthScaled * i + CloudMovementOffsetX, GetBGTopY(Main.instance));
				Rectangle src = new(0, 0, Main.backgroundWidth[textureSlot], Main.backgroundHeight[textureSlot]);
				Main.spriteBatch.Draw(tex, pos, src, color, 0f, default, bgScale, SpriteEffects.None, 0f);
			}
		}
	}

	[UnsafeAccessor(UnsafeAccessorKind.Field, Name = "bgTopY")]
	internal static extern ref int GetBGTopY(Main main);

	public override int ChooseFarTexture() => BackgroundTextureLoader.GetBackgroundSlot(Mod, Path + "SaltBackgroundFar");
	public override void ModifyFarFades(float[] fades, float transitionSpeed)
	{
		for (int i = 0; i < fades.Length; i++)
		{
			if (i == Slot)
			{
				fades[i] += transitionSpeed;
				if (fades[i] > 1f)
					fades[i] = 1f;
			}
			else
			{
				fades[i] -= transitionSpeed;
				if (fades[i] < 0f)
					fades[i] = 0f;
			}
		}
	}
}