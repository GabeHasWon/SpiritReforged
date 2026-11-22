using MonoMod.Cil;

namespace SpiritReforged.Common.Visuals;

/// <summary> Used to forcefully override water style. Can be applied to <see cref="ModBackgroundStyle"/>. </summary>
internal interface IWaterStyle
{
	public void ForceWaterStyle(ref int style);
}

/// <summary> Manually overrides water style in specific scenarios. Doesn't exist on the server. </summary>
[Autoload(Side = ModSide.Client)]
internal class WaterStyleSystem : ModSystem
{
	private static readonly HashSet<ModBackgroundStyle> Overrides = [];
	private static readonly Dictionary<int, int> StyleSets = []; //background, water

	public override void Load()
	{
		IL_Main.CalculateWaterStyle += SetWaterStyleDefault;
		On_Main.CalculateWaterStyle += OverrideWaterStyle;
	}

	public override void SetStaticDefaults()
	{
		if (Main.dedServ)
			return;

		foreach (var scene in Mod.GetContent<ModSceneEffect>()) //Patch transitions for our water styles
		{
			if (scene.SurfaceBackgroundStyle == null || scene.WaterStyle == null)
				continue; //Checks duplicate since not all mods set a surface background style

			StyleSets.TryAdd(scene.SurfaceBackgroundStyle.Slot, scene.WaterStyle.Slot);
		}

		foreach (var style in Mod.GetContent<ModBackgroundStyle>())
		{
			if (style is IWaterStyle)
				Overrides.Add(style);
		}
	}

	private static void SetWaterStyleDefault(ILContext il)
	{
		ILCursor c = new(il);

		c.Index = c.Instrs.Count;
		c.GotoPrev(x => x.MatchRet());

		c.EmitDelegate(Modify);
	}

	/// <summary> Modify the default case of modded water styles (purity).<br/>
	/// Used to prevent water from turning to purity before turning to the correct style during some scene transitions. </summary>
	/// <param name="style"> The current water style. </param>
	/// <returns> The replacement water style. </returns>
	private static int Modify(int style)
	{
		foreach (var set in StyleSets)
		{
			if (Main.bgStyle == set.Key)
				return set.Value;
		}

		return style;
	}

	private static int OverrideWaterStyle(On_Main.orig_CalculateWaterStyle orig, bool ignoreFountains)
	{
		int style = orig(ignoreFountains);

		foreach (var over in Overrides)
			(over as IWaterStyle).ForceWaterStyle(ref style);

		return style;
	}
}