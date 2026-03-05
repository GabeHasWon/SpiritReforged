using MonoMod.Cil;

namespace SpiritReforged.Common.UI;

/// <summary> Moves multiplayer pvp icons down to make room for <see cref="BackpackInterface.BackpackUIState"/> slots. </summary>
internal class MovePvpIcons : ILoadable
{
	public void Load(Mod mod) => IL_Main.DrawPVPIcons += MoveIcons;

	private static void MoveIcons(ILContext il)
	{
		ILCursor c = new(il);
		c.GotoNext(x => x.MatchStloc2());

		c.EmitDelegate(Modify);
		c.EmitAdd();
	}

	private static int Modify() => Main.EquipPage == 2 ? 54 : 0;

	public void Unload() { }
}
