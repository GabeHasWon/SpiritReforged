using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Content.Particles;
using SpiritReforged.Content.Underground.Tiles;

namespace SpiritReforged.Content.Ziggurat.Tiles;

public class EnlilStatue : ModTile, IAutoloadTileItem
{
	public class EnlilBuff : ModBuff
	{
		public override void SetStaticDefaults()
		{
			Main.buffNoTimeDisplay[Type] = true;
			Main.buffNoSave[Type] = true;
		}

		public override void Update(Player player, ref int buffIndex)
		{
			player.jumpSpeedBoost += 0.8f;
			player.maxFallSpeed *= 1.5f;
			player.gravity += 0.05f;
		}
	}

	public static float GetOpacity(int x, int y) => 1f - Math.Clamp(Main.LocalPlayer.Distance(new Vector2(x, y).ToWorldCoordinates(16, 32)) / 1200f, 0, 1);

	public override void SetStaticDefaults()
	{
		Main.tileFrameImportant[Type] = true;
		Main.tileNoAttach[Type] = true;
		Main.tileLavaDeath[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
		TileObjectData.newTile.Height = 3;
		TileObjectData.newTile.Origin = new(0, 2);
		TileObjectData.newTile.CoordinateHeights = [16, 16, 18];
		TileObjectData.newTile.Direction = TileObjectDirection.PlaceLeft;
		TileObjectData.newTile.StyleHorizontal = true;

		TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile);
		TileObjectData.newAlternate.Direction = TileObjectDirection.PlaceRight; 
		TileObjectData.addAlternate(1); 
		TileObjectData.addTile(Type);

		DustType = DustID.Stone;
		AddMapEntry(new Color(107, 90, 64), CreateMapEntryName());
		RegisterItemDrop(this.AutoItemType());
	}

	public override void NearbyEffects(int i, int j, bool closer)
	{
		if (!closer)
			return;

		Main.LocalPlayer.AddBuff(ModContent.BuffType<EnlilBuff>(), 6);
	}

	public override void EmitParticles(int i, int j, Tile tile, short tileFrameX, short tileFrameY, Color tileLight, bool visible)
	{
		if (visible && TileObjectData.IsTopLeft(i, j) && Main.rand.NextBool(8))
		{
			float opacity = GetOpacity(i, j);

			if (opacity > 0)
			{
				Vector2 position = Main.rand.NextVector2FromRectangle(new(i * 16, (j + 3) * 16, 32, 2));
				float magnitude = Main.rand.NextFloat();

				ParticleHandler.SpawnParticle(new EmberParticle(position, Vector2.UnitY * -magnitude, Color.Goldenrod * opacity, (1f - magnitude) * opacity, Main.rand.Next(30, 120), 2));
			}
		}
	}

	public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
	{
		if (TileObjectData.IsTopLeft(i, j))
		{
			float opacity = GetOpacity(i, j);

			if (opacity > 0)
				GlowTileHandler.AddGlowPoint(new Rectangle(i, j + 2, 32, 16), Color.Goldenrod * opacity * 0.5f);
		}

		return true;
	}
	public void StaticItemDefaults()
	{
		ItemID.Sets.ShimmerTransformToItem[Type] = ItemID.CatBast;
		ItemID.Sets.ShimmerTransformToItem[ItemID.CatBast] = Type;
	}

	public void SetItemDefaults(ModItem item)
	{
		item.Item.rare = ItemRarityID.Blue;
		item.Item.value = Item.sellPrice(gold: 2);
	}
}