using SpiritReforged.Common.MathHelpers;
using SpiritReforged.Common.NPCCommon;
using SpiritReforged.Common.Particle;
using SpiritReforged.Content.Particles;
using Terraria.Audio;

namespace SpiritReforged.Content.Desert.NPCs.Robber;

public class LootBag : ModNPC
{
	public sealed class CoinParticle : Particle
	{
		public enum CoinType { Copper, Silver, Gold, Platinum }

		public override ParticleDrawType DrawType => ParticleDrawType.Custom;

		public readonly CoinType Coin;
		private bool _colliding;

		public CoinParticle(Vector2 position, Vector2 velocity, int maxTime, CoinType coin = default)
		{
			Position = position;
			Velocity = velocity;
			MaxTime = maxTime;
			Coin = coin;

			Color = Color.White;
			Scale = 1;
		}

		public override void Update()
		{
			const int hitboxSize = 8;

			Rectangle hitbox = new((int)Position.X - hitboxSize / 2, (int)Position.Y - hitboxSize / 2, hitboxSize, hitboxSize);
			_colliding = CollisionChecks.Tiles(hitbox, CollisionChecks.SolidOrPlatform);

			if (_colliding)
			{
				Rotation = MathHelper.PiOver2;
				Point roundedPosition = Position.ToTileCoordinates();

				if (CollisionChecks.Tiles(new(roundedPosition.X * 16, roundedPosition.Y * 16, hitboxSize, hitboxSize), CollisionChecks.SolidOrPlatform))
				{
					if (Velocity != Vector2.Zero)
					{
						SoundEngine.PlaySound(SoundID.CoinPickup with { PitchVariance = 0.5f }, Position);
					} //First time collision

					Position.Y = roundedPosition.Y * 16;
					Velocity = Vector2.Zero;
				}
			}
			else
			{
				Rotation += Velocity.X * 0.1f;
				Velocity.Y += 0.2f;
			}
		}

		public override void CustomDraw(SpriteBatch spriteBatch)
		{
			Texture2D texture = TextureAssets.Coin[(int)Coin].Value;
			int frame = _colliding ? 2 : ((int)(TimeActive / 4f) % 8);
			Rectangle source = texture.Frame(1, 8, 0, frame, 0, -2);
			float opacity = Math.Clamp(1f - (TimeActive - (MaxTime - 30)) / 30f, 0, 1);

			spriteBatch.Draw(texture, Position - Main.screenPosition, source, Lighting.GetColor(Position.ToTileCoordinates()).MultiplyRGB(Color) * opacity, Rotation, source.Size() / 2, Scale, default, 0);
		}
	}

	public override void SetStaticDefaults()
	{
		NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, new NPCID.Sets.NPCBestiaryDrawModifiers() { Hide = true });
		Main.npcFrameCount[Type] = 3;
	}

	public override void SetDefaults()
	{
		NPC.Size = new(24);
		NPC.lifeMax = 50;
		NPC.value = 500;
		NPC.HitSound = SoundID.NPCHit1;
		NPC.DeathSound = SoundID.NPCDeath26;
		NPC.knockBackResist = 0.5f;
		AIType = -1;
	}

	public override void AI()
	{
		if (NPC.collideX)
			NPC.velocity.X = -NPC.velocity.X;

		if (NPC.collideY)
		{
			if (NPC.velocity.Y != 0)
			{
				Vector2 velocity = Vector2.UnitY * (Main.rand.NextFloat() * -NPC.velocity.Y);

				for (int i = 0; i < 10; i++)
				{
					var dust = Dust.NewDustDirect(NPC.BottomLeft, NPC.width, 2, DustID.Smoke, velocity.X * 0.5f, velocity.Y * 0.5f, 100, default, Main.rand.NextFloat(1, 3));
					dust.noGravity = true;
				}

				for (int i = 0; i < 3; i++)
				{
					Vector2 position = Main.rand.NextVector2FromRectangle(NPC.Hitbox);
					SmokeCloud smoke = new(position, velocity, Color.White * 0.8f, 0.1f, Common.Easing.EaseFunction.EaseCircularOut, 60)
					{
						Pixellate = true,
						PixelDivisor = 5,
						TertiaryColor = Color.SandyBrown
					};

					ParticleHandler.SpawnParticle(smoke);
				}

				SoundEngine.PlaySound(SoundID.NPCHit1 with { PitchVariance = 0.2f }, NPC.Center);
			} //Just collided

			NPC.velocity = Vector2.Zero;
			NPC.rotation = 0;
		}
		else
		{
			NPC.velocity.Y += 0.1f;
			NPC.rotation -= 0.5f;

			if (Main.rand.NextBool(20))
				ParticleHandler.SpawnParticle(new CoinParticle(Main.rand.NextVector2FromRectangle(NPC.Hitbox), NPC.velocity * Main.rand.NextFloat(0.5f), 200, (CoinParticle.CoinType)Main.rand.Next(3)));

			if (Main.rand.NextBool(8))
				Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.CopperCoin).velocity = NPC.velocity * Main.rand.NextFloat(0.5f);
		}
	}

	public override void HitEffect(NPC.HitInfo hit)
	{
		if (!Main.dedServ)
		{
			bool dead = NPC.life <= 0;

			for (int i = 0; i < (dead ? 5 : 3); i++)
				Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.CopperCoin, Scale: Main.rand.NextFloat(0.5f, 1.2f)).velocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(2f);

			for (int i = 0; i < (dead ? 7 : 3); i++)
			{
				float intensity = hit.Knockback + 5;
				Vector2 velocity = (new Vector2(hit.HitDirection * intensity, -intensity) * Main.rand.NextFloat(0.5f, 1)).RotateRandom(1f);
				ParticleHandler.SpawnParticle(new CoinParticle(Main.rand.NextVector2FromRectangle(NPC.Hitbox), velocity, 200, (CoinParticle.CoinType)Main.rand.Next(3)));
			}

			if (dead)
			{
				for (int i = 0; i < 3; i++)
					Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("Graverobber5").Type, 1f);
			}
		}

		NPC.velocity.Y -= 2;
		NPC.collideY = false;

		NPC.frameCounter = 1;
	}

	public override void FindFrame(int frameHeight)
	{
		const float frameRate = 0.2f;

		if (NPC.velocity.Y != 0)
			NPC.frameCounter = 0;
		else
			NPC.frameCounter += frameRate;

		NPC.frame.Y = (int)Math.Min(Main.npcFrameCount[Type] - 1, NPC.frameCounter) * frameHeight;
	}

	public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
	{
		Texture2D texture = TextureAssets.Npc[Type].Value;

		Rectangle source = NPC.frame with { Height = NPC.frame.Height - 2 }; //Remove padding
		Vector2 position = NPC.Center - screenPos + new Vector2(0, NPC.gfxOffY - (source.Height - NPC.height) / 2 + 2);
		SpriteEffects effects = (NPC.spriteDirection == 1) ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

		Main.EntitySpriteDraw(texture, position, source, NPC.DrawColor(drawColor), NPC.rotation, source.Size() / 2, NPC.scale, effects);
		return false;
	}

	public override void ModifyNPCLoot(NPCLoot npcLoot)
	{
		npcLoot.AddCommon(ItemID.Amethyst, 2, 1, 3);
		npcLoot.AddCommon(ItemID.Topaz, 2, 1, 3);
		npcLoot.AddCommon(ItemID.Sapphire, 3, 1, 3);
		npcLoot.AddCommon(ItemID.Emerald, 3, 1, 2);
		npcLoot.AddCommon(ItemID.Ruby, 3, 1, 2);
		npcLoot.AddCommon(ItemID.Amber, 4, 1, 2);
		npcLoot.AddCommon(ItemID.Diamond, 4, 1, 2);
	}
}