using SpiritReforged.Common;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.TileCommon.PresetTiles;
using Terraria.DataStructures;
using Terraria.GameContent.ObjectInteractions;

namespace SpiritReforged.Content.Desert.Tiles;

public class SwordStand : SingleSlotTile<SwordStand.SwordStandSlot>, IAutoloadTileItem
{
	/// <summary> Indicates that a special texture should be used when placed on a <see cref="SwordStand"/>.<para/>
	/// This automatically registers a type to <see cref="SpiritSets.IsSword"/> in <see cref="RegisterIsSword"/>. </summary>
	public interface ISwordStandTexture
	{
		public static readonly Dictionary<int, Asset<Texture2D>> TextureByType = [];

		/// <summary> The texture to use, cached in <see cref="TextureByType"/>. </summary>
		public Asset<Texture2D> StandTexture { get; }
	}

	public class SwordStandSlot : SingleSlotEntity
	{
		public override bool CanAddItem(Item item) => SpiritSets.IsSword[item.type];

		public override bool IsTileValidForEntity(int x, int y)
		{
			Tile tile = Framing.GetTileSafely(x, y);
			return TileObjectData.IsTopLeft(tile) && tile.TileType == ModContent.TileType<SwordStand>();
		}

		public void Draw(Point topLeft, SpriteBatch spriteBatch)
		{
			Vector2 position = topLeft.ToWorldCoordinates(24, 12) - Main.screenPosition + TileExtensions.TileOffset;
			Color lightColor = Lighting.GetColor(new Point(topLeft.X + 1, topLeft.Y));

			if (ISwordStandTexture.TextureByType.TryGetValue(item.type, out var asset))
			{
				Texture2D texture = asset.Value;
				spriteBatch.Draw(texture, position, null, lightColor, 0, texture.Size() / 2, 1, SpriteEffects.None, 0);
			}
			else
			{
				Texture2D texture = TextureAssets.Item[item.type].Value;
				spriteBatch.Draw(texture, position, null, lightColor, MathHelper.PiOver4, texture.Size() / 2, 1, SpriteEffects.None, 0);
			}
		}
	}

	private void RegisterIsSword()
	{
		//Register vanilla items as swords
		for (int type = 0; type < ItemID.Count; type++)
		{
			Item item = ContentSamples.ItemsByType[type];
			if (item.DamageType.CountsAsClass(DamageClass.Melee) && !item.noMelee && item.pick == 0 && item.axe == 0 && item.hammer == 0)
				SpiritSets.IsSword[type] = true;
		}

		//Register ISwordStandTexture members as IsSword
		foreach (ModItem item in Mod.GetContent<ModItem>())
		{
			if (item is ISwordStandTexture s)
			{
				SpiritSets.IsSword[item.Type] = true;
				ISwordStandTexture.TextureByType.Add(item.Type, s.StandTexture);
			}
		}
	}

	public override void SetStaticDefaults()
	{
		Main.tileTable[Type] = true;
		Main.tileFrameImportant[Type] = true;
		Main.tileNoAttach[Type] = true;
		Main.tileLavaDeath[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style3x2);
		TileObjectData.newTile.CoordinateHeights = [16, 18];
		TileObjectData.newTile.StyleWrapLimit = 2;
		TileObjectData.newTile.StyleMultiplier = 2;
		TileObjectData.newTile.StyleHorizontal = true;
		TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop, TileObjectData.newTile.Width, 0);
		ModTileEntity tileEntity = ModContent.GetInstance<SwordStandSlot>();
		TileObjectData.newTile.HookPostPlaceMyPlayer = new PlacementHook(tileEntity.Hook_AfterPlacement, -1, 0, false);

		TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile);
		TileObjectData.newAlternate.AnchorWall = true;
		TileObjectData.newAlternate.AnchorBottom = AnchorData.Empty;
		TileObjectData.addAlternate(1);
		TileObjectData.addTile(Type);

		TileID.Sets.HasOutlines[Type] = true;
		TileID.Sets.DisableSmartCursor[Type] = true;

		LocalizedText name = CreateMapEntryName();
		AddMapEntry(new Color(140, 140, 140), name);
		DustType = -1;

		RegisterIsSword();
	}

	public override bool HasSmartInteract(int i, int j, SmartInteractScanSettings settings) => true;

	public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
	{
		if (TileObjectData.IsTopLeft(i, j) && Entity(i, j) is SwordStandSlot entity)
			entity.Draw(new(i, j), spriteBatch);

		return true;
	}
}