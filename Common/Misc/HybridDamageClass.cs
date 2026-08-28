using StructureHelper.Content.GUI;
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

			DamageSlice[] slices = hybridDamageClass._subClasses.ToArray();

			string result = string.Empty;
			int totalDamage = Main.LocalPlayer.GetWeaponDamage(item, true);
			float totalWeight = hybridDamageClass.GetTotalWeight();

			int index = tooltips.FindIndex(tt => tt.Mod.Equals("Terraria") && tt.Name.Equals("Damage"));

			if (index != -1 && hybridDamageClass._subClasses.Count > 0)
			{
				DamageSlice first = slices[0];
				tooltips[index].Text = $"{Math.Round(totalDamage * (float)(first.Weight / totalWeight))}{first.Class.DisplayName}";

				for (int i = 1; i < slices.Length; i++)
				{
					DamageSlice subClass = slices[i];

					tooltips.Insert(index + i, new TooltipLine(Mod, $"SpiritReforged: HybridDamageClass Line: #{i}", $"{Math.Round(totalDamage * (float)(subClass.Weight / totalWeight))}{subClass.Class.DisplayName}"));
				}
			}	
		}
	}

	public override bool UseStandardCritCalcs => true;

	public readonly record struct DamageSlice(DamageClass Class, float Weight);

	public ReadOnlyCollection<DamageSlice> SubClasses => new(_subClasses.ToList());

	private HashSet<DamageSlice> _subClasses = [];

	public override StatInheritanceData GetModifierInheritance(DamageClass damageClass)
	{
		if (damageClass == Generic)
			return StatInheritanceData.Full;

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
		result._subClasses = new HashSet<DamageSlice>(_subClasses);

		return result;
	}
}