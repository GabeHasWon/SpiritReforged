using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.TileCommon;
using Terraria.Audio;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Desert.Tiles.Chains;

public class ChainLoop : ModTile, IAutoloadTileItem
{
	public static byte GetSegmentCount() => (byte)(1 + Math.Abs(Player.FlexibleWandCycleOffset) % 6);

	public virtual void AddItemRecipes(ModItem item) => item.CreateRecipe().AddIngredient(ItemID.Chain, 5).AddTile(TileID.Anvils).Register();
	public override void SetStaticDefaults()
	{
		base.SetStaticDefaults();

		Main.tileNoFail[Type] = true;
		Main.tileSolid[Type] = false;
		Main.tileFrameImportant[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style1x2Top);
		TileObjectData.newTile.Height = 1;
		TileObjectData.newTile.CoordinateHeights = [16];
		TileObjectData.newTile.DrawYOffset = -8;
		TileObjectData.addTile(Type);

		AddMapEntry(new Color(150, 150, 150));
		DustType = -1;

		this.AutoItem().ResearchUnlockCount = 100;
	}

	public override void PostDrawPlacementPreview(int i, int j, SpriteBatch spriteBatch, Rectangle frame, Vector2 position, Color color, bool validPlacement, SpriteEffects spriteEffects)
	{
		Texture2D chainTexture = TextureAssets.Chain40.Value;
		position.X += 8;
		position.Y += 8;

		for (int y = 0; y < GetSegmentCount(); y++)
		{
			position.Y += chainTexture.Height - 2;
			spriteBatch.Draw(chainTexture, position, null, color, 0, chainTexture.Size() / 2, 1, spriteEffects, 0);
		}
	}

	/// <summary> Finds a <see cref="ChainObject"/> to be associated with this tile. </summary>
	public virtual ChainObject Find(Point16 coords, byte segments) => new(coords, segments);
	public override void PlaceInWorld(int i, int j, Item item)
	{
		Point16 coords = new(i, j);
		byte count = GetSegmentCount();

		ChainObjectSystem.AddObject(Find(coords, count));

		if (Main.netMode == NetmodeID.MultiplayerClient)
			new ChainObjectSystem.PlacementData(coords, count, Type).Send();

		SoundEngine.PlaySound(ChainObject.Rattle with { Pitch = 0.5f, PitchVariance = 0.5f }, new Vector2(i, j).ToWorldCoordinates());
	}

	public override void KillTile(int i, int j, ref bool fail, ref bool effectOnly, ref bool noItem)
	{
		if (!fail && !effectOnly)
			ChainObjectSystem.RemoveObject(new(i, j));
	}
}