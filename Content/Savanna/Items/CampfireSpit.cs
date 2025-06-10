using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.TileCommon.PresetTiles;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Savanna.NPCs.Sparrow;
using SpiritReforged.Content.Vanilla.Food;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.Drawing;

namespace SpiritReforged.Content.Savanna.Items;

public class CampfireSpit : ModItem
{
	/// <summary> Checks whether the target tile position is a campfire and does not already have a fire spit. </summary>
	private bool CanPlace(Player player)
	{
		int i = Player.tileTargetX;
		int j = Player.tileTargetY;

		var target = Framing.GetTileSafely(i, j);

		if (target.HasTile && TileID.Sets.Campfire[target.TileType] && player.IsTargetTileInItemRange(Item))
		{
			TileExtensions.GetTopLeft(ref i, ref j);
			return ModContent.GetInstance<CampfireSlot>().Find(i, j) == -1; //Invalid if this entity already exists
		}

		return false;
	}

	public override void SetStaticDefaults() => Item.ResearchUnlockCount = 25;
	public override void SetDefaults()
	{
		Item.width = Item.height = 14;
		Item.useAnimation = 15;
		Item.useTime = 10;
		Item.maxStack = Item.CommonMaxStack;
		Item.useStyle = ItemUseStyleID.Swing;
		Item.useTurn = true;
		Item.autoReuse = true;
		Item.value = Item.sellPrice(copper: 4);
		Item.UseSound = SoundID.Dig;
	}

	public override void HoldItem(Player player)
	{
		if (CanPlace(player))
		{
			player.cursorItemIconEnabled = true;
			player.cursorItemIconID = Type;
		}
	}

	public override bool CanUseItem(Player player) => CanPlace(player);
	public override bool? UseItem(Player player)
	{
		if (Main.myPlayer == player.whoAmI && player.ItemAnimationJustStarted)
		{
			if (CanPlace(player))
			{
				int i = Player.tileTargetX;
				int j = Player.tileTargetY;

				TileExtensions.GetTopLeft(ref i, ref j);

				int type = ModContent.TileEntityType<CampfireSlot>();
				TileEntity.PlaceEntityNet(i, j, type);

				if (Main.netMode == NetmodeID.MultiplayerClient)
					NetMessage.SendData(MessageID.TileEntityPlacement, number: i, number2: j, number3: type);

				if (--Item.stack <= 0)
					Item.TurnToAir(); //Consume

				return true;
			}
		}

		return null;
	}

	public override void AddRecipes() => CreateRecipe().AddIngredient(ItemID.Wood, 8).AddRecipeGroup(RecipeGroupID.IronBar).AddTile(TileID.WorkBenches).Register();
}

public class CampfireSlot : SingleSlotEntity
{
	private const short cookCounterMax = 60 * 10;
	private short cookCounter;

	/// <summary> Places a <see cref="TileID.Campfire"/> in the world along with this entity, cooking meat. </summary>
	/// <returns> Whether the campfire was successfully placed. </returns>
	public static bool GenerateCampfire(int i, int j)
	{
		int campfire = TileID.Campfire;
		WorldGen.PlaceObject(i, j, campfire, true);

		if (Main.tile[i, j].TileType != campfire)
			return false;

		TileExtensions.GetTopLeft(ref i, ref j);
		PlaceEntityNet(i, j, ModContent.TileEntityType<CampfireSlot>());

		if (ByPosition[new Point16(i, j)] is CampfireSlot slot)
			slot.item = new Item(ModContent.ItemType<CookedMeat>());

		return true;
	}

	public override bool IsTileValidForEntity(int x, int y)
	{
		var t = Main.tile[x, y];
		return TileID.Sets.Campfire[t.TileType] && TileObjectData.IsTopLeft(x, y);
	}

	public override bool CanAddItem(Item item) => RoastGlobalTile.CookedByRaw.ContainsKey(item.type);

	public override void Update()
	{
		base.Update();

		if (RoastGlobalTile.CookedByRaw.TryGetValue(item.type, out int value) && CampfireLit() && ++cookCounter >= cookCounterMax)
		{
			item = new Item(value);
			cookCounter = 0;

			for (int i = 0; i < 3; i++)
			{
				var pos = Position.ToWorldCoordinates() + new Vector2(16, -4) + Main.rand.NextVector2Unit() * Main.rand.NextFloat(4f);
				var settings = new ParticleOrchestraSettings() { PositionInWorld = pos };

				if (Main.netMode == NetmodeID.Server)
					ParticleOrchestrator.BroadcastParticleSpawn(ParticleOrchestraType.WallOfFleshGoatMountFlames, settings);
				else
					ParticleOrchestrator.SpawnParticlesDirect(ParticleOrchestraType.WallOfFleshGoatMountFlames, settings);
			}

			if (Main.netMode == NetmodeID.Server)
				new SingleSlotData((short)ID, item).Send();
		}

		bool CampfireLit() => Framing.GetTileSafely(Position).TileFrameY < 18 * 2;
	}
}

public class RoastGlobalTile : GlobalTile
{
	private static readonly Asset<Texture2D> TileTexture = ModContent.Request<Texture2D>(DrawHelpers.RequestLocal(typeof(RoastGlobalTile), "CampfireSpit_Tile"));

	internal static Dictionary<int, int> CookedByRaw;

	private static TileEntity Entity(int i, int j)
	{
		if (!TileID.Sets.Campfire[Main.tile[i, j].TileType])
			return null;

		TileExtensions.GetTopLeft(ref i, ref j);
		int id = ModContent.GetInstance<CampfireSlot>().Find(i, j);

		return (id == -1) ? null : TileEntity.ByID[id];
	}

	private static bool IsTopHalf(int i, int j) => Main.tile[i, j].TileFrameY % (18 * 2) == 0;

	public override void SetStaticDefaults()
	{
		CookedByRaw = new()
		{
			{ ModContent.ItemType<RawMeat>(), ModContent.ItemType<CookedMeat>() },
			{ ItemID.Marshmallow, ItemID.CookedMarshmallow }, 
			{ ItemID.Squirrel, ItemID.GrilledSquirrel },
			{ ItemID.SquirrelRed, ItemID.GrilledSquirrel },
			{ AutoContent.ItemType<Sparrow>(), ItemID.RoastedBird },
			{ ModContent.ItemType<RawFish>(), ItemID.CookedFish },
			{ ItemID.Bass, ItemID.CookedFish }, 
			{ ItemID.Trout, ItemID.CookedFish }, 
			{ ItemID.AtlanticCod, ItemID.CookedFish }
		};

		if (RecipeGroup.recipeGroups.TryGetValue(RecipeGroupID.Birds, out RecipeGroup birdsGroup))
		{
			foreach (int birdIDs in birdsGroup.ValidItems)
				CookedByRaw[birdIDs] = ItemID.RoastedBird;
		}

		if (RecipeGroup.recipeGroups.TryGetValue(RecipeGroupID.Ducks, out RecipeGroup ducksGroup))
		{
			foreach (int duckIDs in ducksGroup.ValidItems)
				CookedByRaw[duckIDs] = ItemID.RoastedDuck;
		}
	}

	public override void MouseOver(int i, int j, int type)
	{
		if (Entity(i, j) is CampfireSlot slot && IsTopHalf(i, j))
		{
			int iconType = slot.item.IsAir ? ModContent.ItemType<CampfireSpit>() : slot.item.type;
			Main.LocalPlayer.cursorItemIconEnabled = true;
			Main.LocalPlayer.cursorItemIconID = iconType;
			Main.LocalPlayer.noThrow = 2;
		}
	}

	public override void RightClick(int i, int j, int type)
	{
		if (Entity(i, j) is CampfireSlot slot && IsTopHalf(i, j) && slot.OnInteract(Main.LocalPlayer))
		{
			Wiring.ToggleCampFire(i, j, Main.tile[i, j], null, true);
			SoundEngine.PlaySound(SoundID.Item1 with { Pitch = .25f }, new Vector2(i, j).ToWorldCoordinates());
		}
	}

	public override void KillTile(int i, int j, int type, ref bool fail, ref bool effectOnly, ref bool noItem)
	{
		if (effectOnly)
			return;

		if (Entity(i, j) is CampfireSlot slot)
		{
			fail = true;

			if (Main.netMode != NetmodeID.MultiplayerClient)
			{
				TileExtensions.GetTopLeft(ref i, ref j);

				var pos = new Vector2(i, j).ToWorldCoordinates() + new Vector2(16);
				Item.NewItem(new EntitySource_TileBreak(i, j), pos, ModContent.ItemType<CampfireSpit>());

				if (!slot.item.IsAir)
					Item.NewItem(new EntitySource_TileBreak(i, j), pos, slot.item);

				slot.Kill(i, j);
			}
		}
	}

	public override bool PreDraw(int i, int j, int type, SpriteBatch spriteBatch)
	{
		if (TileID.Sets.Campfire[type] && TileObjectData.IsTopLeft(i, j) && Entity(i, j) is CampfireSlot slot)
		{
			var position = new Vector2(i, j) * 16 - Main.screenPosition - new Vector2(0, 16) + TileExtensions.TileOffset;
			spriteBatch.Draw(TileTexture.Value, position, Lighting.GetColor(i + 1, j + 1));

			if (!slot.item.IsAir)
			{
				var itemTexture = TextureAssets.Item[slot.item.type];
				var source = Item.GetDrawHitbox(slot.item.type, Main.LocalPlayer);

				spriteBatch.Draw(itemTexture.Value, position + new Vector2(24, 16), source, Lighting.GetColor(i + 1, j + 1), 0, source.Size() / 2, 1, default, 0);
			}
		}

		return true;
	}
}