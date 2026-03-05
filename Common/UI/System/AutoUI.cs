using Terraria.UI;

namespace SpiritReforged.Common.UI.System;

public abstract class AutoUIState : UIState
{
    public UserInterface UserInterface { get; set; }
    public virtual int Layer(List<GameInterfaceLayer> layers) => layers.FindIndex(layer => layer.Name.Equals("Vanilla: Inventory"));
	public virtual void Unload(Mod mod) { }
}
