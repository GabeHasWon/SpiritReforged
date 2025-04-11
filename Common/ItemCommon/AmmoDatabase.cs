using System.Linq;

namespace SpiritReforged.Common.ItemCommon;

internal class AmmoDatabase : GlobalItem
{
	public readonly record struct AmmoSetting(int AmmoType, params int[] Items);
	private static readonly HashSet<AmmoSetting> AmmoSettings = [];

	public static void RegisterAmmo(int ammoType, params int[] items) => AmmoSettings.Add(new(ammoType, items));

	public override void SetDefaults(Item entity)
	{
		foreach (var s in AmmoSettings)
		{
			if (s.Items.Contains(entity.type))
			{
				entity.ammo = s.AmmoType;
				break;
			}
		}
	}
}