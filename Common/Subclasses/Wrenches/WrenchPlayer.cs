using System.Runtime.CompilerServices;
using Terraria.Audio;

namespace SpiritReforged.Common.Subclasses.Wrenches;

internal class WrenchPlayer : ModPlayer
{
	const int MeleeHitTime = 60000;

	/// <summary>
	/// Sparse array where projectiles set their own flag in <see cref="ISentryHitEntity.SentryHitProjectile.PostAI(Projectile)"/> for checking in <see cref="PostUpdateEquips"/>.
	/// </summary>
	internal readonly bool[] IsSentryHitProjectile = new bool[Main.maxProjectiles];

	/// <summary>
	/// Internal sparse array for per-projectile immune frames.
	/// </summary>
	private readonly int[] _sentryImmune = [];

	public int storedScrap = 0;

	public override void PostUpdateEquips() 
	{
		bool hasItem = Player.itemAnimation > 0 && !Player.ItemAnimationJustStarted;
		Rectangle drawHitbox = Item.GetDrawHitbox(Player.HeldItem.type, Player);
		GetItemHitbox(Player, Player.HeldItem, drawHitbox, out _, out Rectangle hitbox);

		for (int i = 0; i < _sentryImmune.Length; ++i)
		{
			ref int timer = ref _sentryImmune[i];

			if (timer == MeleeHitTime && !hasItem) // Melee hits are hardcoded to only reset when the item being used "resets"
				timer = 0;
			else
				timer = Math.Max(timer - 1, 0);
		}

		foreach (Projectile proj in Main.ActiveProjectiles)
		{
			if (proj.owner != Player.whoAmI || !proj.sentry || _sentryImmune[proj.whoAmI] > 0)
				continue;

			if (hasItem && hitbox.Intersects(proj.Hitbox) && Player.HeldItem.ModItem is ISentryHitEntity wrench && wrench.CanHitSentry(Player, proj))
				OnHitSentry(wrench, proj, true);

			for (int i = 0; i < IsSentryHitProjectile.Length; ++i)
			{
				if (IsSentryHitProjectile[i] && Main.projectile[i].ModProjectile is ISentryHitEntity wrenchProj && wrenchProj.CanHitSentry(Player, proj))
					OnHitSentry(wrenchProj, proj, false);

				IsSentryHitProjectile[i] = false;
			}
		}
	}

	private void OnHitSentry(ISentryHitEntity wrench, Projectile proj, bool isMelee)
	{
		wrench.OnHitSentry(Player, proj);
		_sentryImmune[proj.whoAmI] = 15; // Immune time defaults to 15 frames (1/4th of a second)...

		wrench.ModifySentryImmuneTime(proj, ref _sentryImmune[proj.whoAmI], ref isMelee);

		if (isMelee) // ...unless the item or self-marked projectile counts as "melee", where it will last for as long as the item is being used
			_sentryImmune[proj.whoAmI] = MeleeHitTime;

		SoundStyle sound = Main.rand.NextBool() ? SoundID.Item53 : SoundID.Item52;
		int dustType = DustID.MinecartSpark;
		int dustCount = 4;

		if (wrench.PreHitEffects(ref sound, ref dustType, ref dustCount))
		{
			SoundEngine.PlaySound(sound with { PitchRange = (-0.2f, 0.2f) });

			for (int i = 0; i < dustCount; ++i)
			{
				Dust dust = Main.dust[Dust.NewDust(proj.position, proj.width, proj.height, dustType)];
				dust.fadeIn = 2;
				dust.scale = 0.2f;
			}
		}
	}

	[UnsafeAccessor(UnsafeAccessorKind.Method, Name = "ItemCheck_GetMeleeHitbox")]
	public static extern void GetItemHitbox(Player player, Item sItem, Rectangle heldItemFrame, out bool dontAttack, out Rectangle itemRectangle);
}
