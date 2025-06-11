using SpiritReforged.Common.ItemCommon.FloatingItem;
using SpiritReforged.Common.SimpleEntity;
using SpiritReforged.Common.TileCommon.TileSway;
using Terraria.Audio;

namespace SpiritReforged.Content.Ocean.Items;

public class FishLure : FloatingItem
{
	public override float SpawnWeight => .008f;
	public override float Weight => base.Weight * 0.9f;
	public override float Bouyancy => base.Bouyancy * 1.08f;

	internal static bool WaterBelow()
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

	public override void SetStaticDefaults() => ItemID.Sets.ShimmerTransformToItem[Type] = ModContent.ItemType<InvisibleLure>();
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
		Item.value = Item.sellPrice(silver: 10);
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
			SimpleEntitySystem.NewEntity(typeof(FishLureEntity), Main.MouseWorld);
			return true;
		}

		return null;
	}
}

public class FishLureEntity : SimpleEntity
{
	protected virtual int ItemType => ModContent.ItemType<FishLure>();
	public bool SolidCollision { get; private set; }

	public override void Load()
	{
		Size = new Vector2(16);
		saveMe = true;
	}

	public override void Update()
	{
		SolidCollision = Collision.SolidCollision(position, width, height);

		if (Collision.WetCollision(position, width, height))
			velocity.Y -= .05f;
		else if (!Collision.WetCollision(position, width, height + 2) && !SolidCollision)
			velocity.Y += .1f;
		else
			velocity.Y *= .75f;

		position += velocity;

		//Proximity check
		var player = Main.LocalPlayer;
		var distance = new Vector2(Math.Abs(player.Center.X - Center.X), Math.Abs(player.Center.Y - Center.Y));

		if (distance.X < Main.buffScanAreaWidth * 8 && distance.Y < Main.buffScanAreaHeight * 8)
			player.GetModPlayer<OceanPlayer>().nearLure = true;

		if (Hitbox.Contains(Main.MouseWorld.ToPoint()) && player.IsTargetTileInItemRange(new Item()))
		{
			player.cursorItemIconEnabled = true;
			player.cursorItemIconID = ItemType;

			if (Main.mouseRight && Main.mouseRightRelease)
			{
				Kill();

				if (Main.netMode == NetmodeID.MultiplayerClient)
					new KillSimpleEntityData((short)whoAmI).Send();
			}
		}
	}

	public override void OnKill()
	{
		if (Main.netMode != NetmodeID.MultiplayerClient)
			Item.NewItem(GetSource_Death(), Hitbox, ItemType);

		SoundEngine.PlaySound(SoundID.Dig, Center);
	}

	public override void Draw(SpriteBatch spriteBatch)
	{
		var drawPosition = Center - Main.screenPosition + new Vector2(0, SolidCollision ? 0 : Sin(30f));
		var color = Lighting.GetColor((int)(Center.X / 16), (int)(Center.Y / 16));

		spriteBatch.Draw(Texture.Value, drawPosition, null, color, GetRotation() * .1f, Texture.Size() / 2, 1, SpriteEffects.None, 0f);
	}

	public float GetRotation()
	{
		float rotation = 0;

		if (!SolidCollision)
		{
			rotation = Main.instance.TilesRenderer.GetWindCycle((int)(position.X / 16), (int)(position.Y / 16), TileSwaySystem.Instance.SunflowerWindCounter);
			rotation += TileSwayHelper.GetHighestWindGridPushComplex((int)(position.X / 16), (int)(position.Y / 16), 2, 3, 120, 1f, 5, true);
		}

		return rotation;
	}

	public float Sin(float numerator) => (float)Math.Sin((Main.timeForVisualEffects + Center.X) / numerator);
}