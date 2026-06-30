using SpiritReforged.Common.Particle;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.TileCommon.PresetTiles;
using SpiritReforged.Common.WorldGeneration;
using SpiritReforged.Common.WorldGeneration.GenConfiguration;
using SpiritReforged.Content.Particles;
using Terraria.DataStructures;
using Terraria.GameContent.Biomes.CaveHouse;
using Terraria.GameContent.ObjectInteractions;
using Terraria.ModLoader.Config;
using TileHelper.Common;

namespace SpiritReforged.Content.Underground.Tiles;

public class DisplayCase : SingleSlotTile<DisplayCase.DisplayCaseSlot>, ILoadItem, IGenerationPage
{
	public class DisplayCaseSlot : SingleSlotEntity
	{
		public override bool CanAddItem(Item item) => item.accessory;

		public override bool IsTileValidForEntity(int x, int y)
		{
			Tile tile = Framing.GetTileSafely(x, y);
			return TileObjectData.IsTopLeft(tile) && tile.TileType == ModContent.TileType<DisplayCase>();
		}

		public void Draw(Point topLeft, SpriteBatch spriteBatch)
		{
			Vector2 position = topLeft.ToWorldCoordinates(16, 16) - Main.screenPosition + TileExtensions.TileOffset;
			Color lightColor = Lighting.GetColor(new Point(topLeft.X + 1, topLeft.Y));
			Point innerDimensions = new(20, 20);

			Texture2D texture = TextureAssets.Item[item.type].Value;
			Rectangle source = new((int)(Math.Round(texture.Width / 4.0, MidpointRounding.AwayFromZero) * 2.0) - innerDimensions.X / 2, (int)(Math.Round(texture.Height / 4.0, MidpointRounding.AwayFromZero) * 2.0) - innerDimensions.Y / 2, innerDimensions.X, innerDimensions.Y);
			
			spriteBatch.Draw(texture, position, source, lightColor, 0, source.Size() / 2, 1, 0, 0);
		}
	}

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
	private static int DisplayCaseChance = 6;

	public override void Load() => HouseLoader.BuilderAction += FillDisplayCase;

	public static HouseLoader.BuilderResult FillDisplayCase(HouseBuilder houseBuilder)
	{
		if (houseBuilder.Type is not HouseType.Wood)
			return HouseLoader.Fail;

		foreach (Rectangle room in houseBuilder.Rooms)
		{
			if (WorldGen.genRand.NextBool(DisplayCaseChance) && HouseLoader.TryPlace(new(room.X, room.Y + room.Height - 1, room.Width, 1), ModContent.TileType<DisplayCase>(), out PlaceAttempt placeAttempt))
			{
				placeAttempt.PostPlacement(out DisplayCaseSlot displayCaseSlot);
				displayCaseSlot.item = new Item(WorldGen.genRand.NextFromList(ItemID.BandofRegeneration, ItemID.ManaRegenerationBand, ItemID.HermesBoots, ItemID.CloudinaBottle, ItemID.Aglet));

				return new(true, nameof(HouseLoader.FillMannequin));
			}
		}

		return HouseLoader.Fail;
	}
	#endregion

	public override void SetStaticDefaults()
	{
		Main.tileTable[Type] = true;
		Main.tileFrameImportant[Type] = true;
		Main.tileNoAttach[Type] = true;
		Main.tileLavaDeath[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
		TileObjectData.newTile.Height = 3;
		TileObjectData.newTile.CoordinateHeights = [16, 16, 16];
		TileObjectData.newTile.StyleWrapLimit = 2;
		TileObjectData.newTile.StyleMultiplier = 2;
		TileObjectData.newTile.StyleHorizontal = true;
		TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop, TileObjectData.newTile.Width, 0);
		TileObjectData.newTile.HookPostPlaceMyPlayer = Hook;

		TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile);
		TileObjectData.newAlternate.AnchorWall = true;
		TileObjectData.newAlternate.AnchorBottom = AnchorData.Empty;
		TileObjectData.addAlternate(1);
		TileObjectData.addTile(Type);

		TileID.Sets.HasOutlines[Type] = true;
		TileID.Sets.DisableSmartCursor[Type] = true;

		AddMapEntry(new Color(140, 140, 140), Language.GetText("Mods.SpiritReforged.Items.DisplayCase.DisplayName"));
		DustType = -1;
	}

	public override bool HasSmartInteract(int i, int j, SmartInteractScanSettings settings) => true;

	public override void EmitParticles(int i, int j, Tile tile, short tileFrameX, short tileFrameY, Color tileLight, bool visible)
	{
		if (visible && TileObjectData.IsTopLeft(i, j) && Main.rand.NextBool(8) && Entity(i, j, false) is DisplayCaseSlot entity && !entity.item.IsAir && Lighting.Brightness(i, j) > 0.5f)
		{
			Rectangle area = new(i * 16, j * 16, 32, 32);
			ParticleHandler.SpawnParticle(new SharpStarParticle(Main.rand.NextVector2FromRectangle(area), Vector2.Zero, Color.White, 0.2f, 50, 0, AddLight: false));
		}
	}

	public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
	{
		if (TileObjectData.IsTopLeft(i, j) && Entity(i, j, false) is DisplayCaseSlot entity)
			entity.Draw(new(i, j), spriteBatch);

		return true;
	}
}