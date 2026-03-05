using Mono.Cecil.Cil;
using MonoMod.Cil;
using Terraria.Graphics.Effects;
using static Terraria.Main;

namespace SpiritReforged.Common.Visuals.Skies;

public class SunMoonILEdit : ModSystem
{
	private static SunMoonData _sunData;
	private static SunMoonData _moonData;
	public static SunMoonData SunDrawData => _sunData;

	public static SunMoonData MoonDrawData => _moonData;

	[System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2211:Non-constant fields should not be visible", Justification = "shut UP vs it needs to be a public static field")]
	public static bool SunMoonDrawingEnabled = true;

	public override void Load() => IL_Main.DrawSunAndMoon += EditDrawSunAndMoon; //Store the position of the sun/moon with an il edit

	public override void PreUpdateEntities() => SunMoonDrawingEnabled = true;

	/// <summary>
	/// Directly edits Main.DrawSunAndMoon to do the following:
	/// Store the position, scale, and color of the sun and moon
	/// Directly draw skies right before the sun and moon are drawn
	/// Disable sun/moon drawing, if desired, while still getting the draw data for the sun and moon
	/// </summary>
	/// <param name="il"></param>
	/// <exception cref="ILPatchFailureException"></exception>
	private void EditDrawSunAndMoon(ILContext il)
	{
		try
		{
			ILCursor cursor = new(il);
			int curIndex = 0;
			int[] storedIndex = [0, 0]; //Seperate, as the sun color is easier to grab before the calculations

			//Some notes on the meaning of the below op codes since it looks like gibberish:
			//Ldsfld refers to a static field being pushed onto the stack
			//Stloc means a value being taken from the stack and stored in a local variable
			//Ldloc means to load a variable at a specific index onto the stack
			//Starg means to store a value to an argument at a specific index
			//Ldarg means to load a value from an argument at a specific index
			//Ldc loads a constant value- Ldc_I4_1 loads 1, Ldc_I4_0 loads 0. In the context of a bool, 1 is true, and 0 is false
			//Brfalse jumps to a specified point in the code if the condition on the stack is false- used for conditionals

			//Grab the scales
			cursor.GotoNext(i => i.MatchLdsfld<Main>("ForcedMinimumZoom"));
			cursor.GotoNext(MoveType.After, i => i.MatchStloc(out curIndex));
			cursor.Emit(OpCodes.Ldloc, curIndex);
			cursor.EmitDelegate<Action<float>>(scale => _sunData.Scale = scale * 1.1f); //multiplied by 1.1f because the sun's scale is randomly multiplied by 1.1 right before drawing

			cursor.GotoNext(MoveType.After, i => i.MatchStloc(out curIndex));
			cursor.Emit(OpCodes.Ldloc, curIndex);
			cursor.EmitDelegate<Action<float>>(scale => _moonData.Scale = scale);

			//Grab the index of the sun colors
			cursor.GotoNext(i => i.MatchLdsfld<Main>("atmo")); //Jump to the nearest static variable before the colors are defined
			cursor.GotoNext(i => i.MatchLdloca(out storedIndex[0])); //Sun color 1
			cursor.GotoNext(i => i.MatchLdloca(out storedIndex[1])); //Sun color 2

			//Grab sun position
			cursor.GotoNext(i => i.MatchLdsfld<Main>("sunModY")); //Go to when the static field Main.sunModY or Main.moonModY is being pushed onto the stack
			cursor.GotoNext(MoveType.After, i => i.MatchStloc(out curIndex)); //Go one index after the sun's position is defined, and output that position's index
			cursor.Emit(OpCodes.Ldloc, curIndex); //Load the position vector onto the stack with the index we just got
			cursor.EmitDelegate<Action<Vector2>>(position => _sunData.Position = position); //Set the sun position during day, using the position vector on the stack

			//Set sun colors after they've been modified
			cursor.Emit(OpCodes.Ldloc, storedIndex[0]);
			cursor.EmitDelegate<Action<Color>>(color => _sunData.Color = color);
			cursor.Emit(OpCodes.Ldloc, storedIndex[1]);
			cursor.EmitDelegate<Action<Color>>(color2 => _sunData.SecondaryColor = color2);

			//Draw right underneath the sun
			cursor.Emit(OpCodes.Ldc_I4_1);
			cursor.EmitDelegate(DrawSkyUnderSunMoon);

			//Cancel drawing the sun if bool is enabled
			ILLabel beforeDraw = cursor.MarkLabel();
			cursor.GotoNext(MoveType.Before, i => i.MatchLdsfld<Main>("dayTime"));
			ILLabel afterDraw = cursor.MarkLabel();
			cursor.GotoLabel(beforeDraw);
			cursor.EmitLdsfld(GetType().GetField("SunMoonDrawingEnabled"));
			cursor.Emit(OpCodes.Brfalse, afterDraw);

			//Set the moon color from the modified argument
			cursor.GotoNext(i => i.MatchLdsfld<Main>("atmo"));
			cursor.GotoNext(MoveType.After, i => i.MatchStarg(out curIndex));

			cursor.EmitLdarg(curIndex); //Argument corresponding to moon color
			cursor.EmitDelegate<Action<Color>>(moonColor => _moonData.Color = moonColor);

			//Grab moon position
			cursor.GotoNext(i => i.MatchLdsfld<Main>("moonModY")); //Go to when the static field Main.sunModY or Main.moonModY is being pushed onto the stack
			cursor.GotoNext(MoveType.After, i => i.MatchStloc(out curIndex)); //Go one index after the sun's position is defined, and output that position's index
			cursor.Emit(OpCodes.Ldloc, curIndex); //Load the position vector onto the stack with the index we just got
			cursor.EmitDelegate<Action<Vector2>>(position => _moonData.Position = position); //Set the sun position during day, using the position vector on the stack

			//Draw right underneath the moon
			cursor.Emit(OpCodes.Ldc_I4_0);
			cursor.EmitDelegate(DrawSkyUnderSunMoon);

			//Cancel drawing the moon if bool is enabled
			beforeDraw = cursor.MarkLabel();
			cursor.GotoNext(MoveType.Before, i => i.MatchLdsfld<Main>("dayTime"));
			afterDraw = cursor.MarkLabel();
			cursor.GotoLabel(beforeDraw);
			cursor.EmitLdsfld(GetType().GetField("SunMoonDrawingEnabled"));
			cursor.Emit(OpCodes.Brfalse, afterDraw);

		}
		catch (Exception)
		{
			MonoModHooks.DumpIL(ModContent.GetInstance<SpiritReforgedMod>(), il);
			//throw new ILPatchFailureException(SpiritReforgedMod.Instance, il, e);
		}
	}

	private static void DrawSkyUnderSunMoon(bool isDay)
	{
		if (dayTime && !isDay || !dayTime && isDay)
			return;

		foreach (string key in AutoloadSkyDict.LoadedSkies.Keys)
		{
			if (SkyManager.Instance[key].IsActive() && SkyManager.Instance[key] is AutoloadedSky autosky)
			{
				autosky.DrawBelowSunMoon(spriteBatch);
				if (autosky.DisablesSunAndMoon)
					SunMoonDrawingEnabled = false;
			}	
		}
	}

	public struct SunMoonData
	{
		public Vector2 Position;
		public float Scale;
		public Color Color;
		public Color SecondaryColor;
		public static float Rotation //No need to il edit to get this
		{
			get
			{
				float rotation = (float)(time / dayLength) * 2f - 7.3f;
				if (!dayTime)
				{
					rotation = (float)(time / nightLength) * 2f - 7.3f;
					if (WorldGen.drunkWorldGen)
						rotation = rotation / 2 + MathHelper.Pi;
				}

				return rotation;
			}
		}
	}
}