using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Particles;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Desert.Tiles.Chains;

public class GoldChainLoop : ChainLoop
{
	public class GoldChainObject(Point16 anchor, byte segments) : ChainObject(anchor, segments)
	{
		public static readonly Asset<Texture2D> CenserObject = DrawHelpers.RequestLocal(typeof(ChainLoop), "Censer", false);
		public static readonly Asset<Texture2D> Chain = DrawHelpers.RequestLocal(typeof(ChainLoop), "GoldChain", true);

		public override Texture2D Texture => Chain.Value;

		public override void Update()
		{
			base.Update();
			Censer.EmitSmoke(Hitbox);
		}

		public override void Draw(SpriteBatch spriteBatch)
		{
			base.Draw(spriteBatch);

			var texture = CenserObject.Value;
			float rotation = ((chain == null) ? 0 : chain.EndRotation) + MathHelper.PiOver2;

			Main.EntitySpriteDraw(texture, Position - Main.screenPosition, null, Lighting.GetColor(Position.ToTileCoordinates()), rotation, texture.Size() / 2, 1, default);
		}

		public override void OnKill()
		{
			if (!Main.dedServ && chain != null)
			{
				Mod mod = SpiritReforgedMod.Instance;

				foreach (var vertex in chain.Vertices)
					Gore.NewGoreDirect(new EntitySource_Misc("Chain"), vertex.Position, Vector2.Zero, mod.Find<ModGore>("GoldChain" + Main.rand.Next(1, 4)).Type);
			}
		}
	}

	public override void AddItemRecipes(ModItem item) => item.CreateRecipe().AddIngredient(ItemID.Chain, 5).AddIngredient(AutoContent.ItemType<Censer>()).AddTile(TileID.Anvils).Register();

	public override void PostDrawPlacementPreview(int i, int j, SpriteBatch spriteBatch, Rectangle frame, Vector2 position, Color color, bool validPlacement, SpriteEffects spriteEffects)
	{
		Texture2D chainTexture = GoldChainObject.Chain.Value;
		int segments = GetSegmentCount();

		position.X += 8;
		position.Y += 8;

		for (int y = 0; y < segments; y++)
		{
			position.Y += chainTexture.Height - 2;
			spriteBatch.Draw(chainTexture, position, null, color, 0, chainTexture.Size() / 2, 1, spriteEffects, 0);

			if (y == segments - 1)
			{
				Texture2D censerTexture = GoldChainObject.CenserObject.Value;
				spriteBatch.Draw(censerTexture, position, null, color, 0, censerTexture.Size() / 2, 1, default, 0);
			}
		}
	}

	public override ChainObject Find(Point16 coords, byte segments) => new GoldChainObject(coords, segments);
}