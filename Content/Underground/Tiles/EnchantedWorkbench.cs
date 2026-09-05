using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.UI.Enchantment;
using SpiritReforged.Common.UI.System;
using SpiritReforged.Common.WorldGeneration;
using SpiritReforged.Common.WorldGeneration.Chests;
using SpiritReforged.Common.WorldGeneration.GenConfiguration;
using SpiritReforged.Content.Glyphs;
using SpiritReforged.Content.Particles;
using Terraria.DataStructures;
using Terraria.GameContent.Biomes.CaveHouse;
using Terraria.ModLoader.Config;
using TileHelper.Common;

namespace SpiritReforged.Content.Underground.Tiles;

public sealed class EnchantedWorkbench : ModTile, ILoadItem, IGenerationPage
{
	public const int FullFrameWidth = 18 * 3;

	#region local target
	public static bool HasCoords => ActiveCoordinates != Point16.Zero;

	/// <summary> The tile coordinates of the workbench currently in use.<br/>
	/// Valid for the <b>local client</b> only. </summary>
	public static Point16 ActiveCoordinates { get; private set; }

	public static void RemoveCoords() => ActiveCoordinates = Point16.Zero;
	#endregion

	#region worldgen
	PageInfo IGenerationPage.Info => new()
	{
		CopiedPage = new HouseLoader()
	};

	Mod IGenerationPage.Mod => SpiritReforgedMod.Instance;

	[GenConfigurable(1, 50)]
	[Slider]
	[ReverseMinMax]
	[Denominator]
	private static int EnchantedWorkbenchChance = 10;

	public override void Load() => HouseLoader.BuilderAction += FillEnchantedWorkbench;

	public static HouseLoader.BuilderResult FillEnchantedWorkbench(HouseBuilder houseBuilder)
	{
		if (houseBuilder.Type is not (HouseType.Wood or HouseType.Jungle or HouseType.Ice))
			return HouseLoader.Fail;

		bool placedWorkbench = false;
		bool filledChest = false;

		foreach (Rectangle room in houseBuilder.Rooms)
		{
			if (!placedWorkbench && WorldGen.genRand.NextBool(EnchantedWorkbenchChance) && HouseLoader.TryPlace(room, ModContent.TileType<EnchantedWorkbench>(), out PlaceAttempt placeAttempt))
			{
				placedWorkbench = true;
			}

			if (placedWorkbench && !filledChest && HouseLoader.TryFindChest(room, out Chest chest) && Array.FindIndex(chest.item, static (x) => x.IsAir) is int index && index != -1) //Search for the first instance of air
			{
				ChestPoolUtils.PlaceChestItems([new ChestPoolUtils.ChestInfo(3, 7, 1f, ModContent.ItemType<ChromaticWax>())], chest, index);
				filledChest = true;
			}
		}

		return HouseLoader.Success;
	}
	#endregion

	void ILoadItem.SetItemDefaults(ModItem modItem) => modItem.Item.placeStyle = 1;

	public override void SetStaticDefaults()
	{
		Main.tileFrameImportant[Type] = true;
		Main.tileOreFinderPriority[Type] = 600;
		Main.tileSpelunker[Type] = true;
		Main.tileNoFail[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style3x4);
		TileObjectData.newTile.Origin = new(2, 3);
		TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop, TileObjectData.newTile.Width, 0);
		TileObjectData.newTile.DrawYOffset = 2;
		TileObjectData.addTile(Type);

		DustType = -1;
		AddMapEntry(new Color(50, 25, 55), Language.GetText("Mods.SpiritReforged.Items.EnchantedWorkbenchItem.DisplayName"));
	}

	/// <summary> Deactivates the workbench tile at the provided coordinates and syncs it. </summary>
	public static void Deactivate(int i, int j)
	{
		(i, j) = Helpers.GetTopLeft(i, j);
		for (int x = i; x < i + 3; x++)
		{
			for (int y = j; y < j + 4; y++)
			{
				Tile tile = Framing.GetTileSafely(x, y);

				if (tile.HasTile && tile.TileType == ModContent.TileType<EnchantedWorkbench>())
					tile.TileFrameX += FullFrameWidth;
			}
		}

		NetMessage.SendTileSquare(-1, i, j, 3, 4);
	}

	public override void MouseOver(int i, int j)
	{
		if (UISystem.IsActive<EnchantmentUI>() || Framing.GetTileSafely(i, j).TileFrameX >= FullFrameWidth)
			return;

		Player player = Main.LocalPlayer;

		player.cursorItemIconEnabled = true;
		player.cursorItemIconID = this.AutoItemType();
		player.noThrow = 2;
	}

	public override bool RightClick(int i, int j)
	{
		if (Framing.GetTileSafely(i, j).TileFrameX >= FullFrameWidth)
			return false;

		(i, j) = Helpers.GetTopLeft(i, j);
		ActiveCoordinates = new(i, j);

		UISystem.SetActive<EnchantmentUI>();
		Main.playerInventory = true;

		return true;
	}

	public override void NearbyEffects(int i, int j, bool closer)
	{
		if (!closer && ActiveCoordinates == new Point16(i, j) && Main.LocalPlayer.DistanceSQ(ActiveCoordinates.ToWorldCoordinates()) > 100 * 100)
			UISystem.SetInactive<EnchantmentUI>();
	}

	public override void EmitParticles(int i, int j, Tile tile, short tileFrameX, short tileFrameY, Color tileLight, bool visible)
	{
		if (visible && Framing.GetTileSafely(i, j).TileFrameX < FullFrameWidth)
		{
			Vector2 position = new Vector2(i, j).ToWorldCoordinates();
			float distance = Main.LocalPlayer.DistanceSQ(position);

			if (distance < 100 * 100)
			{
				if (Main.rand.NextBool(25))
				{
					float scale = Main.rand.NextFloat(0.5f, 1);

					ParticleHandler.SpawnParticle(Main.rand.NextBool() 
						? new ShimmerStar(position, ChromaticWax.SpecialColor.Additive(), scale, 20, -Vector2.UnitY)
						: new EmberParticle(position, -Vector2.UnitY, ChromaticWax.SpecialColor.Additive(), scale * 0.5f, 60, 1));
				}

				Lighting.AddLight(position, ChromaticWax.SpecialColor.ToVector3() * MathHelper.Clamp(1f - distance / (100 * 100), 0, 1) * 0.5f);
			}
		}
	}

	public override void KillMultiTile(int i, int j, int frameX, int frameY)
	{
		if (!Main.dedServ && UISystem.IsActive<EnchantmentUI>())
		{
			UISystem.SetInactive<EnchantmentUI>();
			Main.playerInventory = false;
		}
	}
}