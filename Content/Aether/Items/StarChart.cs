using ILLogger;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using Terraria.Graphics.Light;
using Terraria.ModLoader.IO;

namespace SpiritReforged.Content.Aether.Items;

public sealed class StarChart : ModItem
{
	public sealed class StarChartPlayer : ModPlayer
	{
		public bool usedStarChart;

		public override void SaveData(TagCompound tag) => tag[nameof(usedStarChart)] = usedStarChart;
		public override void LoadData(TagCompound tag) => usedStarChart = tag.GetBool(nameof(usedStarChart));
	}

	public const float RangeIncrease = 0.2f; //20%

	public override void Load() => IL_LightingEngine.ExportToMiniMap += UpgradeMappingRange;

	private static void UpgradeMappingRange(ILContext il)
	{
		ILCursor c = new(il);
		if (!c.TryGotoNext(MoveType.After, static x => x.MatchRet()))
		{
			SpiritReforgedMod.Instance.LogIL(nameof(UpgradeMappingRange), "Ret not found.");
			return;
		}

		if (!c.TryGotoNext(MoveType.Before, static x => x.Match(OpCodes.Ldc_I4_0)))
		{
			SpiritReforgedMod.Instance.LogIL(nameof(UpgradeMappingRange), "Ldc_I4_0 not found.");
			return;
		}

		c.Index--;
		c.EmitDelegate(ModifyDimensions);
	}

	private static Rectangle ModifyDimensions(Rectangle area)
	{
		if (Main.LocalPlayer.TryGetModPlayer(out StarChartPlayer starChartPlayer) && starChartPlayer.usedStarChart)
		{
			area.Width += (int)(area.Width * RangeIncrease);
			area.Height += (int)(area.Height * RangeIncrease);
		}

		return area;
	}

	public override void SetDefaults() => Item.CloneDefaults(ItemID.PeddlersSatchel);

	public override bool? UseItem(Player player)
	{
		if (player.ItemAnimationJustStarted)
		{
			StarChartPlayer pursePlayer = player.GetModPlayer<StarChartPlayer>();
			bool didUsePurse = pursePlayer.usedStarChart;
			pursePlayer.usedStarChart = true;

			if (!didUsePurse)
			{
				if (player.whoAmI == Main.myPlayer)
					Main.NewText(this.GetLocalizedValue("StatusText"), 50, byte.MaxValue, 130);

				return true;
			}
		}

		return false;
	}
}