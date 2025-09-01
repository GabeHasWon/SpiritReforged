using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Content.Desert.Tiles.Amber;
using SpiritReforged.Content.Particles;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Desert;

public class SunDropper : ModItem
{
	public override void SetStaticDefaults()
	{
		Item.ResearchUnlockCount = 99;
		ItemID.Sets.DisableAutomaticPlaceableDrop[Type] = true;
	}

	public override void SetDefaults()
	{
		Item.CloneDefaults(ItemID.MagicHoneyDropper);
		Item.createTile = ModContent.TileType<LightShaft>();
	}

	public override void AddRecipes() => CreateRecipe(8)
		.AddIngredient(ItemID.EmptyDropper, 8)
		.AddIngredient(AutoContent.ItemType<PolishedAmber>())
		.AddTile(TileID.CrystalBall)
		.Register();
}

public class LightShaft : ModTile
{
	public override string Texture => AssetLoader.EmptyTexture;

	public override void SetStaticDefaults()
	{
		Main.tileFrameImportant[Type] = true;
		Main.tileBlockLight[Type] = false;
		Main.tileLighted[Type] = true;

		TileID.Sets.DoesntPlaceWithTileReplacement[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style1x1);
		TileObjectData.newTile.AnchorTop = new AnchorData(AnchorType.SolidTile | AnchorType.SolidBottom, 1, 0);
		TileObjectData.newTile.AnchorBottom = AnchorData.Empty;
		TileObjectData.addTile(Type);

		AddMapEntry(Color.LightGoldenrodYellow);
		DustType = -1;
	}

	public override void KillTile(int i, int j, ref bool fail, ref bool effectOnly, ref bool noItem)
	{
		if (!fail)
		{
			for (int x = 0; x < 8; x++)
			{
				var center = new Vector2(i, j).ToWorldCoordinates();
				ParticleHandler.SpawnParticle(new GlowParticle(center - new Vector2(0, 8), Main.rand.NextVector2Unit() * Main.rand.NextFloat(), Color.Goldenrod, Main.rand.NextFloat(0.2f, 0.8f), 30));
			}
		}
	}

	public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b) => (r, g, b) = (0.5f, 0.5f, 0.1f);
	public override void NearbyEffects(int i, int j, bool closer)
	{
		const int size = 12;

		if (closer && !Main.gamePaused && Main.rand.NextBool(30))
		{
			var center = new Vector2(i, j).ToWorldCoordinates();
			ParticleHandler.SpawnParticle(new GlowParticle(new Vector2(center.X + Main.rand.Next(-size, size), center.Y), Vector2.UnitY * Main.rand.NextFloat(), Color.Goldenrod, Main.rand.NextFloat(0.2f, 0.8f), 180));
		}
	}

	public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
	{
		var texture = AssetLoader.LoadedTextures["Ray"].Value;
		var center = new Vector2(i, j).ToWorldCoordinates(8, -8);
		float strength = 0.5f + center.X * center.Y / 10f % 1f * 0.5f;

		for (int x = 0; x < 3; x++)
		{
			float lerp = (float)Math.Sin((Main.timeForVisualEffects + x * 200f) / 50f);
			var position = center + TileExtensions.TileOffset - Main.screenPosition + new Vector2(6f * lerp, 0);

			spriteBatch.Draw(texture, position, null, Color.Goldenrod.Additive() * (0.1f + 0.05f * lerp) * strength, 0, new Vector2(texture.Width / 2, 0), 1.5f, default, 0);
			spriteBatch.Draw(texture, position, null, Color.White.Additive() * (0.05f + 0.025f * lerp) * strength, 0, new Vector2(texture.Width / 2, 0), 0.5f, default, 0);
		}

		return false;
	}
}