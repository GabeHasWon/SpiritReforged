using SpiritReforged.Common.TileCommon.Tree;
using SpiritReforged.Content.Forest.Stargrass.Items;
using Terraria.Utilities;

namespace SpiritReforged.Content.Forest.Stargrass.Tiles;

public class StargrassTree : ModTree
{
	public const string TexturePath = "SpiritReforged/Content/Forest/Stargrass/Tiles/StargrassTree";

	public override TreePaintingSettings TreeShaderSettings => new()
	{
		UseSpecialGroups = true,
		SpecialGroupMinimalHueValue = 11f / 72f,
		SpecialGroupMaximumHueValue = 0.25f,
		SpecialGroupMinimumSaturationValue = 0.88f,
		SpecialGroupMaximumSaturationValue = 1f
	};

	public override void SetStaticDefaults() => GrowsOnTileId = [ModContent.TileType<StargrassTile>(), ModContent.TileType<StargrassMowed>()];
	public override int CreateDust() => DustID.WoodFurniture;
	public override int TreeLeaf() => GoreID.TreeLeaf_Normal;
	public override int DropWood() => ItemID.Wood;
	public override Asset<Texture2D> GetTexture() => ModContent.Request<Texture2D>(TexturePath);
	public override Asset<Texture2D> GetTopTextures() => ModContent.Request<Texture2D>($"{TexturePath}_Tops");
	public override Asset<Texture2D> GetBranchTextures() => ModContent.Request<Texture2D>($"{TexturePath}_Branches");

	public override int SaplingGrowthType(ref int style)
	{
		style = 0;
		return ModContent.TileType<StargrassSapling>();
	}

	public override void SetTreeFoliageSettings(Tile tile, ref int xoffset, ref int treeFrame, ref int floorY, ref int topTextureFrameWidth, ref int topTextureFrameHeight) { }

	public override bool Shake(int x, int y, ref bool createLeaves)
	{
		var options = new WeightedRandom<StargrassTreeShakeEffect>();
		options.Add(StargrassTreeShakeEffect.None, 0.8f);
		options.Add(StargrassTreeShakeEffect.Wood, 0.8f);
		options.Add(StargrassTreeShakeEffect.Acorn, 0.8f);
		options.Add(StargrassTreeShakeEffect.Critter, 0.6f);
		options.Add(StargrassTreeShakeEffect.Fruit, 0.4f);

		StargrassTreeShakeEffect effect = options;
		if (effect == StargrassTreeShakeEffect.Acorn)
		{
			Vector2 offset = this.GetRandomTreePosition(Main.tile[x, y]);
			Item.NewItem(WorldGen.GetItemSource_FromTreeShake(x, y), new Vector2(x, y) * 16 + offset, ItemID.Acorn, Main.rand.Next(1, 2));
		}
		else if (effect == StargrassTreeShakeEffect.Wood)
		{
			Vector2 offset = this.GetRandomTreePosition(Main.tile[x, y]);
			Item.NewItem(WorldGen.GetItemSource_FromTreeShake(x, y), new Vector2(x, y) * 16 + offset, 9, Main.rand.Next(1, 3));
		}
		else if (effect == StargrassTreeShakeEffect.Critter)
		{
			int repeats = Main.rand.Next(1, 4);

			for (int i = 0; i < repeats; ++i)
			{
				Vector2 offset = this.GetRandomTreePosition(Main.tile[x, y]);
				Vector2 pos = new Vector2(x * 16, y * 16) + offset;

				int npcType = Main.rand.Next(3) switch
				{
					0 => NPCID.Bird,
					1 => NPCID.BirdBlue,
					_ => NPCID.BirdRed
				};
				if (Main.rand.NextBool(50))
					npcType = NPCID.GoldBird;

				int npc = NPC.NewNPC(WorldGen.GetItemSource_FromTreeShake(x, y), (int)pos.X, (int)pos.Y, npcType);
				Main.npc[npc].velocity = new Vector2(Main.rand.NextFloat(2, 5), 0).RotatedByRandom(MathHelper.TwoPi);
			}
		}
		else if (effect == StargrassTreeShakeEffect.Fruit)
		{
			Vector2 offset = this.GetRandomTreePosition(Main.tile[x, y]);
			int type = Main.rand.Next([ModContent.ItemType<MidnightApple>(), ModContent.ItemType<CrescentMelon>(), ModContent.ItemType<Pearlberry>()]);
			Item.NewItem(WorldGen.GetItemSource_FromTreeShake(x, y), new Vector2(x, y) * 16 + offset, type, 1);
		}

		return false;
	}
}

public enum StargrassTreeShakeEffect
{
	None = 0,
	Acorn,
	Wood,
	Critter,
	Fruit
}