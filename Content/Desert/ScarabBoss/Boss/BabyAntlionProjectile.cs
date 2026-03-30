using Microsoft.CodeAnalysis;
using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.ModCompat;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.PlayerCommon;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Particles;
using SpiritReforged.Content.Underground.Tiles;
using System.Security.Cryptography;
using Terraria.Audio;
using Terraria.GameContent.Drawing;
using Terraria.Graphics.CameraModifiers;
using static Terraria.GameContent.PlayerEyeHelper;

namespace SpiritReforged.Content.Desert.ScarabBoss.Boss;

public class BabyAntlionProjectile : ModProjectile
{
	private const int MAX_TIMELEFT = 460;

	public bool HasAScarabValid
	{
		get
		{
			if (Projectile.ai[0] < 0 || Projectile.ai[0] >= Main.maxNPCs)
				return false;
			NPC npc = Main.npc[(int)Projectile.ai[0]];
			if (!npc.active)
				return false;

			return npc.type == ModContent.NPCType<Scarabeus>();
		}
	}

	public int DrawStormlion
	{
		get => (int)Projectile.localAI[0];
		set => Projectile.localAI[0] = value;
	}

	public ref float HopHeight => ref Projectile.ai[1];

	public NPC Scarab => Main.npc[(int)Projectile.ai[0]];

	public enum AIState
	{
		Hidden,
		Emerging,
		ChasingScarab,
		Burnt,
		BurntHarmless,
		FlyOff
	}

	public AIState CurrentState
	{
		get => (AIState)Projectile.ai[2];
		set => Projectile.ai[2] = (int)value;
	}

	public Vector2 _originalPosition = Vector2.Zero;

	public override void SetStaticDefaults() => ProjectileID.Sets.DontAttachHideToAlpha[Type] = true;

	public static bool[] _desertScourgeSegmentTypes;

	public override void SetDefaults()
	{
		Projectile.Size = new(26, 26);
		Projectile.hostile = true;
		Projectile.tileCollide = false;
		Projectile.penetrate = -1;
		Projectile.timeLeft = MAX_TIMELEFT;
		Projectile.manualDirectionChange = true;
		Projectile.hide = true;

		Projectile.rotation = Main.rand.NextFloat(MathHelper.TwoPi);

		ProjectileID.Sets.TrailingMode[Type] = 0;
		ProjectileID.Sets.TrailCacheLength[Type] = 7;

		_desertScourgeSegmentTypes = NPCID.Sets.Factory.CreateBoolSet(false);
		if (CrossMod.Fables.Enabled)
		{
			DrawStormlion = Main.rand.NextBool(5) ? 1 : 0;
			if (DrawStormlion > 0 && Main.IsItStorming)
				DrawStormlion = 2 + Main.rand.Next(2);

			if (CrossMod.Fables.TryFind("DesertScourge", out ModNPC scourgeHead) &&
			CrossMod.Fables.TryFind("DesertScourgeHitbox", out ModNPC scourgeHitbox))
			{
				_desertScourgeSegmentTypes = NPCID.Sets.Factory.CreateBoolSet(scourgeHead.Type, scourgeHitbox.Type);
			}				
		}
	}

	public bool QuickFadeAway
	{
		get
		{
			if (!Main.expertMode) //|| (Scarab.ModNPC as Scarabeus).FightingDScourge)
				return true;
			return false;
		}
	}

	public override bool? CanDamage() => CurrentState != AIState.Hidden && Projectile.timeLeft > 30 ? null : false;

	public override void AI()
	{
		if (_originalPosition == Vector2.Zero)
			_originalPosition = Projectile.Center;

		if (CurrentState == AIState.Hidden)
		{
			LieInWait();
			return;
		}

		//Spin around and up into the air before starting to fly
		if (CurrentState == AIState.Emerging)
		{
			SpinEmerge();
			return;
		}

		//Falling down when burnt
		if (CurrentState is AIState.Burnt or AIState.BurntHarmless)
		{
			BurnOffAndFall();
			return;
		}

		//Fly away if there's no valid scarabeus anymore
		if (!HasAScarabValid && CurrentState != AIState.FlyOff)
		{
			CurrentState = AIState.FlyOff;
			Projectile.timeLeft = Math.Min(Projectile.timeLeft, 40);
		}

		//Update the frame for it to flap its wings
		if (++Projectile.frameCounter > 4)
		{
			Projectile.frameCounter = 0;
			Projectile.frame = (Projectile.frame + 1) % 8;
		}

		//Flying off
		if (CurrentState == AIState.FlyOff)
		{
			Projectile.velocity.Y = MathHelper.Lerp(Projectile.velocity.Y, -6f, 0.2f);
			Projectile.velocity.X *= 0.91f;
			Projectile.rotation = Projectile.velocity.X * 0.02f;
			Projectile.direction = Projectile.velocity.X < 0 ? 1 : -1;
			return;
		}

		Vector2 towardsScarab = (Scarab.Top - Vector2.UnitY * 48f - Projectile.Center);
		float distanceToScarab = towardsScarab.Length();

		if (distanceToScarab < 30f)
		{
			if (!Main.dedServ)
			{
				ParticleHandler.SpawnParticle(new FireSploshion(Projectile.Center, Main.rand.Next(15, 25)));

				for (int i = 0; i < 5; i++)
				{
					Dust.NewDustPerfect(Main.rand.NextVector2FromRectangle(Projectile.Hitbox), DustID.Torch, Vector2.Zero, 0, Scale: Main.rand.NextFloat(0.7f, 1f));

					if (Main.rand.NextBool(2))
					{
						var p = new EmberParticle(
							Projectile.Center + Main.rand.NextVector2Circular(20f, 20f),
							Main.rand.NextVector2Circular(3f, 3f),
							Color.Orange,
							Main.rand.NextFloat(0.2f, 0.5f),
							30
							);

						ParticleHandler.SpawnParticle(p);
					}
				}					
			}

			SoundEngine.PlaySound(new SoundStyle("SpiritReforged/Assets/SFX/Item/DragonFire" + Main.rand.Next(1, 4)) with { Volume = 0.05f }, Projectile.Center);
			SoundEngine.PlaySound(new SoundStyle("SpiritReforged/Assets/SFX/Projectile/ElectricZap") with { Volume = 0.45f, PitchVariance = 0.15f }, Projectile.Center);

			CurrentState = QuickFadeAway ? AIState.BurntHarmless : AIState.Burnt;
			Projectile.position.X = Scarab.Center.X;
			Projectile.velocity *= 0.1f;
			Projectile.velocity += Scarab.DirectionTo(Projectile.Center + new Vector2(0f, -16f)) * 3f;
			Projectile.timeLeft = 120;
			Projectile.netUpdate = true;

			Projectile.frame = Main.rand.Next(3);

			if (DrawStormlion == 2)
			{
				Projectile.frame = Main.rand.Next(6);
			}

			Projectile.rotation = Main.rand.NextFloat(MathHelper.TwoPi);

			if (CurrentState == AIState.BurntHarmless)
			{
				Projectile.friendly = false;
				Projectile.velocity = Main.rand.NextVector2Circular(5f, 5f);
			}

			return;
		}

		//Stop following scarab if its no longer burning and were too far
		if (Scarab.ai[0] != (int)Scarabeus.AIState.Swarm && distanceToScarab > 200)
		{
			CurrentState = AIState.FlyOff;
			Projectile.timeLeft = Math.Min(Projectile.timeLeft, 40);
		}

		towardsScarab.Normalize();
		towardsScarab = towardsScarab.RotatedBy(MathF.Sin(Projectile.timeLeft * 0.1f) * 0.16) * 13;

		float accelerationSpeed = Utils.GetLerpValue(400f, 100f, distanceToScarab, true);
		Projectile.velocity = Vector2.Lerp(Projectile.velocity, towardsScarab, 0.1f + accelerationSpeed * 0.2f);
		Projectile.rotation = Projectile.velocity.X * 0.02f;
	}

	public void LieInWait()
	{
		const int emerge_wait_time = 45;

		float progress = 1f - (Projectile.timeLeft - MAX_TIMELEFT + emerge_wait_time) / (float)emerge_wait_time;

		
		//Do dust and smoke on the floor
		if (!Main.dedServ)
		{
			if (Main.rand.NextBool(2))
			{
				Color[] palette = Scarabeus.GetTilePalette(Projectile.Center);

				ParticleHandler.SpawnParticle(new SmokeCloud(Projectile.Bottom, -Vector2.UnitY * Main.rand.NextFloat(1, 3) * MathHelper.Lerp(0.7f, 2f, progress), palette[0] * 0.7f, Main.rand.NextFloat(0.07f, 0.2f) * progress, EaseFunction.EaseQuadOut, Main.rand.Next(20, 40))
				{
					Pixellate = true,
					DissolveAmount = 1,
					SecondaryColor = palette[1] * 0.7f,
					TertiaryColor = palette[2] * 0.7f,
					PixelDivisor = 3,
					ColorLerpExponent = 0.25f,
					Layer = ParticleLayer.BelowSolid
				});
			}

			if (Main.rand.NextBool(5) && !Main.dedServ)
			{
				Vector2 dustPosition = Projectile.Center + Vector2.UnitY * 4f;
				Point tilePosition = dustPosition.ToTileCoordinates();
				int dustIndex = WorldGen.KillTile_MakeTileDust(tilePosition.X, tilePosition.Y, Framing.GetTileSafely(tilePosition));

				Dust dust = Main.dust[dustIndex];
				dust.position = dustPosition + Vector2.UnitX * Main.rand.NextFloat(-16f, 16f);
				dust.velocity.Y -= Main.rand.NextFloat(1.5f, 3f);
				dust.velocity.X *= 0.5f;
				dust.noLightEmittence = true;
				dust.scale = Main.rand.NextFloat(0.5f, 1.2f);
			}
		}

		//Just die if scarab dies lol
		if (!HasAScarabValid)
		{
			Projectile.Kill();
			return;
		}

		//Emerge outta the ground
		if (MAX_TIMELEFT - Projectile.timeLeft > emerge_wait_time)
		{
			SoundEngine.PlaySound(SoundID.DD2_MonkStaffSwing with { Volume = 0.4f }, _originalPosition);
			SoundEngine.PlaySound(SoundID.DD2_MonkStaffGroundImpact with { Volume = 0.2f }, _originalPosition);

			Projectile.direction = (Projectile.Center.X - Scarab.Center.X) < 0 ? 1 : -1;
			CurrentState = AIState.Emerging;
			Projectile.velocity.Y = -HopHeight;
			Projectile.netUpdate = true;
		}
	}

	public void SpinEmerge()
	{
		Projectile.rotation += Projectile.direction * 0.36f;
		Projectile.velocity.Y += 0.2f;

		if (Projectile.velocity.Y >= 0)
		{
			if (HasAScarabValid)
			{
				Projectile.direction = (Projectile.Center.X - Scarab.Center.X) < 0 ? 1 : -1;
				CurrentState = AIState.ChasingScarab;
			}
			else
				CurrentState = AIState.FlyOff;
		}
	}

	public void BurnOffAndFall()
	{
		Projectile.velocity.X *= 0.8f;
		Projectile.velocity.Y += 0.18f;
		if (Projectile.velocity.Y > 0)
			Projectile.velocity.Y *= 1.02f;

		Projectile.rotation += Projectile.velocity.Y * 0.01f;

		if (CurrentState == AIState.BurntHarmless)
		{
			Projectile.timeLeft--;
			Projectile.velocity *= 0.93f;
		}

		if (!Main.dedServ)
		{
			if(Main.rand.NextBool(3))
			{
				Vector2 position = Projectile.Center + Main.rand.NextVector2Circular(10, 10);
				Vector2 velocity = -Vector2.UnitY + Projectile.velocity;
				Color[] colors = [new Color(255, 200, 0, 100), new Color(255, 115, 0, 100), new Color(200, 3, 33, 100)];
				float scale = Main.rand.NextFloat(0.06f, 0.09f);
				int maxTime = (int)(Main.rand.Next(10, 35));

				ParticleHandler.SpawnParticle(new FireParticle(position, velocity, colors, 1.25f, scale, EaseFunction.EaseQuadOut, maxTime)
				{
					ColorLerpExponent = 2.5f
				});
			}

			if (Main.rand.NextBool(4))
			{
				var p = new EmberParticle(
					Projectile.Center + Main.rand.NextVector2Circular(20f, 20f),
					-Projectile.velocity.RotatedByRandom(0.5f) * Main.rand.NextFloat(0.3f),
					Color.Orange,
					Main.rand.NextFloat(0.2f, 0.5f),
					30
					);

				p.emitLight = false;

				ParticleHandler.SpawnParticle(p);
			}

			if (Main.rand.NextBool())
			{
				var p = new SmokeCloud(
					Projectile.Center + Main.rand.NextVector2Circular(5f, 5f),
					-Projectile.velocity.RotatedByRandom(0.25f) * Main.rand.NextFloat(0.2f),
					Color.Black * (1f - Projectile.timeLeft / 120f) * 0.5f,
					0.175f * EaseFunction.EaseCircularInOut.Ease(1f - Projectile.timeLeft / 120f),
					EaseFunction.EaseCircularOut,
					30);

				p.Pixellate = true;
				p.Layer = ParticleLayer.BelowProjectile;

				ParticleHandler.SpawnParticle(p);
			}

			//Get destroyed if overlapping with a scourge segment
			if (Scarab != null && Scarab.active && Scarab.ModNPC is Scarabeus scarabeus && scarabeus.FightingDScourge)
			{
				Rectangle projHitbox = Projectile.Hitbox;
				projHitbox.Inflate(5, 5);

				for (int i = 0; i < Main.maxNPCs; i++)
				{
					NPC n = Main.npc[i];
					if (n.active && _desertScourgeSegmentTypes[n.type] && projHitbox.Intersects(n.Hitbox))
					{
						scarabeus.DuoFightMicroBurnOnScourge();
						Projectile.Kill();
						return;
					}
				}
			}
		}
	}

	public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
	{
		if (CurrentState != AIState.Burnt && CurrentState != AIState.BurntHarmless && CurrentState != AIState.Emerging)
			return null;

		//Bigger hitbox on burnt and emerging ones to make the mmore punishing to players who stand still
		projHitbox.Inflate(14, 8);
		return projHitbox.Intersects(targetHitbox);
	}

	public override void ModifyHitPlayer(Player target, ref Player.HurtModifiers modifiers) => modifiers.Knockback *= 0f;

	public override void OnHitPlayer(Player target, Player.HurtInfo info)
	{
		if (CurrentState is AIState.Burnt or AIState.BurntHarmless)
			target.AddBuff(BuffID.OnFire, Scarabeus.STAT_ANTLION_ONFIRE_DURATION, false);
	}

	public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
	{
		if (Projectile.timeLeft > MAX_TIMELEFT - 90)
			behindNPCsAndTiles.Add(index);
		else if (CurrentState is not AIState.Burnt or AIState.BurntHarmless )
			overPlayers.Add(index);
		else
			behindNPCs.Add(index);
	}

	public override bool PreDraw(ref Color lightColor)
	{
		var bloom = AssetLoader.LoadedTextures["Bloom"].Value;

		if (CurrentState == AIState.Hidden)
			return false;

		Texture2D texture = TextureAssets.Projectile[Type].Value;
		Texture2D stormlionTexture = ModContent.Request<Texture2D>(Texture + "_Stormlion").Value;
		var solid = TextureColorCache.ColorSolid(texture, Color.White);
		Vector2 position = Projectile.Center;
		float rotation = Projectile.rotation;
		float scale = Projectile.scale;
		Rectangle frame = texture.Frame(8, 3, Projectile.frame, CurrentState is AIState.Burnt or AIState.BurntHarmless ? 2 : CurrentState == AIState.Emerging ? 0 : 1);
		SpriteEffects effects = Projectile.direction < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

		lightColor *= Math.Min(1, Projectile.timeLeft / 40f) * Utils.GetLerpValue(MAX_TIMELEFT, MAX_TIMELEFT - 30, Projectile.timeLeft, true);

		if (DrawStormlion > 0)
		{
			texture = stormlionTexture;
			solid = TextureColorCache.ColorSolid(stormlionTexture, Color.White);
			frame = stormlionTexture.Frame(8, 6, Projectile.frame, CurrentState is AIState.Burnt or AIState.BurntHarmless ? 2 : CurrentState == AIState.Emerging ? 0 : 1);

			if (DrawStormlion == 2)
			{
				frame = stormlionTexture.Frame(8, 6, Projectile.frame, CurrentState == AIState.Burnt ? 5 : CurrentState == AIState.Emerging ? 3 : 4);
				lightColor = Color.White;
			}
		}
		
		if (CurrentState is AIState.Emerging or AIState.Burnt or AIState.BurntHarmless)
		{
			Color color = new Color(255, 200, 0) * 0.2f;

			if (CurrentState is AIState.Burnt or AIState.BurntHarmless)
			{
				if (Projectile.timeLeft > 120)
				{
					color = Color.Lerp(new Color(180, 180, 180), color, (Projectile.timeLeft - 120) / 80f);
				}
				else
					color = new Color(180, 180, 180);
			}

			for (int i = 0; i < Projectile.oldPos.Length; i++)
			{
				Vector2 pos = Projectile.oldPos[i] + Projectile.Size / 2f;
				float lerp = 1f - i / (float)Projectile.oldPos.Length;

				Main.spriteBatch.Draw(texture, pos - Main.screenPosition, frame, color * 0.5f * lerp,
				  Projectile.rotation, frame.Size() / 2f, Projectile.scale, 0, 0f);
			}
		}	

		Main.EntitySpriteDraw(texture, position - Main.screenPosition, frame, lightColor, rotation, frame.Size() / 2f, scale, effects, 0);

		if (CurrentState != AIState.FlyOff)
		{
			if (CurrentState is AIState.Burnt or AIState.BurntHarmless)
			{
				float progress = 0f;

				if (Projectile.timeLeft > 80f)
					progress = (Projectile.timeLeft - 80f) / 40f;

				DrawBloom(bloom, solid, position, frame, rotation, scale, effects, progress);
			}
			else
			{
				float dist = Projectile.Distance(Scarab.Center);

				if (dist < 120f)
				{
					float lerp = 1f - (dist - 40f) / 80f;

					DrawBloom(bloom, solid, position, frame, rotation, scale, effects, lerp);
				}
			}		
		}

		return false;
	}

	internal void DrawBloom(Texture2D bloom, Texture2D solid, Vector2 position, Rectangle frame, float rotation, float scale, SpriteEffects effects, float progress)
	{
		Main.spriteBatch.Draw(bloom, position - Main.screenPosition, null, Color.Orange.Additive() * 0.25f * progress,
						0f, bloom.Size() / 2f, 0.45f, 0f, 0);

		Main.EntitySpriteDraw(solid, position - Main.screenPosition, frame, Color.Orange.Additive() * progress, rotation, frame.Size() / 2f, scale, effects, 0);

		Main.spriteBatch.Draw(bloom, position - Main.screenPosition, null, Color.Orange.Additive() * 0.5f * progress,
			0f, bloom.Size() / 2f, 0.25f, 0f, 0);
	}
}