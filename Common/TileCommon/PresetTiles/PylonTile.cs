using Terraria.DataStructures;
using Terraria.Map;
using Terraria.ModLoader.Default;

namespace SpiritReforged.Common.TileCommon.PresetTiles;

public abstract class PylonTile : ModPylon, IAutoloadTileItem
{
	protected ModItem ModItem => Mod.Find<ModItem>(Name + "Item");

	protected const int frameCount = 8;
	public Asset<Texture2D> crystalTexture, crystalHighlightTexture, mapIcon;

	public void SetItemDefaults(ModItem item) => item.Item.SetShopValues(ItemRarityColor.Blue1, Item.buyPrice(gold: 10));

	public override void Load()
	{
		crystalTexture = ModContent.Request<Texture2D>(Texture + "_Crystal");
		crystalHighlightTexture = ModContent.Request<Texture2D>(Texture + "_CrystalHighlight");
		mapIcon = ModContent.Request<Texture2D>(Texture + "_MapIcon");
	}

	/// <summary><inheritdoc/><para/>
	/// Only override this if you need to change basic pylon behaviour. Use <see cref="SetStaticDefaults(LocalizedText)"/> otherwise.</summary>
	public override void SetStaticDefaults()
	{
		Main.tileLighted[Type] = true;
		Main.tileFrameImportant[Type] = true;

		TileID.Sets.InteractibleByNPCs[Type] = true;
		TileID.Sets.PreventsSandfall[Type] = true;
		TileID.Sets.AvoidedByMeteorLanding[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style3x4);
		TileObjectData.newTile.LavaDeath = false;
		TileObjectData.newTile.DrawYOffset = 2;
		TileObjectData.newTile.StyleHorizontal = true;
		var tileEntity = ModContent.GetInstance<PylonTileEntity>();
		TileObjectData.newTile.HookCheckIfCanPlace = new PlacementHook(tileEntity.PlacementPreviewHook_CheckIfCanPlace, 1, 0, true);
		TileObjectData.newTile.HookPostPlaceMyPlayer = new PlacementHook(tileEntity.Hook_AfterPlacement, -1, 0, false);
		TileObjectData.addTile(Type);

		AddToArray(ref TileID.Sets.CountsAsPylon);
		DustType = -1;

		SetStaticDefaults(ModItem.DisplayName);
	}

	/// <summary><inheritdoc cref="ModBlockType.SetStaticDefaults"/><para/>
	/// Includes helper <paramref name="mapEntry"/>.</summary>
	/// <param name="mapEntry"> The default map localization for pylons. </param>
	public virtual void SetStaticDefaults(LocalizedText mapEntry) { }

	public override void MouseOver(int i, int j)
	{
		Main.LocalPlayer.cursorItemIconEnabled = true;
		Main.LocalPlayer.cursorItemIconID = ModItem.Type;
	}

	public override NPCShop.Entry GetNPCShopEntry() => new(ModItem.Type, Condition.HappyEnoughToSellPylons, Condition.NotInEvilBiome);
	public override void KillMultiTile(int i, int j, int frameX, int frameY) => ModContent.GetInstance<PylonTileEntity>().Kill(i, j);
	public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b) => (r, g, b) = (.5f, .5f, .5f);

	public override void SpecialDraw(int i, int j, SpriteBatch spriteBatch)
	{
		var color = Color.White;
		DefaultDrawPylonCrystal(spriteBatch, i, j, crystalTexture, crystalHighlightTexture, new Vector2(0f, -12f), color * .1f, color, 4, frameCount);
	}

	public override void DrawMapIcon(ref MapOverlayDrawContext context, ref string mouseOverText, TeleportPylonInfo pylonInfo, bool isNearPylon, Color drawColor, float deselectedScale, float selectedScale)
	{
		bool mouseOver = DefaultDrawMapIcon(ref context, mapIcon, pylonInfo.PositionInTiles.ToVector2() + new Vector2(1.5f, 2f), drawColor, deselectedScale, selectedScale);
		DefaultMapClickHandle(mouseOver, pylonInfo, ModItem.DisplayName.Key, ref mouseOverText);
	}
}

public sealed class PylonTileEntity : TEModdedPylon;