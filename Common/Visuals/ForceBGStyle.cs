using ILLogger;
using MonoMod.Cil;

namespace SpiritReforged.Common.Visuals;

[Autoload(Side = ModSide.Client)]
internal class BGStyleSystem : ModSystem
{
	private static readonly HashSet<ModBackgroundStyle> Overrides = [];

	public override void SetStaticDefaults()
	{
		foreach (var style in SpiritReforgedMod.Instance.GetContent<ModBackgroundStyle>())
		{
			if (style is IBGStyle)
				Overrides.Add(style);
		}

		IL_Main.GetPreferredBGStyleForPlayer += OverrideBackgroundStyle;
	}

	private static void OverrideBackgroundStyle(ILContext il)
	{
		ILCursor c = new(il);

		if (!c.TryGotoNext(x => x.MatchLdsfld("Terraria.ModLoader.GlobalBackgroundStyleLoader", "loaded")))
		{
			SpiritReforgedMod.Instance.LogIL("Override Background Style", "Instruction 'GlobalBackgroundStyleLoader.loaded' not found.");
			return;
		}

		c.GotoNext(x => x.MatchRet());
		c.EmitDelegate(ModifyStyle);
	}

	/// <returns> The background ID to use. </returns>
	private static int ModifyStyle(int style)
	{
		foreach (var over in Overrides)
			(over as IBGStyle).ForceBackgroundStyle(ref style);

		return style;
	}
}

/// <summary> Used to forcefully override background style. Can be applied to <see cref="ModBackgroundStyle"/>. </summary>
internal interface IBGStyle
{
	public void ForceBackgroundStyle(ref int style);
}