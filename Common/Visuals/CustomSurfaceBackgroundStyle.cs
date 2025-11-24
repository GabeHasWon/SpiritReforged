using Microsoft.Xna.Framework.Graphics;
using System.Runtime.CompilerServices;

namespace SpiritReforged.Common.Visuals;

public abstract class CustomSurfaceBackgroundStyle : ModSurfaceBackgroundStyle
{
	public const string Path = "Assets/Textures/Backgrounds/";
	/// <summary> The identifying string of this style gathered through naming conventions. For example, the Identity of "SaltBGStyle" would be "Salt".<br/>
	/// This is used to fill <see cref="FarTexture"/>, <see cref="MiddleTexture"/> and <see cref="CloseTexture"/> automatically. </summary>
	public string Identity { get; private set; }

	public enum LayerType { Far, Middle, Close }

	public virtual int FarTexture => BackgroundTextureLoader.GetBackgroundSlot(Mod, Path + Identity + "BackgroundFar");
	public virtual int MiddleTexture => BackgroundTextureLoader.GetBackgroundSlot(Mod, Path + Identity + "BackgroundMid");
	public virtual int CloseTexture => BackgroundTextureLoader.GetBackgroundSlot(Mod, Path + Identity + "BackgroundNear");

	/// <summary><inheritdoc cref="ModType.Load"/><para/>
	/// Automatically assigns to <see cref="Identity"/>. </summary>
	public override void Load() => Identity = Name.Split("BG")[0];

	public sealed override int ChooseFarTexture() => Draw(Main.spriteBatch, LayerType.Far) ? FarTexture : -1;
	public sealed override int ChooseMiddleTexture() => Draw(Main.spriteBatch, LayerType.Middle) ? MiddleTexture : -1;
	public sealed override int ChooseCloseTexture(ref float scale, ref double parallax, ref float a, ref float b) => Draw(Main.spriteBatch, LayerType.Close) ? CloseTexture : -1;

	/// <summary> Affects draw behaviour of any given layer. </summary>
	/// <param name="spriteBatch"> The SpriteBatch to use. </param>
	/// <param name="layer"> The layer being drawn. </param>
	/// <returns> Whether default drawing should occur. </returns>
	public virtual bool Draw(SpriteBatch spriteBatch, LayerType layer) => true;

	public sealed override bool PreDrawCloseBackground(SpriteBatch spriteBatch) => true;

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

	#region static helpers
	public static Rectangle GetBounds(int slot) => new(0, 0, Main.backgroundWidth[slot], Main.backgroundHeight[slot]);

	public static Texture2D LoadBackground(int slot)
	{
		Main.instance.LoadBackground(slot);
		return TextureAssets.Background[slot].Value;
	}

	public static void DrawScroll(Action<Vector2, float> drawAction, int start = 0, int end = 0)
	{
		BackgroundStyleHelper.BackgroundLoops = Main.screenWidth / BackgroundStyleHelper.BackgroundWidthScaled + 2;
		int loops = (end == 0) ? BackgroundStyleHelper.BackgroundLoops : end;

		if (Main.screenPosition.Y < Main.worldSurface * 16.0 + 16.0)
		{
			for (int i = start; i < loops; i++)
			{
				Vector2 position = new(BackgroundStyleHelper.BackgroundStartX + BackgroundStyleHelper.BackgroundWidthScaled * i, BackgroundStyleHelper.BackgroundTopY);
				drawAction.Invoke(position, BackgroundStyleHelper.BackgroundScale);
			}
		}
	}
	#endregion
}

public static class BackgroundStyleHelper
{
	[UnsafeAccessor(UnsafeAccessorKind.StaticField, Name = "bgScale")]
	private static extern ref float BGScale(Main main);

	[UnsafeAccessor(UnsafeAccessorKind.Field, Name = "bgParallax")]
	private static extern ref double BGParallax(Main main);

	[UnsafeAccessor(UnsafeAccessorKind.StaticField, Name = "bgWidthScaled")]
	private static extern ref int BGWidthScaled(Main main);

	[UnsafeAccessor(UnsafeAccessorKind.Field, Name = "screenOff")]
	private static extern ref float SCOff(Main main);

	[UnsafeAccessor(UnsafeAccessorKind.Field, Name = "bgStartX")]
	private static extern ref int BGStartX(Main main);

	[UnsafeAccessor(UnsafeAccessorKind.Field, Name = "bgTopY")]
	private static extern ref int BGTopY(Main main);

	[UnsafeAccessor(UnsafeAccessorKind.Field, Name = "scAdj")]
	private static extern ref float SCAdj(Main main);

	[UnsafeAccessor(UnsafeAccessorKind.Field, Name = "bgLoops")]
	private static extern ref int BGLoops(Main main);

	[UnsafeAccessor(UnsafeAccessorKind.StaticField, Name = "ColorOfSurfaceBackgroundsModified")]
	private static extern ref Color ColorOfSurfaceBackgroundsModified(Main main);

	public static float BackgroundScale
	{
		get => BGScale(Main.instance);
		set => BGScale(Main.instance) = value;
	}

	public static double BackgroundParallax
	{
		get => BGParallax(Main.instance);
		set => BGParallax(Main.instance) = value;
	}

	public static int BackgroundWidthScaled
	{
		get => BGWidthScaled(Main.instance);
		set => BGWidthScaled(Main.instance) = value;
	}

	public static float ScreenOff
	{
		get => SCOff(Main.instance);
		set => SCOff(Main.instance) = value;
	}

	public static int BackgroundStartX
	{
		get => BGStartX(Main.instance);
		set => BGStartX(Main.instance) = value;
	}

	public static int BackgroundTopY
	{
		get => BGTopY(Main.instance);
		set => BGTopY(Main.instance) = value;
	}

	public static float ScreenAdj
	{
		get => SCAdj(Main.instance);
		set => SCAdj(Main.instance) = value;
	}

	public static int BackgroundLoops
	{
		get => BGLoops(Main.instance);
		set => BGLoops(Main.instance) = value;
	}

	public static Color SurfaceBackgroundModified
	{
		get => ColorOfSurfaceBackgroundsModified(Main.instance);
		set => ColorOfSurfaceBackgroundsModified(Main.instance) = value;
	}
}