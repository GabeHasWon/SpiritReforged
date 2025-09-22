using SpiritReforged.Common.SimpleEntity;
using SpiritReforged.Content.Desert.Tiles.Chains;

namespace SpiritReforged.Common.ProjectileCommon;

public sealed class GrappleHelper : ModSystem
{
	public override void Load() => On_Projectile.AI_007_GrapplingHooks += FindGrapple;
	private static void FindGrapple(On_Projectile.orig_AI_007_GrapplingHooks orig, Projectile self)
	{
		bool success = false;
		if (!success)
		{
			foreach (ChainObject o in ChainObjectSystem.Objects)
			{
				if (success |= o.CanGrapple(self))
					break;
			}
		}

		if (!success)
		{
			foreach (SimpleEntity.SimpleEntity e in SimpleEntitySystem.Entities)
			{
				if (e is IGrappleable grappleable && (success |= grappleable.CanGrapple(self)))
					break;
			}
		}

		orig(self);
	}

	public static void Latch(Projectile grapple)
	{
		var owner = Main.player[grapple.owner];

		grapple.ai[0] = 2f;
		grapple.velocity *= 0;
		grapple.netUpdate = true;

		owner.grappling[0] = grapple.whoAmI;
		owner.grapCount++;
		owner.GrappleMovement();

		if (Main.netMode != NetmodeID.SinglePlayer && grapple.owner == Main.myPlayer)
			NetMessage.SendData(MessageID.PlayerControls, -1, -1, null, grapple.owner);
	}
}

public interface IGrappleable
{
	public bool CanGrapple(Projectile hook);
}