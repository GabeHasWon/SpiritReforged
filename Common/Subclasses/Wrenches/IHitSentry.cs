using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Content.Particles;
using Terraria.Audio;

namespace SpiritReforged.Common.Subclasses.Wrenches;

/// <summary> Defines an item as a "wrench"; that is, a melee weapon that can hit sentries.<br/>
/// Hooks: <see cref="CanHitSentry(Player, Projectile)"/>, <see cref="OnHitSentry(Player, Projectile)"/>, <see cref="PreHitEffects(ref SoundStyle, ref int, ref int)"/> </summary>
public interface IHitSentry
{
	public sealed class SentryHitProjectile : GlobalProjectile
	{
		public override bool AppliesToEntity(Projectile entity, bool lateInstantiation) => lateInstantiation && entity.ModProjectile is IHitSentry;

		public override void PostAI(Projectile projectile)
		{
			Player owner = Main.player[projectile.owner];
			if (projectile.ModProjectile is not IHitSentry iHitSentry || !owner.TryGetModPlayer(out WrenchPlayer wrenchPlayer))
				return;

			foreach (Projectile sentry in Main.ActiveProjectiles)
			{
				if (!sentry.sentry || sentry.owner != projectile.owner)
					continue;

				if (projectile.Colliding(projectile.Hitbox, sentry.Hitbox) && iHitSentry.CanHitSentry(owner, sentry))
				{
					int immuneTime = owner.itemAnimationMax - 2;
					iHitSentry.OnHitSentry(owner, sentry, ref immuneTime);

					wrenchPlayer.sentryImmune[sentry.type] = immuneTime; //Set immune time
				}
			}
		}
	}

	public sealed class SentryHitItem : GlobalItem
	{
		public override void UseItemHitbox(Item item, Player player, ref Rectangle hitbox, ref bool noHitbox)
		{
			if (noHitbox || item.ModItem is not IHitSentry iHitSentry || !player.TryGetModPlayer(out WrenchPlayer wrenchPlayer))
				return;

			foreach (Projectile projectile in Main.ActiveProjectiles)
			{
				if (!projectile.sentry || projectile.owner != player.whoAmI)
					continue;

				if (hitbox.Intersects(projectile.Hitbox) && iHitSentry.CanHitSentry(player, projectile))
				{
					int immuneTime = player.itemAnimationMax - 2;
					iHitSentry.OnHitSentry(player, projectile, ref immuneTime);
					
					wrenchPlayer.sentryImmune[projectile.type] = immuneTime; //Set immune time
				}
			}
		}
	}

	public static void DropScrap(Player attacker, NPC target)
	{
		if (target.boss && Main.rand.NextBool(8))
			ItemMethods.NewItemSynced(attacker.GetSource_OnHit(target), ModContent.ItemType<ScrapPickup>(), target.Center);
		else if (target.life <= 0)
			ItemMethods.NewItemSynced(attacker.GetSource_OnHit(target), ModContent.ItemType<ScrapPickup>(), target.Center);
	}

	public static void ClientHitEffects(Projectile sentry, int dustCount = 4, int dustType = DustID.MinecartSpark)
	{
		SoundEngine.PlaySound(SoundID.Item53 with { Pitch = 0.5f, PitchVariance = 0.3f });
		SoundEngine.PlaySound(SoundID.Item52 with { Pitch = -0.5f, PitchVariance = 0.5f });

		for (int i = 0; i < dustCount; i++)
		{
			Vector2 position = sentry.BottomLeft + new Vector2(Main.rand.NextFloat(sentry.width), 0);
			Vector2 velocity = Vector2.UnitY * -Main.rand.NextFloat(4);

			Dust dust = Main.dust[Dust.NewDust(sentry.position, sentry.width, sentry.height, dustType)];
			dust.fadeIn = 3;
			dust.scale = 1f;
			dust.velocity = velocity;

			ParticleHandler.SpawnParticle(new ImpactLine(position, velocity, Color.PaleGoldenrod.Additive(100), new Vector2(0.5f, Math.Abs(velocity.Y) / 2), 30));
			ParticleHandler.SpawnParticle(new ImpactLine(position, velocity, Color.White.Additive(), new Vector2(0.25f, Math.Abs(velocity.Y) / 4), 30));
		}

		ParticleHandler.SpawnParticle(new PulseCircle(sentry.Center, Color.PaleGoldenrod.Additive(100), Color.Goldenrod, 0.1f, 200, 30, Easing.EaseFunction.EaseOutBack(0.2f)));
		ParticleHandler.SpawnParticle(new ImpactLinePrim(sentry.Center, Vector2.Zero, Color.PaleGoldenrod.Additive(100), new Vector2(2, 3), 20, 0)
		{ Rotation = MathHelper.PiOver2 });
	}

	/// <summary> Whether the player can hit a sentry. Returns true by default. </summary>
	public bool CanHitSentry(Player player, Projectile sentry) => player.GetModPlayer<WrenchPlayer>().StoredScrap > 0 && player.GetModPlayer<WrenchPlayer>().sentryImmune[sentry.type] == 0;

	/// <summary> Runs when the player hits a sentry. </summary>
	public void OnHitSentry(Player player, Projectile sentry, ref int immuneTime) => ClientHitEffects(sentry);

	/// <summary> Runs before the default hit effects occur. </summary>
	public bool PreHitEffects(ref SoundStyle style, ref int dustType, ref int dustCount) => true;

	/// <summary> Allows modification of the sentry's "immune frames".<br/>
	/// <paramref name="isMelee"/> can be modified to make the i-frames last only as long as the current item is being used. </summary>
	/// <param name="sentry"></param>
	/// <param name="time"></param>
	public void ModifySentryImmuneTime(Projectile sentry, ref int time, ref bool isMelee) { }
}