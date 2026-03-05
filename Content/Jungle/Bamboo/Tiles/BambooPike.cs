using SpiritReforged.Common.TileCommon;
using SpiritReforged.Content.Jungle.Bamboo.Buffs;

namespace SpiritReforged.Content.Jungle.Bamboo.Tiles;

public class BambooPike : ModTile
{
	public override void SetStaticDefaults()
	{
		Main.tileSolid[Type] = false;
		Main.tileBlockLight[Type] = false;
		Main.tileNoFail[Type] = true;
		Main.tileFrameImportant[Type] = true;

		RegisterItemDrop(ModContent.ItemType<BambooPikeItem>());
		AddMapEntry(new Color(80, 140, 35));
		DustType = DustID.JunglePlants;
	}

	public override bool TileFrame(int i, int j, ref bool resetFrame, ref bool noBreak)
	{
		var t = Framing.GetTileSafely(i, j);
		var tUp = Framing.GetTileSafely(i, j - 1);
		var tDown = Framing.GetTileSafely(i, j + 1);

		bool hasTileAbove = tUp.TileType == Type;
		bool hasTileBelow = tDown.TileType == Type;

		if (!tDown.HasTile) //Has any tile below
		{
			WorldGen.KillTile(i, j, false, false, false);
			return false;
		}

		if (!hasTileAbove && !hasTileBelow && resetFrame)
			t.TileFrameX = (short)(18 * Main.rand.Next(3));

		if (hasTileAbove) //Pick the appropriate frame depending on tile stack
			t.TileFrameY = (short)(18 * (hasTileBelow ? 1 : 2));
		else
			t.TileFrameY = 0;

		if (hasTileBelow) //Inherit the same horizontal tile frame as the rest of the stack
			t.TileFrameX = tDown.TileFrameX;

		return false;
	}

	public static void Strike(Entity entity)
	{
		float damage = MathHelper.Clamp(entity.velocity.Y, 1, 10) * 10f;
		Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCDeath12, entity.Center);

		for (int d = 0; d < 20; d++)
			Dust.NewDustPerfect(entity.Bottom + Main.rand.NextVector2Unit() * Main.rand.NextFloat(10f), DustID.Blood);

		if (entity is NPC npc)
		{
			npc.SimpleStrikeNPC((int)damage, 1, false, 0);
			npc.AddBuff(ModContent.BuffType<Impaled>(), 500);
		}
		else if (entity is Player player)
		{
			player.Hurt(BambooPikePlayer.GetDeathReason(player), (int)damage, 0);
			player.AddBuff(ModContent.BuffType<Impaled>(), 500);
			player.velocity = new Vector2(0, 1);
		}
	}

	public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
	{
		if (!TileExtensions.GetVisualInfo(i, j, out var color, out var texture))
			return false;

		var t = Main.tile[i, j];
		var source = new Rectangle(t.TileFrameX, t.TileFrameY, 16, 16);
		var naturalOffset = new Vector2(t.TileFrameX / 18 * 2, 2);
		var drawPos = new Vector2(i, j) * 16 - Main.screenPosition + naturalOffset + TileExtensions.TileOffset;

		spriteBatch.Draw(texture, drawPos, source, color, 0f, new Vector2(0, 0), 1f, SpriteEffects.None, 0f);
		return false;
	}
}

public class BambooPikeItem : ModItem
{
	public override void SetDefaults()
	{
		Item.DefaultToPlaceableTile(ModContent.TileType<BambooPike>());
		Item.width = Item.height = 36;
		Item.value = Item.sellPrice(copper: 5);
	}

	public override bool CanUseItem(Player player) //Allow pikes to be placed when used on any tile in the stack
	{
		var tilePos = new Point(Player.tileTargetX, Player.tileTargetY);

		if (Framing.GetTileSafely(tilePos).TileType == Item.createTile)
		{
			while (Framing.GetTileSafely(tilePos).TileType == Item.createTile)
				tilePos.Y--;

			var t = Framing.GetTileSafely(tilePos);

			if (WorldGen.SolidOrSlopedTile(t))
				return true;

			WorldGen.PlaceTile(tilePos.X, tilePos.Y, Item.createTile);

			if (t.TileType == Item.createTile) //Success!
			{
				if (Main.netMode != NetmodeID.SinglePlayer)
					NetMessage.SendTileSquare(-1, tilePos.X, tilePos.Y, 1);

				if (Item.stack == 1)
					Item.TurnToAir();
				else
					Item.stack--;
			}
		}

		return true;
	}

	public override void AddRecipes() => CreateRecipe().AddIngredient(ItemID.BambooBlock, 2).AddTile(TileID.WorkBenches).Register();
}