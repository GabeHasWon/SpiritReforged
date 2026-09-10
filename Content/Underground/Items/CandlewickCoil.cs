using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Common.WorldGeneration;
using SpiritReforged.Content.Underground.Tiles;

namespace SpiritReforged.Content.Underground.Items;

public class CandlewickCoil : ModItem
{
	public sealed class CandlewickCoilThrown : ModProjectile
	{
		public static readonly Asset<Texture2D> ChainTexture = DrawHelpers.RequestLocal<CandlewickCoil>(nameof(CandlewickCoil) + "_Chain", false);

		public override LocalizedText DisplayName => ModContent.GetInstance<CandlewickCoil>().DisplayName;

		public override void SetDefaults()
		{
			Projectile.CloneDefaults(ProjectileID.RopeCoil);
			Projectile.aiStyle = 0;
			Projectile.tileCollide = true;
		}

		public override void AI()
		{
			if (Projectile.ai[0]++ > 30)
				Projectile.velocity.Y += 0.5f;
		}

		public override void OnKill(int timeLeft)
		{
			const int max_length = 10;

			int length = 0;
			Point center = (Projectile.Center - Projectile.velocity).ToTileCoordinates();
			(int i, int j) = (center.X, center.Y);

			for (int y = 0; y < max_length; y++)
			{
				if (Placer.IsReplaceable(i, j + y))
				{
					WorldGen.PlaceTile(i, j + y, ModContent.TileType<Candlewick>());
					length++;
				}
				else
				{
					break;
				}
			}

			if (Projectile.owner == Main.myPlayer)
			{
				Item result = (length == 0) ? new Item(ModContent.ItemType<CandlewickCoil>()) : new Item(AutoContent.ItemType<Candlewick>(), max_length - length);
				ItemMethods.NewItemSynced(Projectile.GetSource_Death(), result, Projectile.Center);
			}
		}

		public override bool PreDrawExtras() //Rope coil drawing taken from vanilla
		{
			Vector2 vector33 = new(Projectile.position.X + (float)Projectile.width * 0.5f, Projectile.position.Y + (float)Projectile.height * 0.5f);
			Texture2D chain = ChainTexture.Value;

			float num148 = -Projectile.velocity.X;
			float num149 = -Projectile.velocity.Y;
			float num150 = 1f;

			if (Projectile.ai[0] <= 17f)
				num150 = Projectile.ai[0] / 17f;

			int num151 = (int)(30f * num150);
			float num152 = 1f;

			if (Projectile.ai[0] <= 30f)
				num152 = Projectile.ai[0] / 30f;

			float num153 = 0.4f * num152;
			float num154 = num153;
			num149 += num154;
			Vector2[] array = new Vector2[num151];
			float[] array2 = new float[num151];

			for (int k = 0; k < num151; k++)
			{
				float num155 = (float)Math.Sqrt(num148 * num148 + num149 * num149);
				float num156 = 5.6f;

				if (Math.Abs(num148) + Math.Abs(num149) < 1f)
					num156 *= Math.Abs(num148) + Math.Abs(num149) / 1f;

				num155 = num156 / num155;
				num148 *= num155;
				num149 *= num155;
				float num157 = (float)Math.Atan2(num149, num148) - 1.57f;
				array[k].X = vector33.X;
				array[k].Y = vector33.Y;
				array2[k] = num157;
				vector33.X += num148;
				vector33.Y += num149;
				num148 = 0f - Projectile.velocity.X;
				num149 = 0f - Projectile.velocity.Y;
				num154 += num153;
				num149 += num154;
			}

			for (int num158 = --num151; num158 >= 0; num158--)
			{
				vector33.X = array[num158].X;
				vector33.Y = array[num158].Y;

				float rotation17 = array2[num158];
				Color color11 = Lighting.GetColor((int)vector33.X / 16, (int)(vector33.Y / 16f));
				Main.EntitySpriteDraw(chain, new Vector2(vector33.X - Main.screenPosition.X, vector33.Y - Main.screenPosition.Y), new Rectangle(0, 0, chain.Width, chain.Height), color11, rotation17, new Vector2((float)chain.Width * 0.5f, (float)chain.Height * 0.5f), 0.8f, SpriteEffects.None);
			}

			return false;
		}
	}

	public override void SetDefaults()
	{
		Item.CloneDefaults(ItemID.RopeCoil);
		Item.shoot = ModContent.ProjectileType<CandlewickCoilThrown>();
	}

	public override void AddRecipes() => CreateRecipe().AddIngredient(AutoContent.ItemType<Candlewick>(), 10).Register();
}