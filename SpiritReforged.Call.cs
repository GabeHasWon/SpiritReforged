using SpiritReforged.Common.ItemCommon.Backpacks;
using SpiritReforged.Content.Forest.Safekeeper;
using SpiritReforged.Content.Savanna.Ecotone;
using SpiritReforged.Content.Underground.Pottery;
using SpiritReforged.Content.Underground.Tiles.Potion;

namespace SpiritReforged;

public partial class SpiritReforgedMod : Mod
{
	public override object Call(params object[] args)
	{
		try
		{
			if (args is null)
				Logger.Error("Call Error: Arguments are null.");

			if (args.Length == 0)
				Logger.Error("Call Error: Arguments are empty.");

			if (args[0] is not string context)
				return null;

			switch (context)
			{
				case "AddUndead":
					{
						return UndeadNPC.AddCustomUndead(args[1..]);
					}
				case "GetSavannaArea":
					{
						return SavannaEcotone.SavannaArea;
					}
				case "SetSavannaArea":
					{
						if (!WorldGen.generatingWorld)
							throw new Exception("SavannaArea is unused outside of worldgen. Are you sure you're using this right?");

						if (args.Length == 2 && args[1] is Rectangle rectangle)
							return SavannaEcotone.SavannaArea = rectangle;
						else
							throw new ArgumentException("SetSavannaArea parameters should be two elements long: (\"SetSavannaArea\", rectangle)!");
					}
				case "AddPotionVat":
					{
						return PotionColorDatabase.ParseNewPotion(args[1..]);
					}
				case "HasBackpack":
					{
						if (args[1] is not Player player)
							throw new ArgumentException("HasBackpack parameter 1 should be a Player!");

						if (args.Length > 2)
							throw new ArgumentException("HasBackpack parameters should be 2 elements long: (\"HasBackpack\", player)!");

						return player.GetModPlayer<BackpackPlayer>().backpack.ModItem is BackpackItem;
					}
				case "AddPotstiaryRecord":
					{
						return RecordHandler.ManualAddRecord(args[1..]);
					}
				case "PotDiscovered":
					{
						if (args.Length > 3)
							throw new ArgumentException("PotDiscovered parameters should be 3 elements long: (\"PotDiscovered\", string, player)");

						if (args[1] is not string key)
							throw new ArgumentException("PotDiscovered parameter 1 should be a string.");

						if (args[2] is not Player player)
							throw new ArgumentException("PotDiscovered parameter 2 should be a Player.");

						return player.GetModPlayer<RecordPlayer>().IsValidated(key);
					}
				default:
					{
						Logger.Error($"Call Error: Context '{context}' is invalid.");
						return null;
					}
			}
		}
		catch (Exception e)
		{
			Logger.Error("Call Error: " + e.Message + "\n" + e.StackTrace);
		}

		return null;
	}
}
