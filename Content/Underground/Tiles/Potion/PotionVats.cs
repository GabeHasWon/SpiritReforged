using RubbleAutoloader;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.TileCommon.PresetTiles;
using SpiritReforged.Content.Underground.Pottery;
using Terraria.Audio;
using Terraria.DataStructures;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using System.Linq;
using static SpiritReforged.Common.TileCommon.StyleDatabase;
using static SpiritReforged.Common.WorldGeneration.WorldMethods;

namespace SpiritReforged.Content.Underground.Tiles.Potion;

public class PotionVats : PotTile, ICutAttempt
{
	private static Asset<Texture2D> FluidTexture;

	public override Dictionary<string, int[]> TileStyles => new()
	{
		{ "Antique", [0, 1, 2] },
		{ "Cloning", [3, 4, 5] },
		{ "Alchemy", [6, 7, 8] }
	};

	public VatSlot Entity(int i, int j, bool skipTypeCheck = false)
	{
		if (!skipTypeCheck && Main.tile[i, j].TileType != Type)
			return null;

		TileExtensions.GetTopLeft(ref i, ref j);
		int id = ModContent.GetInstance<VatSlot>().Find(i, j);

		return (id == -1) ? null : (VatSlot)TileEntity.ByID[id];
	}

	public override void AddRecord(int type, StyleGroup group)
	{
		var desc = Language.GetText(TileRecord.DescKey + ".Potion");
		RecordHandler.Records.Add(new TileRecord(group.name, type, group.styles).AddDescription(desc).AddRating(4));
	}

	public override void AddItemRecipes(ModItem modItem, StyleGroup group)
	{
		LocalizedText dicovered = AutoloadedPotItem.Discovered;
		var function = (modItem as AutoloadedPotItem).RecordedPot;

		switch (group.name)
		{
			case "PotionVatsAntique":
				modItem.CreateRecipe().AddRecipeGroup("ClayAndMud", 3).AddIngredient(ItemID.Glass).AddRecipeGroup("GoldBars", 2)
					.AddTile(ModContent.TileType<PotteryWheel>()).AddCondition(dicovered, function).Register();
				break;

			case "PotionVatsCloning":
				modItem.CreateRecipe().AddRecipeGroup("ClayAndMud", 3).AddIngredient(ItemID.Glass).AddRecipeGroup(RecipeGroupID.IronBar, 2)
					.AddTile(ModContent.TileType<PotteryWheel>()).AddCondition(dicovered, function).Register();
				break;

			case "PotionVatsAlchemy":
				modItem.CreateRecipe().AddRecipeGroup("ClayAndMud", 3).AddIngredient(ItemID.Glass).AddRecipeGroup("CopperBars", 2)
					.AddTile(ModContent.TileType<PotteryWheel>()).AddCondition(dicovered, function).Register();
				break;
		}
	}

	public override void AddObjectData()
	{
		Main.tileCut[Type] = !Autoloader.IsRubble(Type);
		Main.tileSpelunker[Type] = true;
		Main.tileOreFinderPriority[Type] = 575;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style3x3);
		TileObjectData.newTile.Height = 5;
		TileObjectData.newTile.CoordinateHeights = [16, 16, 16, 16, 16];
		TileObjectData.newTile.Origin = new(1, 4);
		TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop, TileObjectData.newTile.Width, 0);
		TileObjectData.newTile.HookPostPlaceMyPlayer = new PlacementHook(ModContent.GetInstance<VatSlot>().Hook_AfterPlacement, -1, 0, false);
		TileObjectData.newTile.StyleWrapLimit = 3;
		TileObjectData.newTile.StyleHorizontal = true;
		TileObjectData.newTile.UsesCustomCanPlace = true;
		TileObjectData.newTile.DrawYOffset = 2;
		TileObjectData.addTile(Type);

		DustType = Autoloader.IsRubble(Type) ? -1 : DustID.Glass;
		FluidTexture = ModContent.Request<Texture2D>(Texture + "_Fluid");
	}

	public override void AddMapData() => AddMapEntry(new Color(146, 76, 77), Language.GetText("Mods.SpiritReforged.Tiles.PotionVats.MapEntry"));

	public override void KillTile(int i, int j, ref bool fail, ref bool effectOnly, ref bool noItem)
	{
		if (!effectOnly && Autoloader.IsRubble(Type))
		{
			if (Entity(i, j) is VatSlot slot && !slot.item.IsAir)
			{
				fail = true;

				if (Main.netMode != NetmodeID.MultiplayerClient)
				{
					TileExtensions.GetTopLeft(ref i, ref j);

					var pos = new Vector2(i, j).ToWorldCoordinates();

					Item.NewItem(new EntitySource_TileBreak(i, j), pos, slot.item);
					slot.RemoveItem();
				}
			}

			return;
		}

		if (effectOnly || !fail || Generating)
			return;

		fail = AdjustFrame(i, j);
	}

	public bool OnCutAttempt(int i, int j)
	{
		if (ClubQuickKill(i, j))
			return true;

		bool fail = AdjustFrame(i, j);

		var cache = Main.tile[i, j];
		WorldGen.KillTile_MakeTileDust(i, j, cache);
		WorldGen.KillTile_PlaySounds(i, j, true, cache);

		return !fail;
	}

	public override bool KillSound(int i, int j, bool fail)
	{
		if (Autoloader.IsRubble(Type))
			return true;

		var pos = new Vector2(i, j).ToWorldCoordinates(24, 24);
		if (fail)
		{
			int phase = Main.tile[i, j].TileFrameX / 54;
			SoundEngine.PlaySound(SoundID.Shatter with { Pitch = phase / 4f }, pos);
		}
		else
		{
			SoundEngine.PlaySound(SoundID.Shatter with { Pitch = .9f }, pos);
			SoundEngine.PlaySound(new SoundStyle("SpiritReforged/Assets/SFX/Tile/PotBreak") with { Volume = .10f, Pitch = 1f, }, pos);
		}

		return false;
	}

	private static bool AdjustFrame(int i, int j)
	{
		const int fullWidth = 54;
		TileExtensions.GetTopLeft(ref i, ref j);

		if (Main.tile[i, j].TileFrameX > fullWidth)
			return false; //Frame has already been adjusted to capacity

		for (int x = i; x < i + 3; x++)
		{
			for (int y = j; y < j + 5; y++)
			{
				var t = Main.tile[x, y];
				t.TileFrameX += fullWidth;
			}
		}

		return true;
	}

	public override void KillMultiTile(int i, int j, int frameX, int frameY)
	{
		if (Autoloader.IsRubble(Type) || WorldGen.generatingWorld)
			return;

		WorldGen.PlaceTile(i + 1, j + 4, ModContent.TileType<PotionVatsBroken>(), true, style: frameY / 90);
		var center = new Vector2(i, j).ToWorldCoordinates(24, 24);

		PlayerKnockback(center, 120);

		if (Main.netMode != NetmodeID.MultiplayerClient && Entity(i, j, true) is VatSlot slot && !slot.item.IsAir)
		{
			int potion = slot.item.type;
			Projectile.NewProjectile(new EntitySource_TileBreak(i, j), center, Vector2.Zero, ModContent.ProjectileType<BuffAura>(), 0, 0, -1, potion);
		}

		base.KillMultiTile(i, j, frameX, frameY);
	}

	private static void PlayerKnockback(Vector2 origin, int size)
	{
		foreach (var player in Main.ActivePlayers)
		{
			float distance = player.DistanceSQ(origin) / (size * size);

			if (distance < 1)
				player.velocity = player.DirectionFrom(origin) * (1f - distance) * 8f;
		}
	}

	public override void DeathEffects(int i, int j, int frameX, int frameY)
	{
		int style = frameY / 90;

		for (int g = 1; g < 4; g++)
		{
			int goreType = Mod.Find<ModGore>("Vat" + (g + style * 3)).Type;
			Gore.NewGore(new EntitySource_TileBreak(i, j), Main.rand.NextVector2FromRectangle(new Rectangle(i * 16, j * 16, 32, 32)), Vector2.Zero, goreType);
		}

		if (Entity(i, j, true) is VatSlot slot)
		{
			var color = slot.GetColor();

			for (int p = 0; p < 12; p++)
			{
				float magnitude = Main.rand.NextFloat();
				var velocity = Main.rand.NextVector2Unit() * magnitude * 3f;

				ParticleHandler.SpawnParticle(new VaporParticle(new Vector2(i, j).ToWorldCoordinates(24, 40), velocity, color.Additive(110), 1f - magnitude + 1.5f, Main.rand.Next(300, 500))
				{
					Rotation = Main.rand.NextFloat()
				});
			}

			for (int d = 0; d < 50; d++)
			{
				var dust = Dust.NewDustDirect(new Vector2(i, j) * 16, 48, 80, DustID.FoodPiece, 0, 0, 0, color.Additive(120), Main.rand.NextFloat(.75f, 1.5f));
				dust.fadeIn = 1f;
			}
		}
	}

	public override void MouseOver(int i, int j)
	{
		if (Autoloader.IsRubble(Type))
		{
			Player player = Main.LocalPlayer;
			player.noThrow = 2;
			player.cursorItemIconEnabled = true;
			player.cursorItemIconID = (Entity(i, j) is not VatSlot entity || entity.item.IsAir) ? ItemID.None : entity.item.type;
		}
	}

	public override bool RightClick(int i, int j)
	{
		if (Autoloader.IsRubble(Type) && Entity(i, j) is VatSlot entity)
		{
			entity.OnInteract(Main.LocalPlayer);
			return true;
		}

		return false;
	}

	public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
	{
		if (Entity(i, j) is not VatSlot slot || slot.item.IsAir)
			return true;

		var texture = FluidTexture.Value;
		var position = new Vector2(i, j) * 16 - Main.screenPosition + TileExtensions.TileOffset + new Vector2(0, 2);

		var t = Main.tile[i, j];
		var frame = new Rectangle(t.TileFrameX, t.TileFrameY, 16, 16);

		float alpha = 200 + (float)Math.Sin((Main.timeForVisualEffects + i) / 30f) * 40f;
		var color = (Lighting.GetColor(i, j) * 2).MultiplyRGBA(slot.GetColor().Additive((byte)alpha));

		for (int x = 0; x < 2; x++)
			spriteBatch.Draw(texture, position, frame, color);

		return true;
	}
}

public class VatSlot : SingleSlotEntity
{
	public Color GetColor() => GetColorFromPotion(item.type);

	public static Color GetColorFromPotion(int type)
	{
		if (PotionColorDatabase.NaturalBrewColors.TryGetValue(type, out Color value))
			return value;

		if (PotionColorDatabase.DecorativeBrewColors.TryGetValue(type, out Color decor))
			return decor;

		return Color.Transparent;
	}

	/// <summary> Returns a random potion type from those registered for this tile entity. </summary>
	public static int GetRandomNaturalPotion()
	{
		var list = PotionColorDatabase.NaturalBrewColors.Keys.ToList();
		return list[Main.rand.Next(list.Count)];
	}

	public override bool CanAddItem(Item item) => PotionColorDatabase.NaturalBrewColors.ContainsKey(item.type) 
		|| PotionColorDatabase.DecorativeBrewColors.ContainsKey(item.type);

	public override bool IsTileValidForEntity(int x, int y)
	{
		var t = Framing.GetTileSafely(x, y);
		return t.HasTile && TileLoader.GetTile(t.TileType) is PotionVats && TileObjectData.IsTopLeft(x, y);
	}
}