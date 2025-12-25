using SpiritReforged.Common.ItemCommon;
using Terraria.Audio;

namespace SpiritReforged.Common.TileCommon.PresetTiles;

public abstract class FurnitureTile : ModTile, IAutoloadTileItem
{
	public interface IFurnitureData
	{
		public ModItem Item { get; }
		public int Material { get; }
		public SoundStyle? HitSound { get; }
	}

	public readonly record struct BasicInfo(ModItem Item, int Material, int DustType = -1, SoundStyle? HitSound = null) : IFurnitureData;
	public readonly record struct LightedInfo(ModItem Item, int Material, Vector3 Light, int DustType = -1, bool Blur = false, SoundStyle? HitSound = null) : IFurnitureData;

	public virtual IFurnitureData Info => new BasicInfo(this.AutoModItem(), ItemID.None);

	public static readonly Color CommonColor = new(190, 140, 110);

	public virtual void StaticItemDefaults(ModItem item) { }
	public virtual void SetItemDefaults(ModItem item) { }
	public virtual void AddItemRecipes(ModItem item) { }

	public sealed override void SetStaticDefaults()
	{
		if (Info.Item.Type > 0)
			RegisterItemDrop(Info.Item.Type);

		HitSound = Info.HitSound ?? SoundID.Dig;

		StaticDefaults();
	}

	/// <inheritdoc cref="ModBlockType.SetStaticDefaults"/>
	public virtual void StaticDefaults() { }

	public override void NumDust(int i, int j, bool fail, ref int num) => num = 5;
}
