using System.Runtime.CompilerServices;
using Terraria.Audio;

namespace SpiritReforged.Common.Subclasses.Wrenches;

internal class WrenchPlayer : ModPlayer
{
	private readonly HashSet<int> sentryImmune = [];

	public override void PostUpdateEquips() 
	{
		if (Player.HeldItem.ModItem is not ISentryHitItem wrench || Player.itemAnimation <= 0 || Player.ItemAnimationJustStarted)
		{
			if (sentryImmune.Count > 0)
				sentryImmune.Clear();

			return;
		}

		Rectangle drawHitbox = Item.GetDrawHitbox(Player.HeldItem.type, Player);
		GetItemHitbox(Player, Player.HeldItem, drawHitbox, out _, out Rectangle hitbox);

		foreach (Projectile proj in Main.ActiveProjectiles)
		{
			if (proj.owner == Player.whoAmI && proj.sentry && !sentryImmune.Contains(proj.whoAmI) && hitbox.Intersects(proj.Hitbox) && wrench.CanHitSentry(Player, proj))
			{
				wrench.OnHitSentry(Player, proj);
				sentryImmune.Add(proj.whoAmI);

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
		}
	}

	[UnsafeAccessor(UnsafeAccessorKind.Method, Name = "ItemCheck_GetMeleeHitbox")]
	public static extern void GetItemHitbox(Player player, Item sItem, Rectangle heldItemFrame, out bool dontAttack, out Rectangle itemRectangle);
}
