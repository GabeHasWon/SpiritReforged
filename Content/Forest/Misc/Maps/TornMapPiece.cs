using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.ModCompat.Classic;
using SpiritReforged.Common.WorldGeneration.Noise;
using Terraria.Audio;
using Terraria.GameContent.ItemDropRules;

namespace SpiritReforged.Content.Forest.Misc.Maps;

[FromClassic("MapScroll")]
public class TornMapPiece : ModItem
{
	public override void SetStaticDefaults()
	{
		Item.ResearchUnlockCount = 3;

		CrateDatabase.AddCrateRule(ItemID.WoodenCrate, ItemDropRule.Common(Type, 5));
		CrateDatabase.AddCrateRule(ItemID.WoodenCrateHard, ItemDropRule.Common(Type, 5));
	}

	public override void SetDefaults()
	{
		Item.width = Item.height = 28;
		Item.maxStack = Item.CommonMaxStack;
		Item.value = 1000;
		Item.rare = ItemRarityID.White;
		Item.useAnimation = Item.useTime = 30;
		Item.useStyle = ItemUseStyleID.HoldUp;
		Item.consumable = true;
	}

	public override bool? UseItem(Player player)
	{
		const int Radius = 170;

		if (Main.myPlayer == player.whoAmI && !Main.dedServ)
		{
			for (int k = 0; k < 10; k++)
			{
				var dust = Dust.NewDustDirect(player.Center, player.width, player.height, DustID.PortalBolt);
				var vector = new Vector2(Main.rand.NextFloat(-1, 1), Main.rand.NextFloat(-1, 1)) * (Main.rand.Next(50, 100) * 0.04f);

				dust.velocity = vector;
				dust.noGravity = true;
				dust.position = player.Center - Vector2.Normalize(vector) * 34f;
			}

			var point = (player.Center / 16).ToPoint16();
			LightMap(point.X, point.Y, Radius); //Only light up the user's map

			SoundEngine.PlaySound(new SoundStyle("SpiritReforged/Assets/SFX/Item/PageFlip") { Pitch = .5f }, player.Center);
		}

		return true;
	}

	private static void LightMap(int x, int y, int radius)
	{
		int size = radius / 2;
		bool playSound = false;

		for (int i = x - size; i <= x + size; ++i)
		{
			for (int j = y - size; j <= y + size; ++j)
			{
				if (!WorldGen.InWorld(i, j))
					continue;

				float dist = 1 - Vector2.Distance(new Vector2(x, y), new Vector2(i, j)) / (radius * 0.5f);
				float noise = 1f - NoiseSystem.Perlin(i * 2, j * 2) * .75f;

				byte addedLight = (byte)Math.Clamp(255 * dist * 2.5f * noise, 0, 255);

				if (WorldGen.SolidTile(i, j))
					addedLight = (byte)(addedLight * .5f);

				if (addedLight > 0)
				{
					Main.Map.UpdateLighting(i, j, addedLight);

					if (ScanForTreasure(i, j))
						playSound = true;
				}
			}
		}

		Main.refreshMap = true;

		if (playSound)
		{
			SoundEngine.PlaySound(SoundID.CoinPickup with { Pitch = -.5f });
			SoundEngine.PlaySound(SoundID.Coins with { Pitch = 1 });
		}
	}

	/// <returns> Whether a treasure was found at the given coordinates. </returns>
	private static bool ScanForTreasure(int x, int y)
	{
		int type = Main.tile[x, y].TileType;

		if (TileID.Sets.FriendlyFairyCanLureTo[type] && TileObjectData.IsTopLeft(x, y))
		{
			Main.TriggerPing(new Vector2(x, y) + Vector2.One);
			return true;
		}

		return false;
	}

	public override void AddRecipes() => CreateRecipe(2).AddIngredient(ModContent.ItemType<TatteredMap>()).AddTile(TileID.WorkBenches).Register();
}