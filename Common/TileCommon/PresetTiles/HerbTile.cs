using SpiritReforged.Common.TileCommon.CheckItemUse;
using SpiritReforged.Content.Forest.Botanist.Items;
using Terraria.GameContent.Metadata;

namespace SpiritReforged.Common.TileCommon.PresetTiles;

/// <summary> Used for quickly building herb tiles. See <see cref="GetYield"/>. </summary>
public abstract class HerbTile : ModTile, ICheckItemUse
{
	public enum PlantStage : byte
	{
		Planted,
		Growing,
		Grown
	}

	private const int FrameWidth = 18; // A constant for readability and to kick out those magic numbers
	public static readonly HashSet<int> HerbTypes = [TileID.BloomingHerbs, TileID.MatureHerbs];

	public override void SetStaticDefaults()
	{
		Main.tileFrameImportant[Type] = true;
		Main.tileObsidianKill[Type] = true;
		Main.tileCut[Type] = true;
		Main.tileNoFail[Type] = true;
		//Main.tileAlch[Type] = true; //Prevents the tile from existing on normal anchors

		TileID.Sets.ReplaceTileBreakUp[Type] = true;
		TileID.Sets.IgnoredInHouseScore[Type] = true;
		TileID.Sets.IgnoredByGrowingSaplings[Type] = true;
		TileID.Sets.SwaysInWindBasic[Type] = true;

		TileMaterials.SetForTileId(Type, TileMaterials._materialsByName["Plant"]);
		HerbTypes.Add(Type);

		HitSound = SoundID.Grass;
		DustType = DustID.Grass;
	}

	public override bool CanPlace(int i, int j)
	{
		Tile tile = Framing.GetTileSafely(i, j);

		if (!tile.HasTile)
			return true;

		int tileType = tile.TileType;
		if (tileType == Type)
		{
			PlantStage stage = GetStage(i, j);
			return stage == PlantStage.Grown;
		}
		else
		{
			if (Main.tileCut[tileType] || TileID.Sets.BreakableWhenPlacing[tileType] || tileType == TileID.WaterDrip || tileType == TileID.LavaDrip || tileType == TileID.HoneyDrip || tileType == TileID.SandDrip)
			{
				bool foliageGrass = tileType is TileID.Plants or TileID.Plants2;
				bool moddedFoliage = tileType >= TileID.Count && (Main.tileCut[tileType] || TileID.Sets.BreakableWhenPlacing[tileType]);
				bool harvestableVanillaHerb = Main.tileAlch[tileType] && WorldGen.IsHarvestableHerbWithSeed(tileType, tile.TileFrameX / 18);

				if (foliageGrass || moddedFoliage || harvestableVanillaHerb)
				{
					WorldGen.KillTile(i, j);

					if (!tile.HasTile && Main.netMode == NetmodeID.MultiplayerClient)
						NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, 0, i, j);

					return true;
				}
			}

			return false;
		}
	}

	public override void SetSpriteEffects(int i, int j, ref SpriteEffects spriteEffects) => spriteEffects = (i % 2 == 0) ? SpriteEffects.FlipHorizontally : default;
	public override void SetDrawPositions(int i, int j, ref int width, ref int offsetY, ref int height, ref short tileFrameX, ref short tileFrameY) => offsetY = -2;
	public override bool IsTileSpelunkable(int i, int j) => GetStage(i, j) is PlantStage.Grown;

	public virtual bool CanBeHarvested(int i, int j) => Main.tile[i, j].HasTile && GetStage(i, j) == PlantStage.Grown;
	public bool? CheckItemUse(int type, int i, int j)
	{
		if (type is ItemID.StaffofRegrowth or ItemID.AcornAxe)
		{
			if (ModContent.GetModTile(Main.tile[i, j].TileType) is HerbTile herb && herb.CanBeHarvested(i, j))
			{
				WorldGen.KillTile(i, j);
				return true;
			}
		}

		return null;
	}

	/// <summary> Gets the normal quantities of <paramref name="herbType"/> and <paramref name="seedType"/> affected by things like Staff of Regrowth. </summary>
	public static IEnumerable<Item> GetYield(int i, int j, int herbType, int seedType)
	{
		PlantStage stage = GetStage(i, j);

		int herbStack = 0;
		int seedStack = 0;

		if (stage is PlantStage.Planted)
			return [];

		var worldPosition = new Vector2(i, j).ToWorldCoordinates();
		var p = Main.player[Player.FindClosest(worldPosition, 16, 16)];
		int heldType = p.HeldItem.type;

		if (p.active && heldType is ItemID.StaffofRegrowth or ItemID.AcornAxe) //Increased yields with Staff of Regrowth, even when not fully grown
		{
			if (stage is PlantStage.Grown)
				(herbStack, seedStack) = (2, Main.rand.Next(2, 5));
			else if (stage is PlantStage.Growing)
				seedStack = Main.rand.Next(1, 3);
		}
		else if (stage is PlantStage.Grown)
			(herbStack, seedStack) = (1, Main.rand.Next(1, 4));
		else if (stage is PlantStage.Growing)
			herbStack = 1;

		if (BotanistHat.SetActive(p))
		{
			seedStack += 2;
			herbStack += 1;
		}

		return [new Item(herbType, herbStack), new Item(seedType, seedStack)];
	}

	public override void RandomUpdate(int i, int j)
	{
		Tile tile = Framing.GetTileSafely(i, j);
		PlantStage stage = GetStage(i, j);

		if (stage == PlantStage.Planted && Main.rand.NextBool()) //Grow only if just planted
		{
			tile.TileFrameX += FrameWidth;

			if (Main.netMode != NetmodeID.SinglePlayer)
				NetMessage.SendTileSquare(-1, i, j, 1);
		}
	}

	/// <summary> Gets the <see cref="PlantStage"/> of the herb at the given coordinates. </summary>
	public static PlantStage GetStage(int i, int j)
	{
		Tile tile = Framing.GetTileSafely(i, j);
		return (PlantStage)(tile.TileFrameX / FrameWidth);
	}

	/// <summary> Sets the <see cref="PlantStage"/> of the herb at the given coordinates. </summary>
	public static void SetStage(int i, int j, PlantStage stage)
	{
		Tile tile = Framing.GetTileSafely(i, j);
		tile.TileFrameX = (short)(FrameWidth * (int)stage);
	}
}
