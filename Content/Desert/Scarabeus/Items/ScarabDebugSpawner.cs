using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace SpiritReforged.Content.Desert.Scarabeus.Items;

public class ScarabIdol : ModItem
{

	public override void SetDefaults()
	{
		Item.width = Item.height = 16;
		Item.rare = ItemRarityID.Green;
		Item.maxStack = 1;

		Item.useStyle = ItemUseStyleID.HoldUp;
		Item.useTime = Item.useAnimation = 20;

		Item.noMelee = true;
		Item.consumable = false;
		Item.autoReuse = false;

		Item.UseSound = SoundID.Item43;
	}

	public override bool CanUseItem(Player player) => !NPC.AnyNPCs(ModContent.NPCType<Scarabeus>()) && player.ZoneDesert && Main.dayTime;

	public override bool? UseItem(Player player)
	{
		if (Main.netMode == NetmodeID.SinglePlayer)
			NPC.SpawnOnPlayer(player.whoAmI, ModContent.NPCType<Scarabeus>());

		/*else if (Main.netMode == NetmodeID.MultiplayerClient && player == Main.LocalPlayer)
		{
			Vector2 spawnPos = player.Center;
			int tries = 0;
			int maxtries = 300;

			while ((Vector2.Distance(spawnPos, player.Center) <= 200 || WorldGen.SolidTile((int)spawnPos.X / 16, (int)spawnPos.Y / 16) || WorldGen.SolidTile2((int)spawnPos.X / 16, (int)spawnPos.Y / 16) || WorldGen.SolidTile3((int)spawnPos.X / 16, (int)spawnPos.Y / 16)) && tries <= maxtries)
			{
				spawnPos = player.Center + Main.rand.NextVector2Circular(800, 800);
				tries++;
			}

			if (tries >= maxtries)
				return false;

			SpiritMultiplayer.SpawnBossFromClient((byte)player.whoAmI, ModContent.NPCType<Scarabeus>(), (int)spawnPos.X, (int)spawnPos.Y);
		}*/

		//Main.PlaySound(mod.GetLegacySoundSlot(SoundType.Custom, "Sounds/BossSFX/Scarab_Roar1").WithVolume(0.3f), player.position);
		return true;
	}
}
