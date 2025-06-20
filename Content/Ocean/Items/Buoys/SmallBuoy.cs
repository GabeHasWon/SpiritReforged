using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.ModCompat.Classic;
using SpiritReforged.Common.SimpleEntity;
using SpiritReforged.Common.TileCommon.TileSway;
using Terraria.Audio;

namespace SpiritReforged.Content.Ocean.Items.Buoys;

[FromClassic("BuoyItem")]
public class SmallBuoy : ModItem
{
	private static bool WaterBelow()
	{
		for (int i = 0; i < 2; i++)
		{
			var tile = Framing.GetTileSafely(Player.tileTargetX, Player.tileTargetY + i);
			if (i == 0 && tile.LiquidAmount >= 255)
				return false; //Already completely submerged

			if (tile.LiquidAmount > 20 && tile.LiquidType == LiquidID.Water)
				return true;
		}

		return false;
	}

	public override void SetDefaults()
	{
		Item.width = Item.height = 14;
		Item.useAnimation = 15;
		Item.useTime = 10;
		Item.maxStack = Item.CommonMaxStack;
		Item.useStyle = ItemUseStyleID.Swing;
		Item.useTurn = true;
		Item.autoReuse = true;
		Item.consumable = true;
		Item.value = Item.sellPrice(silver: 1);
	}

	public override void HoldItem(Player player)
	{
		if (CanUseItem(player))
		{
			player.cursorItemIconEnabled = true;
			player.cursorItemIconID = Type;
		}
	}

	public override bool CanUseItem(Player player) => WaterBelow() && player.IsTargetTileInItemRange(Item);

	public override bool? UseItem(Player player)
	{
		if (!Main.dedServ && player.whoAmI == Main.myPlayer && player.ItemAnimationJustStarted)
		{
			SimpleEntitySystem.NewEntity<SmallBuoyEntity>(Main.MouseWorld);
			return true;
		}

		return null;
	}

	public override void AddRecipes() => CreateRecipe().AddRecipeGroup("CopperBars").AddIngredient(ItemID.Glass, 1).AddTile(TileID.Anvils).Register();
}

public class SmallBuoyEntity : SimpleEntity
{
	private static Asset<Texture2D> GlowTexture;

	public virtual Texture2D Glowmask => GlowTexture.Value;
	protected virtual int ItemType => ModContent.ItemType<SmallBuoy>();

	private bool solidCollision;

	public override void Load()
	{
		if (!Main.dedServ)
			GlowTexture = ModContent.Request<Texture2D>(TexturePath + "_Glow");

		saveMe = true;
		width = 18;
		height = 42;
	}

	public override void Update()
	{
		solidCollision = Collision.SolidCollision(position, width, height - 18);

		if (Collision.WetCollision(position, width, height + 8))
			velocity.Y -= .05f;
		else if (!Collision.WetCollision(position, width, height + 12) && !solidCollision)
			velocity.Y += .1f;
		else
			velocity.Y *= .75f;

		position += velocity;

		Lighting.AddLight(Top, .3f, .1f, .1f);
		var player = Main.LocalPlayer;

		if (Hitbox.Contains(Main.MouseWorld.ToPoint()) && player.IsTargetTileInItemRange(new Item()))
		{
			player.cursorItemIconEnabled = true;
			player.cursorItemIconID = ItemType;

			if (Main.mouseRight && Main.mouseRightRelease)
			{
				Kill();
				ItemMethods.NewItemSynced(GetSource_Death(), ItemType, Hitbox.Center(), true);

				if (Main.netMode == NetmodeID.MultiplayerClient)
					new KillSimpleEntityData((short)whoAmI).Send();
			}
		}
	}

	public override void OnKill() => SoundEngine.PlaySound(SoundID.Dig, Center);

	public override void Draw(SpriteBatch spriteBatch)
	{
		float Sin(float numerator) => (float)Math.Sin((Main.timeForVisualEffects + Center.X) / numerator);

		var texture = Texture.Value;
		var origin = new Vector2(texture.Width / 2, texture.Height);
		var drawPosition = position - Main.screenPosition + new Vector2(0, solidCollision ? 0 : Sin(30f)) + origin;
		var color = Lighting.GetColor((int)(Center.X / 16), (int)(Center.Y / 16));

		float rotation = 0;

		if (!solidCollision)
		{
			rotation = Main.instance.TilesRenderer.GetWindCycle((int)(position.X / 16), (int)(position.Y / 16), TileSwaySystem.Instance.SunflowerWindCounter);
			rotation += TileSwayHelper.GetHighestWindGridPushComplex((int)(position.X / 16), (int)(position.Y / 16), 2, 3, 120, 1f, 5, true);
		}

		spriteBatch.Draw(texture, drawPosition, null, color, rotation * .1f, origin, 1, SpriteEffects.None, 0f);

		var glowColor = Color.White * (1f - Sin(10f) * .3f);
		spriteBatch.Draw(Glowmask, drawPosition, null, glowColor, rotation * .1f, origin, 1, SpriteEffects.None, 0f);
	}
}