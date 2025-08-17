using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.TileCommon.PresetTiles;
using SpiritReforged.Content.SaltFlats.Tiles.Salt;
using System.IO;
using Terraria.DataStructures;
using Terraria.GameContent.ObjectInteractions;
using Terraria.ModLoader.IO;

namespace SpiritReforged.Content.SaltFlats.Tiles;

public class Osmosifier : SingleSlotTile<OsmosifierSlot>, IAutoloadTileItem
{
	private const int FrameHeight = 38;

	public void SetItemDefaults(ModItem item) => item.Item.value = Item.sellPrice(silver: 1);
	public void AddItemRecipes(ModItem item) => item.CreateRecipe()
		.AddRecipeGroup("CopperBars", 5)
		.AddIngredient(ItemID.Wire, 10)
		.AddTile(TileID.Anvils)
		.Register();

	public override void SetStaticDefaults()
	{
		base.SetStaticDefaults();

		Main.tileLighted[Type] = true;
		Main.tileFrameImportant[Type] = true;

		TileID.Sets.HasOutlines[Type] = true;
		TileID.Sets.DisableSmartCursor[Type] = true;
		TileID.Sets.InteractibleByNPCs[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
		TileObjectData.newTile.StyleHorizontal = false;
		TileObjectData.newTile.Origin = new Point16(0, 1);
		TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop, 1, 0);
		TileObjectData.newTile.HookPostPlaceMyPlayer = new PlacementHook(entity.Hook_AfterPlacement, -1, 0, false);
		TileObjectData.newTile.CoordinateHeights = [16, 18];
		TileObjectData.addTile(Type);

		AddMapEntry(new Color(212, 125, 93));
		RegisterItemDrop(ItemType);

		AnimationFrameHeight = FrameHeight;
		DustType = -1;
	}

	public override bool HasSmartInteract(int i, int j, SmartInteractScanSettings settings) => true;
	public override bool TileFrame(int i, int j, ref bool resetFrame, ref bool noBreak)
	{
		if (TileObjectData.IsTopLeft(i, j))
		{
			var bottomLeft = Main.tile[i, j + 1];
			var bottomRight = Main.tile[i + 1, j + 1];

			bool activate = bottomLeft.LiquidType == LiquidID.Water && bottomLeft.LiquidAmount > 100 && bottomRight.LiquidType == LiquidID.Water && bottomRight.LiquidAmount > 100;
			UpdateFrame(i, j, activate);

			return false;
		}

		return true;

		static void UpdateFrame(int i, int j, bool activate)
		{
			TileExtensions.GetTopLeft(ref i, ref j);

			for (int x = 0; x < 2; x++)
			{
				for (int y = 0; y < 2; y++)
				{
					var t = Main.tile[i + x, j + y];
					short setFrame = (short)((activate ? FrameHeight : 0) + 18 * y);

					if (t.TileType == ModContent.TileType<Osmosifier>())
						t.TileFrameY = setFrame;
				}
			}
		}
	}

	public override void RandomUpdate(int i, int j)
	{
		if (Main.tile[i, j].TileFrameY >= FrameHeight && Entity(i, j) is OsmosifierSlot slot)
		{
			int stack = slot.item.stack;
			slot.item = new(AutoContent.ItemType<SaltBlockDull>(), stack + Main.rand.Next(3, 9));

			if (Main.netMode != NetmodeID.SinglePlayer)
				new TileEntityData((short)slot.ID).Send();
		}
	}

	public override void AnimateTile(ref int frame, ref int frameCounter)
	{
		if (++frameCounter >= 4)
		{
			frameCounter = 0;
			frame = ++frame % 5;
		}
	}

	public override void AnimateIndividualTile(int type, int i, int j, ref int frameXOffset, ref int frameYOffset)
	{
		if (Main.tile[i, j].TileFrameY >= FrameHeight)
			frameYOffset = Main.tileFrame[type] * FrameHeight;
		else
			frameYOffset = 0; //Don't animate
	}

	public override void DrawEffects(int i, int j, SpriteBatch spriteBatch, ref TileDrawInfo drawData)
	{
		if (TileObjectData.IsTopLeft(i, j))
			Main.instance.TilesRenderer.AddSpecialLegacyPoint(i, j);
	}

	public override void SpecialDraw(int i, int j, SpriteBatch spriteBatch)
	{
		if (Entity(i, j) is OsmosifierSlot slot && !slot.item.IsAir)
		{
			var texture = TextureAssets.Item[slot.item.type].Value;
			float sine = (float)Math.Sin(Main.timeForVisualEffects / 30f);
			var position = new Vector2(i, j).ToWorldCoordinates(16, -16) - Main.screenPosition + TileExtensions.TileOffset + Vector2.UnitY * sine * 2;

			spriteBatch.Draw(texture, position, null, Color.White, 0, texture.Size() / 2, 1, default, 0);
		}
	}
}

public class OsmosifierSlot : SingleSlotEntity
{
	public override bool PlayDroppedAnimation => false;
	public override bool CanAddItem(Item item) => false;

	public override bool IsTileValidForEntity(int x, int y)
	{
		var t = Framing.GetTileSafely(x, y);
		return t.HasTile && t.TileType == ModContent.TileType<Osmosifier>() && TileObjectData.IsTopLeft(t);
	}

	public override void NetSend(BinaryWriter writer) => ItemIO.Send(item, writer, true);
	public override void NetReceive(BinaryReader reader) => item = ItemIO.Receive(reader, true);
}