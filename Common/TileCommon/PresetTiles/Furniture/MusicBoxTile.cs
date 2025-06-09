using SpiritReforged.Common.ItemCommon;
using Terraria.DataStructures;
using Terraria.GameContent.Drawing;
using Terraria.GameContent.ObjectInteractions;
using Terraria.Utilities;

namespace SpiritReforged.Common.TileCommon.PresetTiles;

//Though this is technically a furniture helper, don't use FurnitureTile because it autoloads an item
public abstract class MusicBoxTile : ModTile
{
	public abstract string MusicPath { get; }

	/// <summary> Functions like <see cref="ModType.Load"/> and handles item autoloading. </summary>
	public override void Load() => Mod.AddContent(new AutoloadedMusicBoxItem(MusicPath, Name + "Item", Texture + "Item", Name));

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

		RegisterItemDrop(this.AutoItem().type); //Register this drop for all styles
		AddMapEntry(new Color(200, 200, 200), Language.GetText("ItemName.MusicBox"));
		DustType = -1;
	}

	public override void DrawEffects(int i, int j, SpriteBatch spriteBatch, ref TileDrawInfo drawData) //Spawn music notes
	{
		bool chance = (int)Main.timeForVisualEffects % 7 == 0 && Main.rand.NextBool(3);
		if (Lighting.UpdateEveryFrame && new FastRandom(Main.TileFrameSeed).WithModifier(i, j).Next(4) != 0 || !chance)
			return;

		var tile = Framing.GetTileSafely(i, j);
		if (!TileDrawing.IsVisible(tile) || tile.TileFrameX != 36 || tile.TileFrameY % 36 != 0)
			return;

		int goreType = Main.rand.Next(570, 573);
		var position = new Vector2(i, j) * 16 + new Vector2(8, -8);
		var velocity = new Vector2(Main.WindForVisuals * 2f, -0.5f) * new Vector2(Random(), Random());
		var gore = Gore.NewGoreDirect(new EntitySource_TileUpdate(i, j), position, velocity, goreType, .8f);
		gore.position.X -= gore.Width / 2;

		return;

		static float Random() => Main.rand.NextFloat(.5f, 1.5f);
	}

	public override void MouseOver(int i, int j)
	{
		Player player = Main.LocalPlayer;
		player.noThrow = 2;
		player.cursorItemIconEnabled = true;
		player.cursorItemIconID = this.AutoItem().type;
	}

	public override bool HasSmartInteract(int i, int j, SmartInteractScanSettings settings) => true;
}

public sealed class AutoloadedMusicBoxItem(string musicPath, string name, string texture, string tileName) : ModItem
{
	private string _musicPath = musicPath;
	private string _name = name;
	private string _texture = texture;
	private string _tileName = tileName;

	public override string Name => _name;

	public override string Texture => _texture;

	protected override bool CloneNewInstances => true;

	public override ModItem Clone(Item newEntity) //Prevents the need for a parameterless constructor
	{
		var item = base.Clone(newEntity) as AutoloadedMusicBoxItem;
		item._musicPath = _musicPath;
		item._name = _name;
		item._tileName = _tileName;
		return item;
	}

	public override void SetStaticDefaults()
	{
		MusicLoader.AddMusicBox(Mod, MusicLoader.GetMusicSlot(Mod, _musicPath), Type, Mod.Find<ModTile>(_tileName).Type);

		ItemID.Sets.CanGetPrefixes[Type] = false;
		ItemID.Sets.ShimmerTransformToItem[Type] = ItemID.MusicBox;
	}

	public override void SetDefaults() => Item.DefaultToMusicBox(Mod.Find<ModTile>(_tileName).Type, 0);
}
