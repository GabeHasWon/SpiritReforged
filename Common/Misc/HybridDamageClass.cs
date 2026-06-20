using System.Collections.ObjectModel;
using System.Linq;

namespace SpiritReforged.Common.Misc;

public class HybridDamageClass : DamageClass
{
	public sealed class HybridDamageItem : GlobalItem
	{
		public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
		{
			if (item.DamageType is not HybridDamageClass hybridDamageClass)
				return;

			string result = string.Empty;
			int totalDamage = Main.LocalPlayer.GetWeaponDamage(item, true);
			float totalWeight = hybridDamageClass.GetTotalWeight();

			foreach (TooltipLine tooltip in tooltips)
			{
				if (tooltip.Name == "Damage")
				{
					foreach (DamageSlice subClass in hybridDamageClass._subClasses)
					{
						if (result != string.Empty)
							result += '\n';

						result += $"{Math.Round(totalDamage * (float)(subClass.Weight / totalWeight))}{subClass.Class.DisplayName}";
					}

					tooltip.Text = result;
					break;
				}
			}
		}
	}

	public readonly record struct DamageSlice(DamageClass Class, float Weight);

	public ReadOnlyCollection<DamageSlice> SubClasses => new(_subClasses.ToList());

	private HashSet<DamageSlice> _subClasses = [];

	public override StatInheritanceData GetModifierInheritance(DamageClass damageClass)
	{
		foreach (DamageSlice subClass in _subClasses)
		{
			if (damageClass == subClass.Class)
				return StatInheritanceData.Full;
		}

		return StatInheritanceData.None;
	}

	public override bool GetEffectInheritance(DamageClass damageClass) => _subClasses.Any(x => x.Class == damageClass);

	public float GetTotalWeight()
	{
		float result = 0;

		foreach (DamageSlice subClass in _subClasses)
			result += subClass.Weight;

		return result;
	}

	public HybridDamageClass AddSubClass(DamageSlice subClass)
	{
		_subClasses.Add(subClass);
		return this;
	}

	public HybridDamageClass Clone()
	{
		var result = (HybridDamageClass)MemberwiseClone();
		_subClasses = new();

		return result;
	}
}