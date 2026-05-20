using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoMod.Cil;
using SpiritReforged.Common.ConfigurationCommon;
using System.Reflection;
using Terraria.GameContent.Generation;
using Terraria.UI;
using Terraria.WorldBuilding;

namespace SpiritReforged.Common.ModCompat.EcotoneMapper;

internal class EcotoneMapperHooks : ModSystem 
{
	public static bool Enabled => CrossMod.WorldGenPreviewer.Enabled;
	public static bool DebugEnabled => Enabled && ModContent.GetInstance<ReforgedClientConfig>().DebugEcotones;
	public static bool ManualEnabled => Enabled && ActuallyManuallyMapping;
	public static bool AnyEnabled => ManualEnabled && DebugEnabled;

	public static bool ActuallyManuallyMapping { get; internal set; }

	protected static bool ReadyToContinue = true;

	public override void PostSetupContent()
	{
		if (!Enabled)
			return;

		//MethodInfo updateMapMethod = GetModSystemType().GetMethod("UpdateMap", BindingFlags.NonPublic | BindingFlags.Instance);
		MethodInfo generateWorldDetour = GetModSystemType().GetMethod("On_WorldGenerator_GenerateWorld", BindingFlags.NonPublic | BindingFlags.Instance);
		//MonoModHooks.Modify(updateMapMethod, ModifyUpdateMap);
		MonoModHooks.Modify(generateWorldDetour, ModifyGenerateWorld);

		Type worldGenPreviewerType = ((Mod)CrossMod.WorldGenPreviewer).GetType();
		Type uiWorldLoadType = worldGenPreviewerType.Assembly.GetType("WorldGenPreviewer.UIWorldLoadSpecial");

		MonoModHooks.Add(uiWorldLoadType.GetMethod("DrawSelf", BindingFlags.Instance | BindingFlags.NonPublic), DetourDrawSelf);

		On_Main.Update += SimpleCheck;
	}

	public static void DetourDrawSelf(Action<UIState, SpriteBatch> orig, UIState self, SpriteBatch spriteBatch)
	{
		orig(self, spriteBatch);

		EcotoneMapperDisplay.DrawSelectionAreas();
	}

	private void SimpleCheck(On_Main.orig_Update orig, Main self, GameTime gameTime)
	{
		orig(self, gameTime);

		KeyboardState state = Keyboard.GetState();

		if (state.IsKeyDown(Keys.Escape))
		{
			ReadyToContinue = true;
			ActuallyManuallyMapping = false;
		}
	}

	public static void ModifyGenerateWorld(ILContext context)
	{
		ILCursor c = new(context);
		Type systemType = GetModSystemType();

		if (!c.TryGotoNext(x => x.MatchCall(systemType.GetMethod("HandleUserInteractions", BindingFlags.NonPublic | BindingFlags.Instance))))
			return;

		c.EmitDelegate(ForcePauseOnWhatIWant);
	}

	public override void ModifyWorldGenTasks(List<GenPass> tasks, ref double totalWeight)
	{
		if (Enabled && ModContent.GetInstance<ReforgedClientConfig>().DebugEcotones)
		{
			int index = tasks.FindIndex(x => x.Name == "Clean Up Dirt");

			if (index != -1)
			{
				tasks.Insert(index, new PassLegacy("Manual Ecotone Mapper", (progress, config) =>
				{
					ActuallyManuallyMapping = true;
					ReadyToContinue = false;
				}));
			}
		}
	}

	public static void ForcePauseOnWhatIWant()
	{
		if (!ReadyToContinue)
		{
			while (true)
			{
				// Holds the thread until we want something to do with it.
				if (ReadyToContinue)
					break;
			}
		}
	}

	public static void ModifyUpdateMap(ILContext context)
	{
		ILCursor c = new(context);
		Type systemType = GetModSystemType();

		if (!c.TryGotoNext(x => x.MatchStsfld(systemType.GetField("contents", BindingFlags.NonPublic | BindingFlags.Static))))
			return;
	}

	private static Type GetModSystemType()
	{
		Type worldGenPreviewerType = ((Mod)CrossMod.WorldGenPreviewer).GetType();
		return worldGenPreviewerType.Assembly.GetType("WorldGenPreviewer.WorldGenPreviewerModSystem");
	}
}
