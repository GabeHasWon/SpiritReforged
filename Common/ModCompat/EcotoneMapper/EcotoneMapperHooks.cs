using Microsoft.Xna.Framework.Input;
using MonoMod.Cil;
using SpiritReforged.Common.ConfigurationCommon;
using SpiritReforged.Common.WorldGeneration.Ecotones;
using System.Reflection;
using System.Runtime.CompilerServices;
using Terraria.Audio;
using Terraria.GameContent.Generation;
using Terraria.GameContent.UI.Elements;
using Terraria.GameContent.UI.States;
using Terraria.UI;
using Terraria.WorldBuilding;

namespace SpiritReforged.Common.ModCompat.EcotoneMapper;

internal class EcotoneMapperHooks : ModSystem 
{
	public readonly record struct EcotoneEntryPair(EcotoneBase Ecotone, EcotoneSurfaceMapping.EcotoneEntry Entry);

	public static bool Enabled => CrossMod.WorldGenPreviewer.Enabled;
	public static bool DebugEnabled => Enabled && ModContent.GetInstance<ReforgedClientConfig>().DebugEcotones;
	public static bool ManualEnabled => Enabled && ActuallyManuallyMapping;
	public static bool AnyEnabled => ManualEnabled && DebugEnabled;

	public static Dictionary<int, EcotoneEntryPair> ForcedEcotones = [];

	/// <summary>
	/// Whether the user actually wants the manual ecotone mapping system.
	/// </summary>
	public static bool ActuallyManuallyMapping { get; internal set; }

	public static EcotoneBase MappingEcotone { get; internal set; }

	/// <summary>
	/// Used to pause world generation.
	/// </summary>
	internal static bool ReadyToContinue = true;

	public static bool AnyForced<T>() where T : EcotoneBase
	{
		foreach (EcotoneEntryPair pair in ForcedEcotones.Values)
			if (pair.Ecotone is T)
				return true;

		return false;
	}

	public override void PreWorldGen() => ForcedEcotones.Clear();
	public override void PostWorldGen() => ActuallyManuallyMapping = false;

	public override void PostSetupContent()
	{
		if (!Enabled)
			return;
		
		MethodInfo generateWorldDetour = GetModSystemType().GetMethod("On_WorldGenerator_GenerateWorld", BindingFlags.NonPublic | BindingFlags.Instance);
		MonoModHooks.Modify(generateWorldDetour, ModifyGenerateWorld);

		Type worldGenPreviewerType = ((Mod)CrossMod.WorldGenPreviewer).GetType();
		Type uiWorldLoadType = worldGenPreviewerType.Assembly.GetType("WorldGenPreviewer.UIWorldLoadSpecial");

		MonoModHooks.Add(uiWorldLoadType.GetMethod("DrawSelf", BindingFlags.Instance | BindingFlags.NonPublic), DetourDrawSelf);

		On_Main.Update += SimpleCheck;
		On_UIWorldCreation.MakeBackAndCreatebuttons += AddMapperButton;
		On_WorldGenerator.GenerateWorld += AddMappingChecks;
	}

	private void AddMappingChecks(On_WorldGenerator.orig_GenerateWorld orig, WorldGenerator self, GenerationProgress progress)
	{
		List<GenPass> passes = GetPasses(self);

		for (int i = passes.Count - 2; i >= 0; --i)
		{
			if (passes[i] is EcotonePass pass)
			{
				passes.Insert(i, new PassLegacy(pass.Name + " [Re-]Mapping", (_, _) =>
				{
					EcotoneSurfaceMapping.MapEcotones(pass.Ecotone);
				}));
			}
		}

		orig(self, progress);
	}

	[UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_passes")]
	public static extern ref List<GenPass> GetPasses(WorldGenerator self);

	private void AddMapperButton(On_UIWorldCreation.orig_MakeBackAndCreatebuttons orig, UIWorldCreation self, UIElement outerContainer)
	{
		orig(self, outerContainer);

		if (!Enabled)
			return;

		ActuallyManuallyMapping = false;

		UIPanel panel = new()
		{
			HAlign = 0.5f,
			VAlign = 0.5f,
			Left = StyleDimension.FromPixels(-274),
			Top = StyleDimension.FromPixels(-218),
			Width = StyleDimension.FromPixels(40),
			Height = StyleDimension.FromPixels(40),
			PaddingLeft = 4,
			PaddingTop = 4,
			BackgroundColor = new Color(33, 43, 79) * 0.8f
		};

		self.Append(panel);

		UIImageFramed button = new(ModContent.Request<Texture2D>("SpiritReforged/Common/ModCompat/EcotoneMapper/MappingButton"), new Rectangle(0, 0, 32, 32))
		{
			Width = StyleDimension.FromPixels(32),
			Height = StyleDimension.FromPixels(32),
			Left = StyleDimension.FromPixels(0)
		};

		button.OnLeftClick += FlipActuallyMapping;
		button.OnUpdate += (_) => ReframeMappingButton(button, self);
		button.OnMouseOut += (_, _) => RemoveDescription(self);
		panel.Append(button);
	}

	[UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_descriptionText")]
	public static extern ref UIText GetDescriptionText(UIWorldCreation ui);

	private void FlipActuallyMapping(UIMouseEvent evt, UIElement listeningElement)
	{
		ActuallyManuallyMapping = !ActuallyManuallyMapping;
		ReadyToContinue = true;

		SoundEngine.PlaySound(SoundID.MenuTick);
	}

	private static void RemoveDescription(UIWorldCreation self)
	{
		UIText description = GetDescriptionText(self); // Resets description text which is set below
		description?.SetText(Language.GetText("UI.WorldDescriptionDefault"));
	}

	private static void ReframeMappingButton(UIImageFramed button, UIWorldCreation self)
	{
		bool hover = button.ContainsPoint(Main.MouseScreen);
		button.SetFrame(new Rectangle(hover ? 34 : 0, !ActuallyManuallyMapping ? 34 : 0, 32, 32));

		UIText description = GetDescriptionText(self);

		if (hover)
		{
			const string Key = "Mods.SpiritReforged.Generation.Mapping.";
			description?.SetText(Language.GetTextValue(Key + "Toggle", Language.GetTextValue(Key + (ActuallyManuallyMapping ? "Disable" : "Enable"))));
		}
	}

	public static void DetourDrawSelf(Action<UIState, SpriteBatch> orig, UIState self, SpriteBatch spriteBatch)
	{
		orig(self, spriteBatch);

		EcotoneMapperDisplay.DrawSelectionAreas();
	}

	private void SimpleCheck(On_Main.orig_Update orig, Main self, GameTime gameTime)
	{
		orig(self, gameTime);

		if (Main.keyState.IsKeyDown(Keys.Escape) && Main.oldKeyState.IsKeyUp(Keys.Escape))
			ReadyToContinue = true;
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
		if (Enabled && (ModContent.GetInstance<ReforgedClientConfig>().DebugEcotones || ActuallyManuallyMapping))
		{
			int index = tasks.FindIndex(x => x.Name == "Clean Up Dirt");

			if (false && index != -1)
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
