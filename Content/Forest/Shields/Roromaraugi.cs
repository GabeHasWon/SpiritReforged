using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.NPCCommon;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.PlayerCommon;
using SpiritReforged.Common.ProjectileCommon.Abstract;
using SpiritReforged.Common.Subclasses.Greatshields;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Jungle.Bamboo.Items;
using SpiritReforged.Content.Particles;
using System.IO;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ModLoader.IO;

namespace SpiritReforged.Content.Forest.Shields;

public class Roromaraugi : GreatshieldItem
{
	public sealed class SurfaceImpactNPC : GlobalNPC
	{
		public override bool InstancePerEntity => true;

		public bool canImpact;

		public override void AI(NPC npc)
		{
			if (canImpact)
			{
				if (Collision.SolidCollision(npc.position - new Vector2(2), npc.width + 4, npc.height + 4))
				{
					canImpact = false;
					float damage = Math.Min(npc.velocity.Y / 50f, 1) * 50;

					if (damage > 5 && npc.GetWereThereAnyInteractions() && Main.player[npc.lastInteraction] is Player player)
					{
						player.ApplyDamageToNPC(npc, (int)damage, 2, Math.Sign(npc.velocity.X), false, DamageClass.Melee, true);
						npc.velocity.Y = -7 * npc.knockBackResist; //Bounce up

						if (Main.netMode == NetmodeID.MultiplayerClient)
							new NPCVelocityPacketData((short)npc.whoAmI, npc.velocity).Send();
					}

					if (!Main.dedServ)
					{
						for (int i = 0; i < 3; i++)
						{
							Vector2 velocity = (Vector2.UnitY * -(Math.Clamp(npc.velocity.Y, 1.5f, 3) + Main.rand.NextFloat(-1f, 1f))).RotatedByRandom(1.5f);
							ParticleHandler.SpawnParticle(new CartoonHit(npc.Bottom, 20, 1, velocity.ToRotation() - MathHelper.PiOver2 - MathHelper.PiOver4, velocity));
						}

						SoundEngine.PlaySound(SoundID.DD2_MonkStaffGroundImpact with { Pitch = 0.5f }, npc.Center);
						Collision.TileCollision(npc.position, npc.velocity, npc.width, npc.height);
					}
				}
				else
				{
					npc.velocity.Y++;
				}
			}
		}

		public override void SendExtraAI(NPC npc, BitWriter bitWriter, BinaryWriter binaryWriter) => bitWriter.WriteBit(canImpact);

		public override void ReceiveExtraAI(NPC npc, BitReader bitReader, BinaryReader binaryReader) => canImpact = bitReader.ReadBit();
	}

	public class RoromaraugiSwing : SwungProjectile
	{
		public override string Texture => DrawHelpers.RequestLocal<Roromaraugi>("Roromaraugi_Held");

		public override float SwingTime => (_chargeTimeMax == 0 || !FullyCharged) ? base.SwingTime : base.SwingTime * 2f; //Check if _chargeTimeMax equals zero to avoid recursion

		public int ChargeTimeMax
		{
			get
			{
				Player owner = Main.player[Projectile.owner];
				return _chargeTimeMax = (_chargeTimeMax == 0) ? _chargeTimeMax = (int)(SwingTime * 1.5f * owner.GetTotalAttackSpeed(DamageClass.Melee)) : _chargeTimeMax;
			}
			set => _chargeTimeMax = value;
		}

		public bool FullyCharged => _chargeTime >= ChargeTimeMax;

		private int _chargeTimeMax;
		private int _chargeTime;
		private bool _released;

		public override IConfiguration SetConfiguration() => new BasicConfiguration(EaseFunction.EaseCircularOut, 54, 25);

		public override float GetRotation(out float armRotation, out Player.CompositeArmStretchAmount stretch)
		{
			float value = base.GetRotation(out armRotation, out stretch);
			return value + MathHelper.PiOver2 + Progress * SwingDirection;
		}

		public override void AI()
		{
			base.AI();
			Player owner = Main.player[Projectile.owner];

			if (owner.channel && !_released)
			{
				if (++_chargeTime == ChargeTimeMax && Main.myPlayer == Projectile.owner)
					SoundEngine.PlaySound(SoundID.MaxMana, Projectile.Center);

				Counter--;
				Projectile.velocity = owner.DirectionTo(PlayerMouseHandler.GetMouse(Projectile.owner));
			}
			else 
			{
				if (FullyCharged)
				{
					if (Progress > 0.8f)
						Projectile.scale = Math.Max(Projectile.scale - 0.1f, 0);
					else
						Projectile.scale = Math.Min(Projectile.scale + 0.1f, 1.5f);

					if (!Main.dedServ && !_released)
					{
						SoundEngine.PlaySound(KendoBladeLunge.BigSwing, Projectile.Center);
						Projectile.damage *= 2; //Double damage
					}
				}

				_released = true;
			}
		}

		public override bool? CanDamage() => _released ? base.CanDamage() : false;

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			int direction = Math.Sign(SwingArc);
			bool sendVelocity = false;

			if (direction == 1)
			{
				if (FullyCharged && !Collision.SolidCollision(target.position, target.width, target.height + 2) && target.TryGetGlobalNPC(out SurfaceImpactNPC impactNPC))
				{
					impactNPC.canImpact = true;
					target.netUpdate = true;

					sendVelocity = true;
				}
			}
			else
			{
				sendVelocity = true;
			}

			if (sendVelocity)
			{
				target.velocity.Y += Projectile.knockBack * 5 * direction * target.knockBackResist;

				if (Main.netMode == NetmodeID.MultiplayerClient)
					new NPCVelocityPacketData((short)target.whoAmI, target.velocity).Send(); //Sync NPC velocity from the multiplayer client
			}
		}

		public override bool PreDraw(ref Color lightColor)
		{
			Texture2D texture = TextureAssets.Projectile[Type].Value;
			SpriteEffects effects = (SwingDirection == -1) ? SpriteEffects.FlipHorizontally : default;
			Vector2 origin = new(texture.Width / 2, texture.Height - 6); //The handle

			Color smearColor = Projectile.GetAlpha(lightColor).MultiplyRGB(Color.PaleVioletRed) * Math.Min(Counter / SwingTime * 3, 1) * 0.5f;
			float smearRotation = Projectile.rotation - SwingArc * 0.5f * Projectile.spriteDirection + ((Projectile.spriteDirection == -1) ? MathHelper.Pi : 0);

			DrawHeld(lightColor, origin, Projectile.rotation, effects);

			if (!_released && FullyCharged) //Charge visual
				DrawHeld(Color.White.Additive() * EaseFunction.EaseSine.Ease((_chargeTime - ChargeTimeMax) / 30f) * 0.5f, origin, Projectile.rotation, effects);

			DrawSmear(smearColor, smearRotation, (SwingDirection == -1) ? SpriteEffects.FlipVertically : default);

			return false;
		}
	}

	public override DrawLayer Layer => DrawLayer.BackArm;

	public override ShieldInfo SetInfo()
	{
		Item.damage = 12;
		Item.rare = ItemRarityID.Green;
		Item.useTime = Item.useAnimation = 20;
		Item.knockBack = 3.8f;
		Item.channel = true;
		Item.shoot = ModContent.ProjectileType<RoromaraugiSwing>();

		return new ShieldInfo(30, 60);
	}

	public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
	{
		SwungProjectile.Spawn(position, velocity, type, damage, knockback, player, 3, source);
		return false;
	}

	public override void OnBlockDamage(Player player, Player.HurtInfo info)
	{
		if (player.whoAmI == Main.myPlayer)
		{
			int damage = player.GetWeaponDamage(Item);
			float knockback = player.GetWeaponKnockback(Item);

			SwungProjectile.Spawn(player.Center, Vector2.UnitX * player.direction, ModContent.ProjectileType<RoromaraugiSwing>(), damage, knockback, player, -3);
		}
	}

	public override void DrawShield(ref PlayerDrawSet drawInfo, bool guarding)
	{
		if (drawInfo.drawPlayer.ownedProjectileCounts[ModContent.ProjectileType<RoromaraugiSwing>()] == 0) //Don't draw while performing a swing
		{
			const int idle_frame = 0;
			const int jump_frame = 5;

			Player player = drawInfo.drawPlayer;
			Texture2D texture = HeldTexture;
			Color color = Lighting.GetColor(player.Center.ToTileCoordinates());
			SpriteEffects effects = drawInfo.playerEffect;
			Vector2 origin = new(texture.Width / 2, texture.Height - 30);

			Vector2 offhand = GetOffhand(player, out int frame) + new Vector2(9 * player.direction, 3);
			Vector2 halfSize = player.Size / 2;

			float rotation = drawInfo.rotation;
			if (guarding)
			{
				rotation = player.AngleTo(PlayerMouseHandler.GetMouse(player.whoAmI)) + (player.direction == -1 ? MathHelper.Pi : 0);
				player.bodyFrame.Y = 0;
			}
			else if (frame is jump_frame or idle_frame)
			{
				rotation -= 0.3f * player.direction;
			}

			Vector2 position = drawInfo.Position + halfSize + (offhand - halfSize).RotatedBy(rotation) - Main.screenPosition;
			if (guarding)
			{
				float scale = 1f + EaseFunction.EaseSine.Ease((float)Main.timeForVisualEffects / 30f) * 0.3f;
				drawInfo.DrawDataCache.Add(new(texture, position.Floor(), null, color * (1f - (scale - 1f) / 0.4f), rotation, origin, scale, effects, 0));
			}

			drawInfo.DrawDataCache.Add(new(texture, position.Floor(), null, color, rotation, origin, 1, effects, 0));
		}
	}
}