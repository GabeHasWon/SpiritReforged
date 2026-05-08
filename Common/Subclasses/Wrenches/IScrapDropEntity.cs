namespace SpiritReforged.Common.Subclasses.Wrenches;

#nullable enable

/// <summary>
/// Marks an item or projectile as a melee (hitbox, not class) weapon that can drop scrap when hitting bosses or killing enemies.<br/>
/// Note that, for projectiles, this only works for player-owned projectiles, and will have issues otherwise.<br/>
/// Hooks: <see cref="OverrideConditions(Player, NPC, ref bool)"/>
/// </summary>
internal interface IScrapDropEntity
{
	/// <summary>
	/// Implementation for the scrap dropper.
	/// </summary>
	public class ScrapDropItem : GlobalItem
	{
		public override void OnHitNPC(Item item, Player player, NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (item.ModItem is not IScrapDropEntity scrap)
				return;

			TrySpawnScrap(player, target, scrap);
		}
	}

	public class ScrapDropProjectile : GlobalProjectile
	{
		public override bool AppliesToEntity(Projectile entity, bool lateInstantiation) => lateInstantiation && entity.ModProjectile is IScrapDropEntity;

		public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (projectile.ModProjectile is not IScrapDropEntity scrap)
				return;

			TrySpawnScrap(Main.player[projectile.owner], target, scrap);
		}
	}

	// Whoops! Wrote all this before I realized it's not useful. Too bad! - Gabe
	///// <summary>
	///// Contains either an item or projectile, but not both, which is a <see cref="IScrapDropEntity"/>.
	///// </summary>
	//public readonly struct ItemOrProj
	//{
	//	public readonly Item? Item;
	//	public readonly Projectile? Projectile;

	//	public ItemOrProj(Item? Item = null, Projectile? Projectile = null)
	//	{
	//		this.Item = Item;
	//		this.Projectile = Projectile;

	//		if (Item is not null && Projectile is not null)
	//			throw new ArgumentException("One of Item or Projectile must be null.");
	//	}

	//	public IScrapDropEntity Scrap => (Item is null ? Projectile!.ModProjectile as IScrapDropEntity : Item.ModItem as IScrapDropEntity)!;
	//}

	private static void SpawnScrap(Player player, NPC target) => Item.NewItem(player.GetSource_OnHit(target), target.Hitbox, ModContent.ItemType<ScrapItem>());

	private static void TrySpawnScrap(Player player, NPC target, IScrapDropEntity scrap)
	{
		bool autoDrop = true;

		if (scrap.OverrideConditions(player, target, ref autoDrop))
		{
			if (autoDrop)
				StandardScrapSpawn(player, target);

			return;
		}

		StandardScrapSpawn(player, target);
	}

	static void StandardScrapSpawn(Player player, NPC target)
	{
		if (target.boss)
		{
			if (Main.rand.NextBool(8))
				SpawnScrap(player, target);
		}
		else if (target.life <= 0)
			SpawnScrap(player, target);
	}

	/// <summary>
	/// Allows an item or projectile to define under what conditions scrap is dropped.<br/>
	/// <paramref name="autoDrop"/> can be used to override if the scrap drops automatically or manually. Defaults to true.
	/// </summary>
	public bool OverrideConditions(Player player, NPC target, ref bool autoDrop) => false;
}
