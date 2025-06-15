namespace SpiritReforged.Common.ItemCommon.Abstract;

public abstract class TorchItem : ModItem
{
	public abstract int TileType { get; }
	public virtual Vector3 Light => new(1f);

	public override void SetStaticDefaults()
	{
		Item.ResearchUnlockCount = 100;

		ItemID.Sets.ShimmerTransformToItem[Type] = ItemID.ShimmerTorch;
		ItemID.Sets.SingleUseInGamepad[Type] = true;
		ItemID.Sets.Torches[Type] = true;
	}

	public override void SetDefaults()
	{
		Item.DefaultToTorch(TileType, 0);
		Item.value = 50;
	}

	public override void HoldItem(Player player)
	{
		if (player.wet)
			return;

		if (Main.rand.NextBool(player.itemAnimation > 0 ? 7 : 30))
		{
			var d = Dust.NewDustDirect(new Vector2(player.itemLocation.X + (player.direction == -1 ? -16f : 6f), player.itemLocation.Y - 14f * player.gravDir), 4, 4, DustID.Torch, 0f, 0f, 100);
			if (!Main.rand.NextBool(3))
				d.noGravity = true;

			d.velocity *= 0.3f;
			d.velocity.Y -= 1.5f;
			d.position = player.RotatedRelativePoint(d.position);
		}

		Vector2 position = player.RotatedRelativePoint(new Vector2(player.itemLocation.X + 12f * player.direction + player.velocity.X, player.itemLocation.Y - 14f + player.velocity.Y), true);
		Lighting.AddLight(position, Light);
	}

	public override void PostUpdate()
	{
		if (!Item.wet)
			Lighting.AddLight(Item.Center, Light);
	}
}