using Terraria.DataStructures;

namespace SpiritReforged.Common.PlayerCommon;

[ReinitializeDuringResizeArrays]
public class StatsPlayer : ModPlayer
{
	public class CoinDropNPC : GlobalNPC
	{
		public override bool PreKill(NPC npc)
		{
			Player p = Main.player[npc.lastInteraction];
			float mult = p.GetModPlayer<StatsPlayer>().CoinDrops.ApplyTo(1f);

			npc.value *= mult;

			return true;
		}
	}

	/// <summary> Whether this weapon is innacurate and <b>NOT</b> a shotgun. Typically used by vanilla IDs to allow for <see cref="Innacurate"/> stat adjustments. </summary>
	public static readonly bool[] Innacurate = ItemID.Sets.Factory.CreateNamedSet(nameof(Innacurate)).Description("Whether this item shoots with any level of innacuracy").RegisterBoolSet(ItemID.Minishark);

	public StatModifier CoinDrops = StatModifier.Default;
	public StatModifier Inaccuracy = StatModifier.Default;
	public StatModifier ShotSpeed = StatModifier.Default;
	public StatModifier ShotCount = StatModifier.Default;

	public override void Load() => On_Projectile.ApplyStatsFromSource += SourceVelocity;

	public override void ResetEffects()
	{
		CoinDrops = StatModifier.Default;
		Inaccuracy = StatModifier.Default;
		ShotSpeed = StatModifier.Default;
		ShotCount = StatModifier.Default;
	}

	public override void ModifyShootStats(Item item, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback) => velocity *= ShotSpeed.ApplyTo(1f); //Shot speed adjustments

	private static void SourceVelocity(On_Projectile.orig_ApplyStatsFromSource orig, Projectile self, IEntitySource spawnSource)
	{
		orig(self, spawnSource);
		if (spawnSource is EntitySource_Parent sourceParent && sourceParent.Entity is Player player && spawnSource is IEntitySource_WithStatsFromItem sourceItem)
		{
			Item item = sourceItem.Item;
			if (Innacurate[item.type]) //Accuracy adjustments
			{
				float currentAngle = self.velocity.ToRotation();
				float targetAngle = player.MountedCenter.AngleTo(Main.MouseWorld);

				self.velocity = self.velocity.RotatedBy(Utils.AngleLerp(currentAngle, targetAngle, 1f - player.GetModPlayer<StatsPlayer>().Inaccuracy.ApplyTo(1f)) - currentAngle);
			}
		}
	}
}