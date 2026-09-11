using SpiritReforged.Common.ModCompat;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.TileCommon.PresetTiles;
using SpiritReforged.Common.WorldGeneration.Noise;
using SpiritReforged.Content.Forest.Stargrass.Tiles;
using SpiritReforged.Content.Particles;

namespace SpiritReforged.Content.Crossmod.Spooky.SpookyForest.Plants;

public class GreenSpookyStargrass : GrassTile
{
	public virtual int PlantsType => ModContent.TileType<GreenStargrassPlants>();
	public virtual int VineType => ModContent.TileType<StargrassVine>();
	protected virtual Color ParticleColor => new Color(157, 130, 80) * 0.66f;

	public override bool IsLoadingEnabled(Mod mod) => CrossMod.Spooky.Enabled;

	public static Color GetGlowColor(int i, int j) 
	{
		float sine = (float)((Math.Sin(NoiseSystem.Perlin(i * 1.2f, j * 0.2f) * 3f + Main.GlobalTimeWrappedHourly * 1.3f) + 1f) * 0.5f);
		return Color.White * MathHelper.Lerp(0.2f, 1f, sine);
	}

	public override void SetStaticDefaults()
	{
		base.SetStaticDefaults();

		Main.tileLighted[Type] = true;
		TileID.Sets.Conversion.Grass[Type] = true;

		//int mowType = ModContent.TileType<StargrassMowed>();
		//SpiritSets.Mowable[Type] = (Type == mowType) ? -1 : ModContent.TileType<StargrassMowed>();
		//TileHelperSets.TileGlowmask[Type] = Helpers.RequestGlowmask(this, GetGlowColor);

		RegisterItemDrop(ItemID.DirtBlock);
		AddGrassMapEntry();
		DustType = DustID.Flare_Blue;

		int[] mergeTypes = [TileID.Vines, TileID.VineFlowers, TileID.Plants, TileID.Plants2, TileID.DyePlants];

		foreach (int type in mergeTypes)
		{
			if (TileObjectData.GetTileData(type, 0) is TileObjectData data && data.AnchorValidTiles != null)
				data.AnchorValidTiles = [.. data.AnchorValidTiles, Type]; //Allow type to anchor to THIS type
		}

		TileID.Sets.Conversion.Grass[Type] = true;
	}

	protected virtual void AddGrassMapEntry() => AddMapEntry(new Color(108, 180, 88));

	public override void FloorVisuals(Player player)
	{
		int chance = (int)Math.Clamp(50 - 7.5f * player.velocity.Length(), 1, 50);

		if (chance >= 1 && Main.rand.NextBool(chance))
			SpawnParticles(player);
	}

	internal void SpawnParticles(Player player)
	{
		if (Main.rand.NextBool(5))
		{
			int type = DustID.YellowStarDust;
			Dust.NewDust(player.Bottom, player.width, 4, type, Main.rand.NextFloat(-1f, 1), Main.rand.NextFloat(-2f, -1f));
		}
		else
		{
			Vector2 velocity = new Vector2(0, -1).RotatedByRandom(MathHelper.PiOver2) * Main.rand.NextFloat(0.9f, 1.5f);
			bool left = true;

			ParticleHandler.SpawnParticle(new GlowParticle(player.Bottom + new Vector2(Main.rand.Next(player.width), 0), velocity,
				ParticleColor, Main.rand.NextFloat(0.35f, 0.5f), 60, 10, p =>
				{
					p.Velocity = p.Velocity.RotatedBy(left ? 0.1f : -0.1f);

					if (p.Velocity.Y > 0)
					{
						left = !left;
						p.Velocity = p.Velocity.RotatedBy(left ? 0.1f : -0.1f);
					}
				}));
		}
	}

	public override bool TileFrame(int i, int j, ref bool resetFrame, ref bool noBreak)
	{
		if (!WorldGen.TileIsExposedToAir(i, j))
			Main.tile[i, j].TileType = TileID.Grass;

		return true;
	}

	public override void GrowPlants(int i, int j)
	{
		if (Main.rand.NextBool(5) && WorldGen.GrowMoreVines(i, j) && Main.tile[i, j + 1].LiquidType != LiquidID.Lava)
			Placer.GrowVine(i, j + 1, ModContent.TileType<StargrassVine>());

		if (!Main.rand.NextBool(4) || Framing.GetTileSafely(i, j - 1).HasTile)
			return;

		int style = Main.rand.Next(StargrassFlowers.StyleRange);

		WorldGen.PlaceObject(i, j - 1, PlantsType, true, style);
		NetMessage.SendObjectPlacement(-1, i, j - 1, PlantsType, style, 0, -1, -1);
	}

	public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b) => (r, g, b) = (0.05f, 0.5f, 0.2f);

	public override void PostDraw(int i, int j, SpriteBatch spriteBatch)
	{
		if (!TileMethods.GetVisualInfo(i, j, out Color color, out Texture2D tex))
			return;

		Tile tile = Main.tile[i, j];
		spriteBatch.Draw(tex, new Vector2(i, j) * 16, new Rectangle(tile.TileFrameX, tile.TileFrameY + tex.Height / 2, 16, 16), GetGlowColor(i, j).MultiplyRGB(color));
	}
}

public class OrangeSpookyStargrass : GreenSpookyStargrass
{
	public override int PlantsType => ModContent.TileType<OrangeStargrassPlants>();
	protected override Color ParticleColor => new Color(77, 196, 25) * 0.66f;

	protected override void AddGrassMapEntry() => AddMapEntry(new Color(255, 156, 67));
	public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b) => (r, g, b) = (0.35f, 0.35f, 0.05f);
}