namespace SpiritReforged.Common.ItemCommon.FloatingItem;

public abstract class FloatingItem : ModItem
{
	public virtual float SpawnWeight => 1f;
	public virtual float Bouyancy => -0.1f;
	public virtual float Weight => 0.1f;

	public override void Update(ref float gravity, ref float maxFallSpeed)
	{
		if (Item.shimmerWet)
			return;

		if (Item.wet) //Float
		{
			gravity = Bouyancy;
			Item.velocity.Y = Math.Max(Item.velocity.Y, Bouyancy * 7);

			if (!Collision.WetCollision(Item.position - Vector2.UnitY * 4, Item.width, Item.height))
				gravity = Weight;
		}
		else
		{
			gravity = Weight;
		}
	}

	public override void GrabRange(Player player, ref int grabRange) => grabRange = (int)(grabRange * 0.66f);
}