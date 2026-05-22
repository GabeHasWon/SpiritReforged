using SpiritReforged.Common.Misc;
using SpiritReforged.Common.TileCommon.PresetTiles;
using SpiritReforged.Common.Visuals;
using Terraria.DataStructures;
using Terraria.GameContent.ObjectInteractions;

namespace SpiritReforged.Content.Desert.ScarabBoss.Items;

public sealed class ScarabRadio : ModItem
{
	public sealed class ScarabRadioTile : ModTile
	{
		public override void SetStaticDefaults()
		{
			Main.tileFrameImportant[Type] = true;
			Main.tileObsidianKill[Type] = true;

			TileID.Sets.HasOutlines[Type] = true;
			TileID.Sets.DisableSmartCursor[Type] = true;

			TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
			TileObjectData.newTile.CoordinateHeights = [16, 18];
			TileObjectData.newTile.Origin = new Point16(0, 1);
			TileObjectData.newTile.LavaDeath = false;
			TileObjectData.addTile(Type);

			RegisterItemDrop(ModContent.ItemType<ScarabRadio>()); //Register this drop for all styles
			AddMapEntry(new Color(191, 142, 111), Language.GetText("ItemName.MusicBox"));
			DustType = -1;
		}

		public override bool HasSmartInteract(int i, int j, SmartInteractScanSettings settings) => true;

		public override void EmitParticles(int i, int j, Tile tile, short tileFrameX, short tileFrameY, Color tileLight, bool visible)
		{
			if (visible && tile.TileFrameX == 36 && tile.TileFrameY % 36 == 0 && MusicBoxTile.SpawnNote)
				MusicBoxTile.SpawnMusicNote(i, j);
		}

		public override void MouseOver(int i, int j)
		{
			Player player = Main.LocalPlayer;
			player.noThrow = 2;
			player.cursorItemIconEnabled = true;
			player.cursorItemIconID = ModContent.ItemType<ScarabRadio>();
		}
	}

	public static int MusicSlot { get; private set; }
	private static readonly Asset<Texture2D> HeldTexture = DrawHelpers.RequestLocal<ScarabRadio>("ScarabRadioHeld", false);

	public override void Load() => On_PlayerDrawLayers.DrawPlayer_27_HeldItem += DrawHeldItem;

	private static void DrawHeldItem(On_PlayerDrawLayers.orig_DrawPlayer_27_HeldItem orig, ref PlayerDrawSet drawinfo)
	{
		int heldType = drawinfo.drawPlayer.HeldItem.type;
		if (heldType == ModContent.ItemType<ScarabRadio>())
		{
			Texture2D texture = HeldTexture.Value;
			Rectangle source = texture.Frame(1, 3, 0, (int)(Main.timeForVisualEffects / 8f) % 3, 0, -2);
			Vector2 origin = source.Size() / 2;

			float sine = (float)Math.Min(Math.Sin(Main.timeForVisualEffects / 5f), 0);
			Vector2 offset = new(4 * drawinfo.drawPlayer.direction, -22 + sine * 10);

			Vector2 location = (drawinfo.drawPlayer.Center - Main.screenPosition + offset + new Vector2(0, drawinfo.drawPlayer.gfxOffY)).Floor();
			Color color = drawinfo.drawPlayer.HeldItem.GetAlpha(Lighting.GetColor(drawinfo.ItemLocation.ToTileCoordinates()));
			float rotation = drawinfo.drawPlayer.itemRotation + (float)Math.Sin(Main.timeForVisualEffects / 10f) * sine * 0.3f + MathHelper.PiOver4 * drawinfo.drawPlayer.direction;

			drawinfo.DrawDataCache.Add(new DrawData(texture, location, source, color, rotation, origin, 1, drawinfo.itemEffect));
			return; //Skips orig
		}

		orig(ref drawinfo);
	}

	public override void SetStaticDefaults()
	{
		MusicSlot = MusicLoader.GetMusicSlot(Mod, "Assets/Music/RadioScarab");
		MusicLoader.AddMusicBox(Mod, MusicSlot, Type, ModContent.TileType<ScarabRadioTile>());

		ItemID.Sets.CanGetPrefixes[Type] = false;
		ItemID.Sets.ShimmerTransformToItem[Type] = ItemID.MusicBox;
	}

	public override void SetDefaults()
	{
		Item.DefaultToMusicBox(ModContent.TileType<ScarabRadioTile>());
		Item.maxStack = 1;
		Item.holdStyle = ItemHoldStyleID.HoldUp;
		Item.value = Item.sellPrice(gold: 1);
		Item.rare = ItemRarityID.Orange;
	}

	public override void HoldItem(Player player)
	{
		if (!Main.dedServ && Main.LocalPlayer.DistanceSQ(player.Center) < 250 * 250)
		{
			ChooseMusic.SetMusic(MusicSlot);

			if ((int)Main.timeForVisualEffects % 20 == 0 && Main.rand.NextBool(3))
			{
				var gore = Gore.NewGoreDirect(player.GetSource_FromThis("HeldItem"), player.Top - new Vector2(0, 20), new Vector2(0, -0.5f), Main.rand.Next(570, 573), 0.8f);
				gore.position.X -= gore.Width / 2;
			}
		}
	}
}