using MonoMod.Cil;
using SpiritReforged.Common.ConfigurationCommon;
using SpiritReforged.Common.WorldGeneration;
using SpiritReforged.Common.WorldGeneration.Ecotones;
using SpiritReforged.Common.WorldGeneration.GenConfiguration;
using System.Reflection;
using System.Runtime.CompilerServices;
using Terraria.Audio;
using Terraria.GameContent.Generation;
using Terraria.GameContent.UI.Elements;
using Terraria.GameContent.UI.States;
using Terraria.ModLoader.IO;
using Terraria.UI;
using Terraria.WorldBuilding;

namespace SpiritReforged.Common.ModCompat.EcotoneMapper;

internal class EcotoneMapperHooks : ModSystem 
{
	public readonly record struct EcotoneEntryPair(EcotoneBase Ecotone, EcotoneSurfaceMapping.EcotoneEntry Entry);

	public static bool Enabled => CrossMod.WorldGenPreviewer.Enabled;

	public static Dictionary<int, EcotoneEntryPair> ForcedEcotones = [];

	/// <summary>
	/// If true, the world is being manually mapped.
	/// </summary>
	public static bool ActuallyManuallyMapping { get; internal set; }

	/// <summary>
	/// The current ecotone being mapped. This is set automatically if a <see cref="EcotonePass"/> is properly used.
	/// </summary>
	public static EcotoneBase MappingEcotone { get; internal set; }

	/// <summary>
	/// Used to pause world generation between threads. Set this to true to continue the generation.
	/// </summary>
	internal static bool ReadyToContinue = true;

	/// <summary>
	/// Whether this world was manually mapped or not. This is solely a marker and does nothing but show the "manually mapped" icon on the world select screen.
	/// </summary>
	[WorldBound]
	public bool ManuallyMappedWorld = false;

	/// <summary>
	/// If any ecotone of type <typeparamref name="T"/> has been forced.
	/// </summary>
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

		On_UIWorldCreation.MakeBackAndCreatebuttons += AddMapperButton;
		On_WorldGenerator.GenerateWorld += AddMappingChecks;
		On_AWorldListItem.GetIconElement += AddMappingIcon;
	}

	private UIElement AddMappingIcon(On_AWorldListItem.orig_GetIconElement orig, AWorldListItem self)
	{
		UIElement element = orig(self);

		if (DataHasMappingHeader(self))
			element.Append(new UIImage(ModContent.Request<Texture2D>("SpiritReforged/Common/ModCompat/EcotoneMapper/MappingIcon")) { Left = StyleDimension.FromPixels(-4) });

		return element;
	}

	internal static bool DataHasMappingHeader(AWorldListItem self) => self.Data.TryGetHeaderData<EcotoneMapperHooks>(out TagCompound tag) && tag.ContainsKey("manuallyMapped");

	private void AddMappingChecks(On_WorldGenerator.orig_GenerateWorld orig, WorldGenerator self, GenerationProgress progress)
	{
		List<GenPass> passes = GetPasses(self);
		int lastEcotonePass = -1;

		// Display mapping tools before every ecotone pass
		for (int i = passes.Count - 2; i >= 0; --i)
		{
			if (passes[i] is EcotonePass pass)
			{
				if (lastEcotonePass == -1)
					lastEcotonePass = i;

				passes.Insert(i, new PassLegacy(pass.Name + " Mapping", (prog, _) =>
				{
					prog.Message = Language.GetText("Mods.SpiritReforged.Generation.Mapping.Step").Format(pass.Ecotone.DisplayName.Value);
					ForcedEcotones.Clear();
					EcotoneSurfaceMapping.MapEcotones(pass.Ecotone);
				}));
			}
		}

		// And then hide it after the last one
		if (lastEcotonePass != -1)
		{
			passes.Insert(lastEcotonePass + 3, new PassLegacy("Hide Mapping", (_, _) =>
			{
				ActuallyManuallyMapping = false;
				MappingEcotone = null;
			}));
		}

		if (ActuallyManuallyMapping)
			passes.Insert(1, new PassLegacy("Mark as Mapped", (_, _) => ModContent.GetInstance<EcotoneMapperHooks>().ManuallyMappedWorld = true));

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

		int leftOffset = -274;

		if (CrossMod.RussianTranslate.Enabled)
		{
			leftOffset = -334;
		}

		UIPanel panel = new()
		{
			HAlign = 0.5f,
			VAlign = 0.5f,
			Left = StyleDimension.FromPixels(leftOffset),
			Top = StyleDimension.FromPixels(-168),
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
			Left = StyleDimension.FromPixels(0),
			OverrideSamplerState = SamplerState.PointClamp
		};

		button.OnLeftClick += FlipActuallyMapping;
		button.OnUpdate += (_) => ReframeMappingButton(button, self);
		button.OnMouseOut += (_, _) => RemoveDescription(self);
		GenConfigUIState.AddHoverTicks(button, false);
		panel.Append(button);
	}

	/// <summary>
	/// Gets the description text for a given <see cref="UIWorldCreation"/> UI state.
	/// </summary>
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

	/// <summary>
	/// This is used to hang the generation thread until it's ready to continue.
	/// </summary>
	[MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
	public static void ForcePauseOnWhatIWant()
	{
		if (!ReadyToContinue)
		{
			while (true)
			{
				if (ReadyToContinue)
					break;
			}
		}
	}

	private static Type GetModSystemType()
	{
		Type worldGenPreviewerType = ((Mod)CrossMod.WorldGenPreviewer).GetType();
		return worldGenPreviewerType.Assembly.GetType("WorldGenPreviewer.WorldGenPreviewerModSystem");
	}

	public override void SaveWorldData(TagCompound tag) => tag.Add("manuallyMapped", ManuallyMappedWorld);
	public override void LoadWorldData(TagCompound tag) => ManuallyMappedWorld = tag.GetBool("manuallyMapped");

	public override void SaveWorldHeader(TagCompound tag)
	{
		if (ManuallyMappedWorld)
			tag.Add("manuallyMapped", true);
	}
}
