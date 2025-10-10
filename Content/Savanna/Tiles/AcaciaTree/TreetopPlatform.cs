using SpiritReforged.Common.PlayerCommon;
using SpiritReforged.Common.ProjectileCommon;
using SpiritReforged.Common.SimpleEntity;
using SpiritReforged.Common.TileCommon.TileSway;
using SpiritReforged.Common.TileCommon.Tree;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Savanna.Tiles.AcaciaTree;

public class TreetopPlatform : SimpleEntity, IGrappleable
{
	public override string TexturePath => AssetLoader.EmptyTexture;
	public Point16? TreePosition { get; private set; }

	public override void Load()
	{
		width = 288;
		height = 8;
	}

	public override void Update()
	{
		TreePosition ??= Center.ToPoint16(); //Initialize
		var tilePos = TreePosition.Value;
		//Use a position convenient to acacia treetops
		Center = tilePos.ToVector2() * 16 + new Vector2(8, -112) + TreeExtensions.GetPalmTreeOffset(tilePos.X, tilePos.Y);

		if (ModContent.GetInstance<AcaciaTree>().FindSegment(TreePosition.Value.X, TreePosition.Value.Y) is not CustomTree.SegmentType.LeafyTop)
			Kill();
	}

	public void UpdateStanding(Entity entity)
	{
		if (TreePosition is null)
			return;

		var pos = TreePosition.Value;
		float rotation = AcaciaTree.GetSway(pos.X, pos.Y);

		//The difference in rotation from last tick, used to control how much the entity displaces horizontally
		float diff = rotation - AcaciaTree.GetSway(pos.X, pos.Y, ModContent.GetInstance<AcaciaPlatformDetours>().OldTreeWindCounter);
		//Scalar based on the entity's distance from platform center
		float strength = (entity.Center.X - Center.X) / (width * .5f);
		//How much the entity is displaced by the previous factors
		float disp = (entity is NPC) ? 5f : 10f;

		entity.velocity.Y = 0;

		var newPosition = new Vector2(entity.position.X + diff * disp, Hitbox.Top + 10 - entity.height + rotation * strength * disp);
		if (!Collision.SolidCollision(newPosition, entity.width, entity.height))
			entity.position = newPosition;

		if (entity is Player player)
		{
			player.Rotate(rotation * .07f, new Vector2(player.width * .5f, player.height));
			player.gfxOffY = 0;
		}
	}

	public bool CanGrapple(Projectile hook)
	{
		if (hook.type != ProjectileID.SquirrelHook) //Only allow the Squirrel Hook to grapple platforms
			return false;

		const int height = 4;
		var hitbox = new Rectangle(Hitbox.X, Hitbox.Y + height + 16, Hitbox.Width, height); //Adjust the hitbox to be more grapple friendly

		if (hook.getRect().Intersects(hitbox) && !Collision.SolidCollision(hook.position, hook.width, hook.height))
		{
			hook.Center = new Vector2(hook.Center.X, hitbox.Center.Y);
			GrappleHelper.Latch(hook);

			return true;
		}

		return false;
	}
}

internal class AcaciaPlatformPlayer : ModPlayer
{
	public override void PreUpdateMovement()
	{
		foreach (var p in AcaciaTree.Platforms)
		{
			var lowRect = Player.getRect() with { Height = Player.height / 2, Y = (int)Player.position.Y + Player.height / 2 };
			if (lowRect.Intersects(p.Hitbox) && Player.velocity.Y >= 0 && !Player.FallThrough())
			{
				p.UpdateStanding(Player);

				if (Player.controlDown)
					Player.GetModPlayer<CollisionPlayer>().fallThrough = true;

				break; //It would be redundant to check for other platforms when the player is already on one
			}
		}
	}
}

internal class AcaciaPlatformDetours : ILoadable
{
	public double OldTreeWindCounter { get; private set; }

	public void Load(Mod mod)
	{
		On_NPC.UpdateCollision += CheckNPCCollision;
		TileSwaySystem.PreUpdateWind += PreserveWindCounter;
	}
	public void Unload() { }

	private static void CheckNPCCollision(On_NPC.orig_UpdateCollision orig, NPC self)
	{
		if (!self.noGravity)
		{
			foreach (var p in AcaciaTree.Platforms)
				if (self.getRect().Intersects(p.Hitbox) && self.velocity.Y >= 0)
					p.UpdateStanding(self);
		}

		orig(self);
	}

	private void PreserveWindCounter() => OldTreeWindCounter = TileSwaySystem.Instance.TreeWindCounter;
}