using SpiritReforged.Common.BuffCommon;
using SpiritReforged.Common.EmoteCommon;
using Terraria.DataStructures;
using Terraria.GameContent.UI;

namespace SpiritReforged.Content.Desert.ScarabBoss.Items.ScarabPet;

public class ScarabPetProjectile : ModProjectile, IModifyEmotePosition
{
	const float GROUND_MOVE_SPEED = 6f;
	const int COMPLETE_IDLE_DISTANCE = 300;

	private static Asset<Texture2D> Glow = null;

	private Player Owner => Main.player[Projectile.owner];
	private bool HasJumped { get => Projectile.ai[0] != 0; set => Projectile.ai[0] = value ? 1 : 0; }
	private bool Jumping { get => Projectile.ai[1] != 0; set => Projectile.ai[1] = value ? 1 : 0; }
	private ref float EmoteTimer => ref Projectile.ai[2];

	private int _state = 0;

	public override void SetStaticDefaults()
	{
		Main.projFrames[Type] = 2;
		Main.projPet[Type] = true;

		Glow = ModContent.Request<Texture2D>(Texture + "_Glow");

		ProjectileID.Sets.CharacterPreviewAnimations[Type]
			.WithOffset(-12, 0)
			.WithSpriteDirection(-1)
			.WithCode(CustomAnimation);
	}

	public static void CustomAnimation(Projectile proj, bool walking)
	{
		if (walking)
		{
			if (++proj.frameCounter >= 5)
			{
				proj.frameCounter = 0;

				if (++proj.frame > 11)
					proj.frame = 9;
			}
		}
		else
			proj.frame = 0;
	}

	public override void SetDefaults()
	{
		Projectile.CloneDefaults(ProjectileID.Truffle);
		Projectile.aiStyle = 0;
		Projectile.width = 40;
		Projectile.height = 40;
		Projectile.light = 0;

		AIType = ProjectileID.None;
	}

	public override void AI()
	{
		Main.player[Projectile.owner].GetModPlayer<PetPlayer>().PetFlag(Projectile);

		if (Projectile.velocity.X > 0)
			Projectile.spriteDirection = -1;
		else if (Projectile.velocity.X < 0)
			Projectile.spriteDirection = 1;

		if (_state == 0)
			NearbyMovement();
		else if (_state == 1)
			FollowPlayerGround();
		else
			FollowPlayerFlight();
	}

	private void NearbyMovement()
	{
		if (HasJumped) // Start unfurl animation here
			Projectile.frameCounter = 25;

		HasJumped = false;
		Jumping = false;

		if (Projectile.frameCounter <= 0) // Base frame
			Projectile.frame = 0;
		else // Unfurl
		{
			Projectile.frameCounter--;

			if (Projectile.frameCounter > 20)
				Projectile.frame = 12;
			else if (Projectile.frameCounter > 15)
				Projectile.frame = 13;
			else if (Projectile.frameCounter > 10)
				Projectile.frame = 14;
			else if (Projectile.frameCounter > 5)
				Projectile.frame = 15;
			else
				Projectile.frame = 16;
		}

		Projectile.velocity.X *= 0.95f;
		Projectile.velocity.Y += 0.2f;

		if (Math.Abs(Projectile.velocity.X) < 0.1f)
		{
			Projectile.spriteDirection = Projectile.direction = -Math.Sign(Owner.Center.X - Projectile.Center.X);

			if (EmoteTimer++ > 240 && Main.rand.NextBool(80))
			{
				int emote = Main.rand.Next(3) switch
				{
					0 => EmoteID.EmotionLove,
					1 => EmoteID.ItemGoldpile,
					_ => EmoteID.EmoteHappiness,
				};

				EmoteBubble.NewBubble(emote, new WorldUIAnchor(Projectile), 120);
				EmoteTimer = 0;
			}
		}

		float throwaway = 6;
		Collision.StepUp(ref Projectile.position, ref Projectile.velocity, Projectile.width, Projectile.height, ref throwaway, ref Projectile.gfxOffY);

		if (Projectile.DistanceSQ(Main.player[Projectile.owner].Center) > COMPLETE_IDLE_DISTANCE * COMPLETE_IDLE_DISTANCE)
			ResetState(1);
	}

	private void FollowPlayerGround()
	{
		Projectile.velocity.Y += 0.2f;

		if (!HasJumped)
		{
			HasJumped = true;
			Jumping = true;

			Projectile.velocity.Y = -4f;
			Projectile.frameCounter = 20;
		}
		else
		{
			if (!Jumping)
			{
				float throwaway = 6;
				Collision.StepUp(ref Projectile.position, ref Projectile.velocity, Projectile.width, Projectile.height, ref throwaway, ref Projectile.gfxOffY);

				if (Owner.Center.X < Projectile.Center.X)
					Projectile.velocity.X = MathHelper.Lerp(Projectile.velocity.X, -GROUND_MOVE_SPEED, 0.15f);
				else
					Projectile.velocity.X = MathHelper.Lerp(Projectile.velocity.X, GROUND_MOVE_SPEED, 0.15f);

				Projectile.frameCounter--;

				int offset = Math.Abs(Projectile.frameCounter % 15); // Roll loop
				if (offset > 10)
					Projectile.frame = 9;
				else if (offset > 5)
					Projectile.frame = 10;
				else
					Projectile.frame = 11;

				float dist = Projectile.DistanceSQ(Main.player[Projectile.owner].Center);
				if (dist <= Math.Pow(COMPLETE_IDLE_DISTANCE - 50, 2))
					ResetState(0);
				else if (dist > 800 * 800)
					ResetState(2);

				if (Projectile.velocity.Y == 0.2f && Main.rand.NextBool(40))
					Projectile.velocity.Y = Main.rand.NextFloat(-2.5f, -1.2f);
			}
			else
			{
				Projectile.frameCounter--;

				if (Projectile.frameCounter > 15)
					Projectile.frame = 1;
				else if (Projectile.frameCounter > 10)
					Projectile.frame = 2;
				else if (Projectile.frameCounter > 5)
					Projectile.frame = 3;
				else
				{
					int offset = Math.Abs(Projectile.frameCounter % 20); // Falling loop
					if (offset > 15)
						Projectile.frame = 4;
					else if (offset > 10)
						Projectile.frame = 5;
					else if (offset > 5)
						Projectile.frame = 6;
					else
						Projectile.frame = 7;
				}

				if (Projectile.velocity.Y == 0.2f) // Tile collision edge case
				{
					Jumping = false;

					Projectile.frame = 8;
					Projectile.frameCounter = 10;
				}
			}
		}
	}

	private void FollowPlayerFlight()
	{
		const float MaxSpeedDistance = 1400;

		Projectile.tileCollide = false;

		float dist = Projectile.DistanceSQ(Main.player[Projectile.owner].Center);
		float magnitude = 13;

		if (dist > MaxSpeedDistance * MaxSpeedDistance)
			magnitude = 13 + ((float)Math.Sqrt(dist) - MaxSpeedDistance) * 0.1f;

		Projectile.velocity = Vector2.Lerp(Projectile.velocity, Projectile.DirectionTo(Owner.Center) * magnitude, 0.05f);

		Player player = Main.player[Projectile.owner];
		if (dist < 700 * 700 && Collision.CanHitLine(Projectile.position, Projectile.width, Projectile.height, player.position, player.width, player.height))
		{
			Projectile.tileCollide = true;
			ResetState(1);
		}
		
		Projectile.frameCounter--;
		int offset = Math.Abs(Projectile.frameCounter % 9);

		if (offset > 6)
			Projectile.frame = 9;
		else if (offset > 3)
			Projectile.frame = 10;
		else
			Projectile.frame = 11;
	}

	private void ResetState(int newState)
	{
		_state = newState;

		Projectile.frameCounter = 0;
	}

	public override bool OnTileCollide(Vector2 oldVelocity)
	{
		if (HasJumped && Jumping)
		{
			Jumping = false;

			Projectile.frame = 8;
			Projectile.frameCounter = 10;
		}

		return false;
	}

	public override bool PreDraw(ref Color lightColor)
	{
		Texture2D tex = ModContent.Request<Texture2D>(Texture).Value;
		var rect = new Rectangle(0, 0, 42, 40);
		SpriteEffects effect = Projectile.spriteDirection == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

		if (Projectile.frame <= 3)
			rect.Y = Projectile.frame * 42;
		else if (Projectile.frame >= 4 && Projectile.frame <= 8)
		{
			rect.X = 42;
			rect.Y = (Projectile.frame - 4) * 42;
		}
		else if (Projectile.frame >= 9 && Projectile.frame <= 11)
		{
			rect.X = 84;
			rect.Y = (Projectile.frame - 9) * 42;
		}
		else
		{
			rect.X = 126;
			rect.Y = (Projectile.frame - 12) * 42;
		}

		Vector2 drawPos = Projectile.position - Main.screenPosition;
		Main.EntitySpriteDraw(tex, drawPos, rect, lightColor, Projectile.rotation, Vector2.Zero, 1f, effect, 0);
		Main.EntitySpriteDraw(Glow.Value, drawPos, rect, Projectile.GetAlpha(Color.White), Projectile.rotation, Vector2.Zero, 1f, effect, 0);
		return false;
	}

	void IModifyEmotePosition.ModifyEmotePosition(ref Vector2 position)
	{
		position.X += Projectile.direction * 2;
		position.Y += 16;
	}
}
