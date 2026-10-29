using SpiritReforged.Common.ItemCommon.Backpacks;
using System.Runtime.CompilerServices;

namespace SpiritReforged.Common.PlayerCommon;

/// <summary>
/// Unused, as backpack auto-select functionality could not function as-is. Will keep for posterity/future use.
/// </summary>
internal static class AutoSelectUtils
{
	//private class AutoSelectHooks : ILoadable
	//{
	//	void ILoadable.Load(Mod mod)
	//	{
	//		On_Player.SmartSelect_PickToolForStrategy += AddBackpackForTooling;
	//	}

	//	private void AddBackpackForTooling(On_Player.orig_SmartSelect_PickToolForStrategy orig, Player self, int tX, int tY, int toolStrategy, bool wetTile)
	//	{
	//		if (self.GetModPlayer<BackpackPlayer>().backpack is { IsAir: false } backpack)
	//		{
	//			if (SmartSelect_PickToolForStrategy(tX, tY, toolStrategy, wetTile, (backpack.ModItem as BackpackItem).Items, self))
	//				return;
	//		}

	//		orig(self, tX, tY, toolStrategy, wetTile);
	//	}

	//	void ILoadable.Unload() { }
	//}

	public static bool SmartSelect_PickToolForStrategy(int tX, int tY, int toolStrategy, bool wetTile, Item[] inventory, Player player)
	{
		for (int i = 0; i < inventory.Length; i++)
		{
			int type = inventory[i].type;

			switch (toolStrategy)
			{
				case 0:
					if (ItemID.Sets.Torches[type])
					{
						SmartSelect_SelectItem(player, i);
						return true;
					}

					if (ItemID.Sets.Glowsticks[type])
					{
						SmartSelect_SelectItem(player, i);
						return true;
					}

					break;

				case 1:
					if (inventory[i].hammer > 0)
					{
						SmartSelect_SelectItem(player, i);
						return true;
					}

					break;

				case 2:
					if (inventory[i].axe > 0)
					{
						SmartSelect_SelectItem(player, i);
						return true;
					}

					break;

				case 3:
					if (inventory[i].pick > 0)
					{
						SmartSelect_SelectItem(player, i);
						return true;
					}

					break;

				case 4:
					if (inventory[i].type != ItemID.FlareGun && ItemID.Sets.Torches[type] && !ItemID.Sets.WaterTorches[type] && !ItemID.Sets.Glowsticks[type])
					{
						if (player.nonTorch == -1)
							player.nonTorch = player.selectedItem;

						if (inventory[player.selectedItem].createTile < TileID.Dirt || !TileID.Sets.Torch[inventory[player.selectedItem].createTile])
						{
							player.selectedItem = i;
							return true;
						}

						break;
					}

					if (ItemID.Sets.Glowsticks[type] & wetTile)
					{
						SmartSelect_SelectItem(player, i);
						return true;
					}

					if ((type == ItemID.FlareGun) & wetTile)
					{
						bool flag3 = false;
						for (int num3 = 57; num3 >= 0; num3--)
						{
							if (inventory[num3].ammo == inventory[i].useAmmo && inventory[num3].stack > 0)
							{
								flag3 = true;
								break;
							}
						}

						if (!flag3 && !ItemLoader.NeedsAmmo(inventory[i], player))
							flag3 = true;

						if (flag3)
						{
							SmartSelect_SelectItem(player, i);
							return true;
						}
					}
					else if (ItemID.Sets.WaterTorches[type])
					{
						SmartSelect_SelectItem(player, i);
						return true;
					}

					break;
				case 5:
					{
						if (ItemID.Sets.Torches[type])
						{
							if (player.nonTorch == -1)
								player.nonTorch = player.selectedItem;

							if (inventory[player.selectedItem].createTile < TileID.Dirt || !TileID.Sets.Torch[inventory[player.selectedItem].createTile])
							{
								player.selectedItem = i;
								return true;
							}

							break;
						}

						int thisType = type;

						switch (thisType)
						{
							case 930:
								{
									bool flag2 = false;
									for (int num2 = 57; num2 >= 0; num2--)
									{
										if (inventory[num2].ammo == inventory[i].useAmmo && inventory[num2].stack > 0)
										{
											flag2 = true;
											break;
										}
									}

									if (flag2)
									{
										SmartSelect_SelectItem(player, i);
										return true;
									}

									return false;
								}
							default:
								if (!ItemID.Sets.Glowsticks[thisType])
									return false;
								break;
							case 282:
							case 286:
							case 3002:
							case 3112:
							case 4776:
								break;
						}

						SmartSelect_SelectItem(player, i);
						return true;
					}
				case 6:
					{
						int num4 = Main.tile[tX, tY].TileFrameX / 72;
						int num5 = -1;
						switch (num4)
						{
							case 0:
								num5 = 929;
								break;
							case 1:
								num5 = 1338;
								break;
							case 2:
								num5 = 1345;
								break;
						}

						if (type == num5)
						{
							SmartSelect_SelectItem(player, i);
							return true;
						}

						break;
					}
				case 7:
					{
						ItemTrader itemTrader = TryGettingItemTraderFromBlock(null, Main.tile[tX, tY]);
						if (itemTrader != null && itemTrader.TryGetTradeOption(inventory[i], out var _))
						{
							SmartSelect_SelectItem(player, i);
							return true;
						}

						if (ItemID.Sets.ExtractinatorMode[type] >= 0)
						{
							SmartSelect_SelectItem(player, i);
							return true;
						}

						break;

						[UnsafeAccessor(UnsafeAccessorKind.StaticMethod)]
						static extern ItemTrader TryGettingItemTraderFromBlock(Player player, Tile targetBlock);
					}

				case 8:
					{
						if (ItemID.Sets.IsPaintScraper[type])
						{
							SmartSelect_SelectItem(player, i);
							return true;
						}

						break;
					}
			}
		}

		return false;
	}

	[UnsafeAccessor(UnsafeAccessorKind.Method)]
	private static extern void SmartSelect_SelectItem(Player player, int t);
}
