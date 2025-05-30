using SpiritReforged.Common.BuffCommon;
using Terraria.Audio;
using Terraria.GameContent.Golf;

namespace SpiritReforged.Content.Savanna.Items.WrithingSticks;

[AutoloadPetBuff]
public class TumblerPet : ModProjectile
{
	private static Asset<Texture2D> Highlight;

	private bool readyToGolf;
	public ref float Counter => ref Projectile.ai[0];
	public ref float GolfTime => ref Projectile.ai[1];
	private const int GolfTimeMax = 40;

	public override void SetStaticDefaults()
	{
		Main.projPet[Type] = true;
		ProjectileID.Sets.CharacterPreviewAnimations[Type] = ProjectileID.Sets.SimpleLoop(0, Main.projFrames[Type])
			.WithSpriteDirection(-1)
			.WithCode(DelegateMethods.CharacterPreview.FloatAndSpinWhenWalking);
		ProjectileID.Sets.TrailCacheLength[Type] = 5;
		ProjectileID.Sets.TrailingMode[Type] = 1;

		if (!Main.dedServ)
			Highlight = ModContent.Request<Texture2D>(Texture + "_Highlight");
	}

	public override void SetDefaults() => Projectile.Size = new Vector2(24);

	public override void AI()
	{
		var owner = Main.player[Projectile.owner];

		Projectile.rotation += Projectile.velocity.Length() * .08f * Projectile.direction;

		const float speed = 5f;
		if (GolfTime == 0)
		{
			if (Projectile.Distance(owner.Center) > 16 * 80) //Teleport when very far out of range
			{
				Projectile.Center = owner.Center;
				Projectile.velocity = Vector2.Zero;
			}
			else if (Projectile.Distance(owner.Center) > 16 * 5) //Chase the player when reasonably out of range
			{
				if (Projectile.velocity.Y == 0)
				{
					if (Projectile.Distance(owner.Center) > 16 * 30 || Counter > 10)
						Projectile.tileCollide = false;

					if (++Counter % 10 == 0) //Hop periodically
					{
						if (Math.Abs(Projectile.velocity.X) < .2f)
							Projectile.velocity.Y = -5f;
						else
						{
							Projectile.velocity.Y = -3f * ((float)Math.Abs(Projectile.velocity.X) / speed);
							Counter = 0;
						}
					}
				}

				if (Projectile.tileCollide == false)
					Projectile.velocity = Common.MathHelpers.ArcVelocityHelper.GetArcVel(Projectile.Center, owner.Center, 4f, 10f);
				else
					Projectile.velocity.X = MathHelper.Lerp(Projectile.velocity.X, Math.Sign(owner.Center.X - Projectile.Center.X) * speed, .02f);
			}
			else //Slow down when nearby the player
			{
				Projectile.velocity.X *= .95f;
				Counter = 0;

				if (!WorldGen.SolidOrSlopedTile(Framing.GetTileSafely(Projectile.Center)))
					Projectile.tileCollide = true;
			}
		}

		HandleGolf(owner);
		Collision.StepUp(ref Projectile.position, ref Projectile.velocity, Projectile.width, Projectile.height, ref Projectile.stepSpeed, ref Projectile.gfxOffY);

		if (Math.Abs(Main.windSpeedCurrent) > .3f)
			Projectile.velocity.X += Main.windSpeedCurrent * .05f;

		Projectile.velocity.Y += .25f;
	}

	private void HandleGolf(Player owner)
	{
		bool InRange()
		{
			float collisionPoint = 0;
			return Collision.CheckAABBvLineCollision(Projectile.position, Projectile.Size, owner.Center, owner.Center + Vector2.UnitX * 30 * owner.direction, 10, ref collisionPoint);
		}

		if (GolfHelper.IsPlayerHoldingClub(owner) && InRange())
		{
			if (GolfTime == 0 && !owner.channel && owner.ItemAnimationActive && owner.itemAnimation == owner.itemAnimationMax / 2)
			{
				SoundEngine.PlaySound(SoundID.Item126 with { Pitch = 1f }, Projectile.Center);

				if (owner.whoAmI == Main.myPlayer)
				{
					Vector2 shotVector = (Main.MouseWorld - Projectile.Center) * .003f;
					GolfHelper.ShotStrength shotStrength = GolfHelper.CalculateShotStrength(shotVector, GolfHelper.GetClubProperties((short)owner.HeldItem.type));
					Vector2 vector = Vector2.Normalize(shotVector) * shotStrength.AbsoluteStrength;

					GolfHelper.HitGolfBall(Projectile, vector, shotStrength.RoughLandResistance);
					
					GolfTime = GolfTimeMax;
					Projectile.netUpdate = true;
				}

				for (int i = 0; i < 10; i++)
					Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.WoodFurniture, Scale: Main.rand.NextFloat(.8f, 1.5f)).velocity = Projectile.velocity * Main.rand.NextFloat(.5f);
			}

			if (owner.whoAmI == Main.myPlayer)
				readyToGolf = true;
		}
		else
			readyToGolf = false;

		if (GolfTime > 0)
		{
			if (owner.velocity.X != 0)
				GolfTime = 0; //Stop tracking when the player moves

			owner.remoteVisionForDrone = true;
			Main.DroneCameraTracker.Track(Projectile);
		}

		if ((int)Projectile.velocity.Length() == 0)
			GolfTime = MathHelper.Max(GolfTime - 1, 0);
	}

	public override bool TileCollideStyle(ref int width, ref int height, ref bool fallThrough, ref Vector2 hitboxCenterFrac)
	{
		fallThrough = false;
		return true;
	}

	public override bool OnTileCollide(Vector2 oldVelocity)
	{
		if (GolfTime > 0)
			Projectile.velocity.X *= .95f;

		if (Math.Abs(oldVelocity.Y) > 1)
		{
			Projectile.velocity.Y = oldVelocity.Y * -.5f;

			if (GolfTime > 0)
				SoundEngine.PlaySound(SoundID.Item126 with { Pitch = 1f, Volume = .5f }, Projectile.Center);
		}

		return false;
	}

	public override bool PreDraw(ref Color lightColor)
	{
		Texture2D texture = TextureAssets.Projectile[Type].Value;

		//Draw normally
		Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition + new Vector2(0, Projectile.gfxOffY), null, Projectile.GetAlpha(lightColor), Projectile.rotation, texture.Size() / 2, Projectile.scale, SpriteEffects.None, 0);

		for (int i = Projectile.oldPos.Length - 1; i >= 0; i--) //Draw trail
		{
			var position = Projectile.oldPos[i] + Projectile.Size / 2 - Main.screenPosition + new Vector2(0, Projectile.gfxOffY);
			Main.EntitySpriteDraw(texture, position, null, Projectile.GetAlpha(lightColor) * .5f * (1f - (float)i / Projectile.oldPos.Length), Projectile.rotation, texture.Size() / 2, Projectile.scale, SpriteEffects.None, 0);
		}

		for (int i = 0; i < 2; i++) //Draw eye
		{
			var color = Projectile.GetAlpha((i == 0) ? new Color(38, 102, 132) : new Color(125, 175, 201));
			int offsetX = i * -2;

			Main.EntitySpriteDraw(TextureAssets.MagicPixel.Value, Projectile.Center - Main.screenPosition + new Vector2(offsetX, Projectile.gfxOffY), new Rectangle(0, 0, 2, 2), color, 0, new Vector2(.5f), Projectile.scale, SpriteEffects.None, 0);
		}

		if (readyToGolf) //Draw golf outline
			Main.EntitySpriteDraw(Highlight.Value, Projectile.Center - Main.screenPosition + new Vector2(0, Projectile.gfxOffY), null, Projectile.GetAlpha(Color.White), Projectile.rotation, Highlight.Size() / 2, Projectile.scale, SpriteEffects.None, 0);

		return false;
	}
}