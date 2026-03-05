using System.Linq;
using Terraria.UI;

namespace SpiritReforged.Common.UI.System;

[Autoload(Side = ModSide.Client)]
public class UISystem : ModSystem
{
    private static readonly HashSet<AutoUIState> UIStates = [];

	/// <summary> Gets the AutoUIState of the given type. </summary>
	internal static T GetState<T>() where T : AutoUIState => UIStates.FirstOrDefault(x => x is T) as T;
	/// <summary> Checks whether the AutoUIState of the given type is active. </summary>
	internal static bool IsActive<T>() where T : AutoUIState => UIStates.FirstOrDefault(x => x is T).UserInterface.CurrentState is not null;
	/// <summary> Enables the AutoUIState of the given type. </summary>
	internal static void SetActive<T>() where T : AutoUIState => UIStates.FirstOrDefault(x => x is T).UserInterface.SetState(GetState<T>());
	/// <summary> Disables the AutoUIState of the given type. </summary>
	internal static void SetInactive<T>() where T : AutoUIState => UIStates.FirstOrDefault(x => x is T).UserInterface.SetState(null);

	public override void Load()
    {
        var uiStates = Mod.Code.GetTypes().Where(x => !x.IsAbstract && x.IsSubclassOf(typeof(AutoUIState)));
        foreach (var state in uiStates)
        {
            var s = Activator.CreateInstance(state) as AutoUIState;
            s.UserInterface = new UserInterface();

			s.Activate();
			UIStates.Add(s);
		}
	}

	public override void Unload()
	{
		foreach (var state in UIStates)
			state.Unload(Mod);
	}

	public override void UpdateUI(GameTime gameTime)
    {
        foreach (var state in UIStates)
            state.UserInterface?.Update(gameTime);
    }

    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        foreach (var state in UIStates)
        {
            int index = state.Layer(layers);
            if (index != -1)
                layers.Insert(index, new LegacyGameInterfaceLayer(
                    "SpiritReforged: UI" + state.UniqueId,
                    delegate
                    {
						if (state.UserInterface.CurrentState is not null)
							state.UserInterface?.Draw(Main.spriteBatch, new GameTime());

                        return true;
                    },
                    InterfaceScaleType.UI)
                );
        }
    }
}