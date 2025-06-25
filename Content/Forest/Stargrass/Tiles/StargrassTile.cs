using SpiritReforged.Common;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.TileCommon.Conversion;
using SpiritReforged.Common.TileCommon.PresetTiles;
using SpiritReforged.Common.Visuals.Glowmasks;
using SpiritReforged.Common.WorldGeneration.Noise;
using SpiritReforged.Content.Particles;
using SpiritReforged.Content.Savanna.Items;
using SpiritReforged.Content.Savanna.Tiles;

namespace SpiritReforged.Content.Forest.Stargrass.Tiles;

[AutoloadGlowmask("Method:Content.Forest.Stargrass.Tiles.StargrassTile Glow")]
public class StargrassTile : GrassTile, ISetConversion
{
	public virtual ConversionHandler.Set ConversionSet => new()
	{
		{ BiomeConversionID.Corruption, TileID.CorruptGrass },
		{ BiomeConversionID.Crimson, TileID.CrimsonGrass },
		{ BiomeConversionID.Hallow, TileID.HallowedGrass },
		{ BiomeConversionID.PurificationPowder, TileID.Grass },
		{ SavannaConversion.ConversionType, ModContent.TileType<SavannaGrass>() }
	};

	public static Color Glow(object obj) 
	{
		var pos = (Point)obj;
		float sine = (float)((Math.Sin(NoiseSystem.Perlin(pos.X * 1.2f, pos.Y * 0.2f) * 3f + Main.GlobalTimeWrappedHourly * 1.3f) + 1f) * 0.5f);
		return Color.White * MathHelper.Lerp(0.2f, 1f, sine);
	}

	public override void SetStaticDefaults()
	{
		base.SetStaticDefaults();

		Main.tileLighted[Type] = true;
		TileID.Sets.Conversion.Grass[Type] = true;

		int mowType = ModContent.TileType<StargrassMowed>();
		SpiritSets.Mowable[Type] = (Type == mowType) ? -1 : ModContent.TileType<StargrassMowed>();

		RegisterItemDrop(ItemID.DirtBlock);
		AddMapEntry(new Color(28, 216, 151));
		DustType = DustID.Flare_Blue;

		this.AnchorSelfTo(TileID.Vines, TileID.VineFlowers, TileID.Plants, TileID.Plants2, TileID.DyePlants);
	}

	public override void FloorVisuals(Player player)
	{
		int chance = (int)Math.Clamp(50 - 7.5f * player.velocity.Length(), 1, 50);

		if (chance >= 1 && Main.rand.NextBool(chance))
			SpawnParticles(player);
	}

	internal static void SpawnParticles(Player player)
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
				new Color(0, 157, 227) * 0.66f, Main.rand.NextFloat(0.35f, 0.5f), 60, 10, p =>
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

	public override void Convert(int i, int j, int conversionType)
	{
		if (ConversionHandler.FindSet(nameof(StargrassTile), conversionType, out int newType))
			WorldGen.ConvertTile(i, j, newType);
	}

	public override bool TileFrame(int i, int j, ref bool resetFrame, ref bool noBreak)
	{
		if (!WorldGen.TileIsExposedToAir(i, j))
			Main.tile[i, j].TileType = TileID.Grass;

		return true;
	}

	public override void GrowPlants(int i, int j)
	{
		if (Main.rand.NextBool(5) && Main.tile[i, j + 1].LiquidType != LiquidID.Lava)
			Placer.GrowVine(i, j + 1, ModContent.TileType<StargrassVine>());

		if (!Main.rand.NextBool(4) || Framing.GetTileSafely(i, j - 1).HasTile)
			return;

		int style = Main.rand.Next(StargrassFlowers.StyleRange);

		WorldGen.PlaceObject(i, j - 1, ModContent.TileType<StargrassFlowers>(), true, style);
		NetMessage.SendObjectPlacement(-1, i, j - 1, ModContent.TileType<StargrassFlowers>(), style, 0, -1, -1);
	}

	public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b) => (r, g, b) = (0.05f, 0.2f, 0.5f);
}