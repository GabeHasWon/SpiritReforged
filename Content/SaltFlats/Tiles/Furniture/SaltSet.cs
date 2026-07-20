using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Content.SaltFlats.Tiles.Salt;
using TileHelper.Common;
using TileHelper.Content.Tiles;
using static TileHelper.Autoloader;

namespace SpiritReforged.Content.SaltFlats.Tiles.Furniture;

public class SaltSet : ILoadable
{
	public void Load(Mod mod) => ILoadItem.PostAutoloadItems += LoadSaltFurniture;

	private static void LoadSaltFurniture()
	{
		string saltName = typeof(SaltSet).Namespace + ".Salt";
		TileHelper.ArgumentCollection arguments = AllArgs(DustID.BubbleBurst_White, new Vector3(0.75f, 0.75f, 0.95f), SaltBlock.Break, false)
			- new ClockTile()
			- new BarrelTile()
			- new BenchTile();

		arguments.Get<ChandelierTile>().WindCycle = 0;

		LoadFurnitureSet(saltName, arguments, AutoContent.ItemType<SaltPanel>());
	}

	public void Unload() { }
}

public class SaltClock : ClockTile, ILoadItem
{
	private const int FrameHeight = 90;

	public void AddItemRecipes(ModItem modItem) => DataStructures.Recipes[FurnitureName]?.Invoke(modItem, AutoContent.ItemType<SaltPanel>());

	public override void SetStaticDefaults()
	{
		base.SetStaticDefaults();

		AnimationFrameHeight = FrameHeight;
		HitSound = SaltBlock.Break;
	}

	public override void AnimateTile(ref int frame, ref int frameCounter)
	{
		if (++frameCounter >= 4)
		{
			frameCounter = 0;
			frame = ++frame % 5;
		}
	}
}