using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Savanna.Tiles;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.Drawing;
using Terraria.GameContent.ObjectInteractions;

namespace SpiritReforged.Content.Desert.Tiles;

public class ZigguratTorch : ModTile, IAutoloadTileItem
{
	public const int FrameHeight = 18;
	public static readonly Asset<Texture2D> Flame = DrawHelpers.RequestLocal(typeof(ZigguratTorch), "ZigguratTorch_Flame", false);

	public static readonly SoundStyle Ignite = new("SpiritReforged/Assets/SFX/Tile/TorchIgnite", 2)
	{
		PitchVariance = 0.2f
	};

	public static readonly SoundStyle Extinguish = new("SpiritReforged/Assets/SFX/Tile/TorchExtinguish", 2)
	{
		PitchVariance = 0.2f
	};

	public void AddItemRecipes(ModItem item) => item.CreateRecipe().AddRecipeGroup(RecipeGroupID.Wood, 10).AddIngredient(ModContent.ItemType<SavannaTorchItem>(), 5).Register();

	public override void SetStaticDefaults()
	{
		Main.tileLighted[Type] = true;
		Main.tileFrameImportant[Type] = true;
		Main.tileNoAttach[Type] = true;

		TileID.Sets.HasOutlines[Type] = true;
		TileID.Sets.FramesOnKillWall[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style1x1);
		TileObjectData.newTile.AnchorBottom = AnchorData.Empty;
		TileObjectData.newTile.AnchorTop = AnchorData.Empty;
		TileObjectData.newTile.AnchorWall = true;
		TileObjectData.addTile(Type);

		AddMapEntry(new Color(174, 110, 48));
		RegisterItemDrop(this.AutoItemType());

		DustType = -1;
		AdjTiles = [TileID.Torches];

		this.AutoItem().ResearchUnlockCount = 5;
	}

	public override bool HasSmartInteract(int i, int j, SmartInteractScanSettings settings) => true;
	public override void MouseOver(int i, int j)
	{
		Player player = Main.LocalPlayer;
		player.noThrow = 2;
		player.cursorItemIconEnabled = true;
		player.cursorItemIconID = this.AutoItemType();
	}

	public override bool RightClick(int i, int j)
	{
		SoundEngine.PlaySound(SoundID.Mech, new Vector2(i * 16, j * 16));
		HitWire(i, j);

		return true;
	}

	public override void HitWire(int i, int j)
	{
		Tile tile = Main.tile[i, j];
		tile.TileFrameY = (short)((tile.TileFrameY == 0) ? FrameHeight : 0);

		var sound = (tile.TileFrameY == 0) ? Extinguish : Ignite;
		SoundEngine.PlaySound(sound, new Vector2(i, j).ToWorldCoordinates());

		if (Main.netMode != NetmodeID.SinglePlayer)
			NetMessage.SendTileSquare(-1, i, j);
	}

	public override void NearbyEffects(int i, int j, bool closer)
	{
		if (!closer && Main.tile[i, j].TileFrameY == 0)
		{
			var worldCoords = new Vector2(i, j).ToWorldCoordinates();
			if (Main.LocalPlayer.Distance(worldCoords) < 16 * 3)
			{
				HitWire(i, j);

				for (int x = 0; x < 5; x++)
					ParticleOrchestrator.SpawnParticlesDirect(ParticleOrchestraType.AshTreeShake, new() { PositionInWorld = worldCoords - new Vector2(0, 8) });
			}
		}
	}

	public override void EmitParticles(int i, int j, Tile tile, short tileFrameX, short tileFrameY, Color tileLight, bool visible)
	{
		if (Main.tile[i, j].TileFrameY != 0)
		{
			if (Main.rand.NextBool())
			{
				var dust = Dust.NewDustDirect(new Vector2(i, j).ToWorldCoordinates(0, -4), 16, 2, DustID.Torch, Scale: Main.rand.NextFloat(1, 3));
				dust.noGravity = true;
				dust.velocity.Y = -Main.rand.NextFloat(3);
				dust.fadeIn = 1;
			}

			if (Main.rand.NextBool())
			{
				var dust2 = Dust.NewDustDirect(new Vector2(i, j).ToWorldCoordinates(0, -8), 16, 2, DustID.Smoke, Alpha: 100, Scale: Main.rand.NextFloat(1, 2));
				dust2.noGravity = true;
				dust2.velocity = Vector2.UnitY.RotatedByRandom(0.5f) * -3;
				dust2.fadeIn = 1;
			}
		}
	}

	public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
	{
		if (Main.tile[i, j].TileFrameY != 0)
		{
			float pulse = Main.rand.Next(28, 42) * 0.005f + (270 - Main.mouseTextColor) / 700f;
			(r, g, b) = (0.9f + pulse, 0.4f + pulse, 0.1f + pulse);
		}
	}

	public override void PostDraw(int i, int j, SpriteBatch spriteBatch)
	{
		var tile = Main.tile[i, j];
		if (TileDrawing.IsVisible(tile) && Main.tile[i, j].TileFrameY != 0)
		{
			Texture2D texture = TextureAssets.Flames[0].Value;
			Rectangle source = new(0, 0, 22, 22);
			var position = new Vector2(i, j).ToWorldCoordinates(8 + Main.rand.NextFloat(-1f, 1f) * 1.5f, 4) - Main.screenPosition + TileExtensions.TileOffset;

			spriteBatch.Draw(texture, position, source, Color.White.Additive(50), 0, source.Size() / 2, 1.3f, SpriteEffects.None, 0);

			DrawFlame(spriteBatch, (int)((Main.timeForVisualEffects + j * 0.1f) / 6f) % 5, i, j, Color.White.Additive(50) * 0.5f);
			DrawFlame(spriteBatch, (int)((Main.timeForVisualEffects + j * 0.1f) / 6f) % 5, i, j, Color.White.Additive(50) * 0.5f);
		}

		static void DrawFlame(SpriteBatch spriteBatch, int frame, int i, int j, Color color)
		{
			Texture2D texture = Flame.Value;
			var source = texture.Frame(1, 5, 0, frame, 0, -2);
			var position = new Vector2(i, j).ToWorldCoordinates(8 + Main.rand.NextFloat(-1f, 1f) * 1.5f, 2) - Main.screenPosition + TileExtensions.TileOffset;

			spriteBatch.Draw(texture, position, source, color, 0, new Vector2(source.Width / 2, source.Height), 1, SpriteEffects.None, 0);
		}
	}
}