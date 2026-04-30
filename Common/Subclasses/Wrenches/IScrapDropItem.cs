namespace SpiritReforged.Common.Subclasses.Wrenches;

/// <summary>
/// Marks an item as a melee (hitbox, not class) weapon that can drop scrap when hitting bosses or killing enemies.<br/>
/// Hooks: <see cref="OverrideConditions(Item, Player, NPC, ref bool)"/>
/// </summary>
internal interface IScrapDropItem
{
	/// <summary>
	/// Implementation for the scrap dropper.
	/// </summary>
	public class ScrapDropItem : GlobalItem
	{
		public override void OnHitNPC(Item item, Player player, NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (item.ModItem is not IScrapDropItem scrap)
				return;

			bool autoDrop = true;

			if (scrap.OverrideConditions(item, player, target, ref autoDrop))
			{
				if (autoDrop)
					SpawnScrap(player, target);

				return;
			}	

			if (target.boss)
			{
				if (Main.rand.NextBool(8))
					SpawnScrap(player, target);
			}
			else if (target.life <= 0)
				SpawnScrap(player, target);
		}

		private static void SpawnScrap(Player player, NPC target) => Item.NewItem(player.GetSource_OnHit(target), target.Hitbox, ModContent.ItemType<ScrapItem>());
	}

	/// <summary>
	/// Allows an item to define under what conditions scrap is dropped.<br/>
	/// <paramref name="autoDrop"/> can be used to override if the scrap drops automatically or manually. Defaults to true.
	/// </summary>
	public bool OverrideConditions(Item item, Player player, NPC target, ref bool autoDrop) => false;
}
