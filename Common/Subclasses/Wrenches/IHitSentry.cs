using SpiritReforged.Common.ItemCommon;
using Terraria.Audio;

namespace SpiritReforged.Common.Subclasses.Wrenches;

/// <summary> Defines an item as a "wrench"; that is, a melee weapon that can hit sentries.<br/>
/// Hooks: <see cref="CanHitSentry(Player, Projectile)"/>, <see cref="OnHitSentry(Player, Projectile)"/>, <see cref="PreHitEffects(ref SoundStyle, ref int, ref int)"/> </summary>
public interface IHitSentry
{
	public class SentryHitProjectile : GlobalProjectile
	{
		public override bool AppliesToEntity(Projectile entity, bool lateInstantiation) => lateInstantiation && entity.ModProjectile is IHitSentry;

		public override void PostAI(Projectile projectile)
		{
			if (projectile.ModProjectile is not IHitSentry)
				return;

			Main.player[projectile.owner].GetModPlayer<WrenchPlayer>().IsSentryHitProjectile[projectile.whoAmI] = true;
		}
	}

	public static void DropScrap(Player attacker, NPC target)
	{
		if (target.boss && Main.rand.NextBool(8))
			ItemMethods.NewItemSynced(attacker.GetSource_OnHit(target), ModContent.ItemType<ScrapPickup>(), target.Center);
		else if (target.life <= 0)
			ItemMethods.NewItemSynced(attacker.GetSource_OnHit(target), ModContent.ItemType<ScrapPickup>(), target.Center);
	}

	/// <summary>  Whether the player can hit a sentry. Returns true by default. </summary>
	public bool CanHitSentry(Player player, Projectile sentry) => true;

	/// <summary> Runs when the player hits a sentry. </summary>
	public void OnHitSentry(Player player, Projectile sentry);

	/// <summary> Runs before the default hit effects occur. </summary>
	public bool PreHitEffects(ref SoundStyle style, ref int dustType, ref int dustCount) => true;

	/// <summary> Allows modification of the sentry's "immune frames".<br/>
	/// <paramref name="isMelee"/> can be modified to make the i-frames last only as long as the current item is being used. </summary>
	/// <param name="sentry"></param>
	/// <param name="time"></param>
	public void ModifySentryImmuneTime(Projectile sentry, ref int time, ref bool isMelee) { }
}