using SpiritReforged.Common.PlayerCommon;
using SpiritReforged.Common.SimpleEntity;
using SpiritReforged.Common.TileCommon.TileSway;
using SpiritReforged.Common.TileCommon.Tree;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Savanna.Tiles.AcaciaTree;

public class TreetopPlatform : SimpleEntity
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

		if (!ModContent.GetInstance<AcaciaTree>().IsTreeTop(TreePosition.Value.X, TreePosition.Value.Y))
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
		On_Projectile.AI_007_GrapplingHooks += CheckGrappling;
		On_NPC.UpdateCollision += CheckNPCCollision;
		TileSwaySystem.PreUpdateWind += PreserveWindCounter;
	}

	private static void CheckGrappling(On_Projectile.orig_AI_007_GrapplingHooks orig, Projectile self)
	{
		if (self.type != ProjectileID.SquirrelHook) //Only allow the Squirrel Hook to grapple platforms
		{
			orig(self);
			return;
		}

		foreach (var p in AcaciaTree.Platforms)
		{
			const int height = 4;
			var hitbox = new Rectangle(p.Hitbox.X, p.Hitbox.Y + height + 16, p.Hitbox.Width, height); //Adjust the hitbox to be more grapple friendly

			if (self.getRect().Intersects(hitbox) && !Collision.SolidCollision(self.Bottom, self.width, 8))
			{
				self.Center = new Vector2(self.Center.X, hitbox.Center.Y);
				Latch(self);
			}
		}

		orig(self);

		static void Latch(Projectile p)
		{
			var owner = Main.player[p.owner];

			p.ai[0] = 2f;
			p.velocity *= 0;
			p.netUpdate = true;

			owner.grappling[0] = p.whoAmI;
			owner.grapCount++;
			owner.GrappleMovement();

			if (Main.netMode != NetmodeID.SinglePlayer && p.owner == Main.myPlayer)
				NetMessage.SendData(MessageID.PlayerControls, -1, -1, null, p.owner);
		}
	}

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

	public void Unload() { }
}
