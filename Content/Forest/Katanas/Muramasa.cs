using SpiritReforged.Common;
using SpiritReforged.Common.Easing;
using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.ProjectileCommon.Abstract;
using SpiritReforged.Common.VerletChains;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Ocean.Items.Reefhunter.Particles;
using SpiritReforged.Content.Particles;
using SpiritReforged.Content.SaltFlats.NPCs;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ModLoader.IO;

namespace SpiritReforged.Content.Forest.Katanas;

public class Muramasa : GlobalItem, IDrawHeld
{
	public sealed class	MuramasaEnchantPlayer : ModPlayer
	{
		public bool enchanted;

		public override void SaveData(TagCompound tag) => tag[nameof(enchanted)] = enchanted;
		public override void LoadData(TagCompound tag) => enchanted = tag.GetBool(nameof(enchanted));
	}

	public sealed class WaterWave : ModProjectile
	{
		public const int TIME_LEFT_MAX = 100;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.DD2SquireSonicBoom;

		public override void SetDefaults()
		{
			Projectile.Size = new(40);
			Projectile.friendly = true;
			Projectile.ignoreWater = true;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
			Projectile.timeLeft = 40;
			Projectile.extraUpdates = 1;
			Projectile.DamageType = DamageClass.Melee;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 20;
		}

		public override void AI()
		{
			Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
			Projectile.velocity *= 0.95f;

			if (Projectile.timeLeft < 20)
				Projectile.Opacity -= 1f / 20;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) { }

		public override bool PreDraw(ref Color lightColor)
		{
			Texture2D texture = TextureAssets.Projectile[Type].Value;

			for (int i = 1; i <= 5; i++)
				Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition - Projectile.velocity * i, null, Projectile.GetAlpha(Color.CadetBlue.Additive(80)) * (1f - i / 5f), Projectile.rotation, texture.Size() / 2, Projectile.scale, 0);

			DrawHelpers.DrawOutline(default, default, default, default, (offset) =>
				Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition + offset * Projectile.scale, null, Projectile.GetAlpha(Color.Cyan.Additive()), Projectile.rotation, texture.Size() / 2, Projectile.scale, 0));

			Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, Projectile.GetAlpha(Color.White.Additive()), Projectile.rotation, texture.Size() / 2, Projectile.scale, 0);
			return false;
		}
	}

	public sealed class MuramasaSwing : SwungProjectile, IDrawPixelated
	{
		public enum State { Swing, EnchantedSwing, Enchant, RemoveEnchantment }

		public State UseState
		{
			get => (State)Projectile.ai[0];
			set => Projectile.ai[0] = (int)value;
		}

		public override string Texture => "Terraria/Images/Item_" + ItemID.Muramasa;

		public override LocalizedText DisplayName => Lang.GetItemName(ItemID.Muramasa);

		public override float SwingTime => (UseState == State.EnchantedSwing) ? base.SwingTime * 2 : base.SwingTime;

		public override IConfiguration SetConfiguration() => new BasicConfiguration(EaseFunction.EaseQuarticOut, 84, 25);

		public override void AI()
		{
			base.AI();

			if (Main.dedServ)
				return;

			if (UseState == State.Enchant)
			{
				if (Counter == 5)
				{
					for (int i = 0; i < 10; i++)
						Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Gold);

					SoundEngine.PlaySound(SoundID.Tink with { Pitch = -0.5f }, Projectile.Center);
					SoundEngine.PlaySound(SoundID.DD2_CrystalCartImpact with { Pitch = 0.5f }, Projectile.Center);
				}
			}
			else if (UseState == State.RemoveEnchantment)
			{
				if (Counter == 5)
					SoundEngine.PlaySound(SoundID.DD2_CrystalCartImpact with { Pitch = -0.5f }, Projectile.Center);
			}
			else if (UseState == State.EnchantedSwing)
			{
				if (Counter == 5)
					SoundEngine.PlaySound(Wisp.Death with { Pitch = 0.9f, Volume = 0.4f }, Projectile.Center);

				if (Progress < 0.5f && Main.rand.NextBool(3))
				{
					ParticleHandler.SpawnParticle(new EmberParticle(Vector2.Lerp(Projectile.Center, GetEndPosition(), Main.rand.NextFloat(0.2f, 1f)), Projectile.velocity, Color.Cyan, 0.5f, 15, 2));
					ParticleHandler.SpawnParticle(new CompositeSmoke(Vector2.Lerp(Projectile.Center, GetEndPosition(), Main.rand.NextFloat(0.2f, 1f)), Projectile.velocity, Color.Cyan, 20));
				}
			}
		}

		public override bool? CanDamage() => (UseState is State.Swing or State.EnchantedSwing) ? null : false;

		public override float GetRotation(out float armRotation, out Player.CompositeArmStretchAmount stretch)
		{
			float value = base.GetRotation(out armRotation, out stretch);
			if (UseState is State.Enchant or State.RemoveEnchantment)
			{
				return value - ((Projectile.direction == -1) ? MathHelper.PiOver2 : MathHelper.Pi);
			}
			else
			{
				return value + MathHelper.PiOver4 * SwingDirection;
			}
		}

		public override bool PreDraw(ref Color lightColor)
		{
			SpriteEffects effects = (SwingDirection == -1) ? SpriteEffects.FlipVertically : default;
			for (int i = 1; i <= 3; i++)
			{
				float rotation = Projectile.rotation - i * 0.3f * SwingDirection * Math.Max(1f - Progress * 2, 0);
				DrawHeld(lightColor * (1f - i / 3f) * 0.8f, new Vector2(0, (effects == SpriteEffects.FlipVertically) ? 0 : TextureAssets.Projectile[Type].Value.Height), rotation, effects);
			}

			if (UseState == State.EnchantedSwing)
			{
				DrawHelpers.DrawOutline(default, default, default, default, (offset) =>
					DrawHeld(Color.Cyan.Additive() * (1f - Progress), new Vector2(0, (effects == SpriteEffects.FlipVertically) ? 0 : TextureAssets.Projectile[Type].Value.Height) + offset, Projectile.rotation, effects));
			}

			DrawHeld(lightColor, new Vector2(0, (effects == SpriteEffects.FlipVertically) ? 0 : TextureAssets.Projectile[Type].Value.Height), Projectile.rotation, effects);
			return false;
		}

		void IDrawPixelated.DrawPixelated(SpriteBatch spriteBatch)
		{
			if (SwingArc != 0)
			{
				Player owner = Main.player[Projectile.owner];

				//Draw a custom smear
				Main.instance.LoadProjectile(985);
				Texture2D smear = TextureAssets.Projectile[985].Value;

				SpriteEffects effects = (SwingDirection == -1) ? SpriteEffects.FlipVertically : default;
				Rectangle source = smear.Frame(1, 4, 0, (int)(Progress * 14f));
				float rotation = Projectile.rotation + SwingDirection * Progress - MathHelper.PiOver4 * SwingDirection;

				Color lightColor = Lighting.GetColor(Projectile.Center.ToTileCoordinates());
				Vector2 origin = new(source.Width, source.Height / 2);
				Vector2 smearWorldPosition = owner.Center + (Vector2.UnitX * (GetConfig<BasicConfiguration>().Reach + 10)).RotatedBy(rotation);
				Vector2 smearDrawPosition = smearWorldPosition - Main.screenPosition;

				IDrawPixelated.PixelateDrawPosition(ref smearDrawPosition);

				if (UseState == State.EnchantedSwing)
				{
					DrawHelpers.DrawOutline(default, default, default, default, (offset) =>
						spriteBatch.Draw(smear, smearDrawPosition + offset * 0.45f, source, Projectile.GetAlpha(lightColor.MultiplyRGB(Color.DodgerBlue)).Additive(), rotation, origin, 0.45f, effects, 0));

					spriteBatch.Draw(smear, smearDrawPosition, source, Projectile.GetAlpha(lightColor.MultiplyRGB(Color.White)).Additive(), rotation, origin, 0.45f, effects, 0);
				}
				else
				{
					spriteBatch.Draw(smear, smearDrawPosition, source, Projectile.GetAlpha(lightColor.MultiplyRGB(Color.Blue)), rotation, origin, 0.45f, effects, 0);

					source = smear.Frame(1, 4, 0, (int)(Progress * 20f));
					spriteBatch.Draw(smear, smearDrawPosition, source, Projectile.GetAlpha(lightColor.MultiplyRGB(Color.DodgerBlue)), rotation, origin, 0.45f, effects, 0);
				}
			}
		}
	}

	public override bool InstancePerEntity => true;

	public static readonly Asset<Texture2D> HeldTexture = DrawHelpers.RequestLocal<Muramasa>("Muramasa_Held", false);
	private float _swingArc;
	private float _freeRotation;

	#region chain
	private static readonly Rectangle _chainSource = new(12, 44, 6, 12);
	private static readonly Rectangle _lockSource = new(0, 42, 10, 14);

	public Chain GetChain(Player player)
	{
		if (_chain == null)
		{
			int length = _chainSource.Height;
			return _chain = new Chain(length - 2, 5, player.Center, new ChainPhysics(0.9f, 0.5f, 3f));
		}
		else
		{
			return _chain;
		}
	}

	private Chain _chain;
	private Vector2 _endPosition;

	private void ApplyChainPhysics(Player player, Vector2 end) //Based on ChainObject
	{
		if (_endPosition == default)
			_endPosition = end;

		Chain chain = GetChain(player);
		Vector2 startPosition = end;
		Vector2 endPosition = _endPosition;

		endPosition += new Vector2(0, 3f); //Gravity
		endPosition -= ChainConstraint(startPosition, _endPosition, (int)(chain.Segments[0].Length * chain.Segments.Count));

		if (!endPosition.HasNaNs())
			_endPosition = endPosition;

		chain.Update(startPosition, endPosition);
	}

	private static Vector2 ChainConstraint(Vector2 start, Vector2 end, int totalLength) //Based on ChainObject
	{
		Vector2 delta = start - end;
		float distance = delta.Length();
		float finalDistance = totalLength - distance;

		if (finalDistance > 0) //Compact indefinitely
		{
			return Vector2.Zero;
		}

		float fraction = finalDistance / Math.Max(distance, 1) / 2;
		delta *= fraction;

		return delta;
	}
	#endregion

	public override bool AppliesToEntity(Item entity, bool lateInstantiation) => entity.type == ItemID.Muramasa;

	public override void SetStaticDefaults() => SpiritSets.IsSword[ItemID.Muramasa] = SpiritSets.IsKatana[ItemID.Muramasa] = true;

	public override void SetDefaults(Item entity)
	{
		int animationTime = entity.useAnimation;
		entity.DefaultToSpear(ModContent.ProjectileType<MuramasaSwing>(), 1, animationTime);
	}

	public override void HoldItem(Item item, Player player)
	{
		if (!player.ItemAnimationActive)
		{
			player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Quarter, -0.35f * player.direction);
			_freeRotation = MathHelper.Lerp(_freeRotation, player.velocity.X / 5f, 0.2f);
		}

		if (!Main.dedServ)
			ApplyChainPhysics(player, player.GetFrontHandPosition(0, player.compositeFrontArm.rotation));
	}

	public override bool AltFunctionUse(Item item, Player player) => true;

	public override bool? UseItem(Item item, Player player)
	{
		if (player.altFunctionUse == 2 && player.TryGetModPlayer(out MuramasaEnchantPlayer empowerPlayer))
			empowerPlayer.enchanted = !empowerPlayer.enchanted; //Toggle empowered status

		return null;
	}

	public override bool Shoot(Item item, Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
	{
		_swingArc = _swingArc switch
		{
			3f => 5f,
			5f => -5f,
			_ => 3f
		};

		float arc = _swingArc;
		bool empowered = player.TryGetModPlayer(out MuramasaEnchantPlayer empowerPlayer) && empowerPlayer.enchanted;
		MuramasaSwing.State useState = empowered ? MuramasaSwing.State.EnchantedSwing : MuramasaSwing.State.Swing;

		if (player.altFunctionUse == 2)
		{
			useState = empowered ? MuramasaSwing.State.Enchant : MuramasaSwing.State.RemoveEnchantment;
			arc = 0;
		}
		else if (empowered)
		{
			Projectile.NewProjectile(source, position, velocity * 15, ModContent.ProjectileType<WaterWave>(), damage, knockback, player.whoAmI);
		}

		SwungProjectile.Spawn(position, velocity, type, damage, knockback, player, arc, source, (int)useState);
		return false;
	}

	void IDrawHeld.DrawHeld(ref PlayerDrawSet drawinfo)
	{
		Vector2 lockOrigin = new(_lockSource.Width / 2, 0);
		bool empowered = drawinfo.drawPlayer.TryGetModPlayer(out MuramasaEnchantPlayer empowerPlayer) && empowerPlayer.enchanted;

		if (!drawinfo.drawPlayer.ItemAnimationActive)
		{
			Texture2D texture = HeldTexture.Value;
			IDrawHeld.DrawSwordHeld(ref drawinfo, texture, texture.Frame(2, 3, 0, empowered ? 1 : 0, -2, -2));
			IDrawHeld.DrawSwordHeld(ref drawinfo, texture, texture.Frame(2, 3, 1, empowered ? 1 : 0, -2, -2), Color.White.Additive() * EaseFunction.EaseSine.Ease((float)Main.timeForVisualEffects / 80f) * 0.4f); //Glowmask

			if (!empowered) //Draw lock
			{
				Vector2 bobOffset = Main.OffsetsPlayerHeadgear[drawinfo.drawPlayer.bodyFrame.Y / drawinfo.drawPlayer.bodyFrame.Height] * drawinfo.drawPlayer.gravDir;
				Vector2 center = drawinfo.drawPlayer.MountedCenter + bobOffset;
				Vector2 drawPos = new((int)(center.X + 20 * drawinfo.drawPlayer.direction - Main.screenPosition.X), (int)(center.Y + 4 * drawinfo.drawPlayer.gravDir - Main.screenPosition.Y + drawinfo.drawPlayer.gfxOffY));

				float rotation = _freeRotation;
				Color color = Lighting.GetColor((int)drawinfo.drawPlayer.Center.X / 16, (int)drawinfo.drawPlayer.Center.Y / 16);

				drawinfo.DrawDataCache.Add(new DrawData(texture, drawPos, _lockSource, color, rotation, lockOrigin, 1, drawinfo.playerEffect, 0));
			}
		}
		else if (!empowered)
		{
			Chain chain = GetChain(drawinfo.drawPlayer);
			chain.Draw(Main.spriteBatch, HeldTexture.Value, _chainSource);
			Color lightColor = Lighting.GetColor(chain.EndPosition.ToTileCoordinates());

			Main.spriteBatch.Draw(HeldTexture.Value, chain.EndPosition - Main.screenPosition, _lockSource, lightColor, chain.EndRotation + MathHelper.PiOver2, lockOrigin, 1, 0, 0);
		}
	}
}