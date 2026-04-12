using SpiritReforged.Common;
using SpiritReforged.Common.Easing;
using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.ModCompat;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.ProjectileCommon;
using SpiritReforged.Common.ProjectileCommon.Abstract;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Dusts;
using SpiritReforged.Content.Ocean.Items.Blunderbuss;
using SpiritReforged.Content.Particles;
using SpiritReforged.Content.Savanna.Items.HuntingRifle;
using Terraria.Audio;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Forest.Rapiers;

public sealed class Reiterpallasch : ModItem, IDrawHeld
{
	public class ReiterpallaschSwing : RapierProjectile
	{
		public enum MoveType { Basic, Fire, Flourish }

		public static readonly SoundStyle Click = new("SpiritReforged/Assets/SFX/Item/ClickClack")
		{
			PitchVariance = 0.25f
		};

		public MoveType Move { get => (MoveType)Projectile.ai[0]; set => Projectile.ai[0] = (int)value; }

		public override float SwingTime => (Move is MoveType.Flourish or MoveType.Fire) ? (base.SwingTime * 2) : base.SwingTime;

		public override string Texture => ModContent.GetInstance<Reiterpallasch>().Texture;
		public override LocalizedText DisplayName => ModContent.GetInstance<Reiterpallasch>().DisplayName;

		private float _holdDistance;

		public override IConfiguration SetConfiguration() => new RapierConfiguration(EaseFunction.EaseQuadOut, 90, 12, 12, 0);

		public override void AI()
		{
			base.AI();

			if (Move == MoveType.Flourish)
			{
				if (Counter == SwingTime / 6)
				{
					_holdDistance = -20;
					SoundEngine.PlaySound(HuntingRifleProj.Ring, Projectile.Center);
				}

				if (Counter == SwingTime / 2)
				{
					for (int i = 0; i < 8; i++)
					{
						var dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Smoke, Alpha: 100, Scale: Main.rand.NextFloat(1, 1.5f));
						dust.velocity = Vector2.UnitY * -Main.rand.NextFloat(1, 5);
						dust.noGravity = true;

						if (Main.rand.NextBool())
							Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Torch, Scale: 2).noGravity = true;
					}

					_holdDistance = -15;

					SoundEngine.PlaySound(SoundID.Mech, Projectile.Center);
					SoundEngine.PlaySound(Click, Projectile.Center);
				}

				_holdDistance = Math.Min(_holdDistance + 1, -10);
			}
			else if (Move == MoveType.Fire)
			{
				_holdDistance = Math.Min(_holdDistance + 4, -12);

				if (SwingArc != 0 && Progress < 0.4f)
					_holdDistance = -30;

				if (Counter == ((SwingArc == 0) ? 1 : SwingTime / 2))
				{
					TrickFire();
					_holdDistance = -30;
				}
			}
			else
			{
				_holdDistance = (SwingArc == 0) ? Math.Max(30 * (0.6f - Progress * 2), -6) : Progress * -10;

				if (!Main.dedServ && Counter == 1)
				{
					Vector2 position = Projectile.Center + Projectile.velocity * 28;
					ParticleHandler.SpawnParticle((BasicNoiseCone)new BasicNoiseCone(position, Projectile.velocity, 20, new(90, 150)).SetColors(Color.White.Additive(100), Color.Red).SetIntensity(1.5f).AttachTo(Projectile));
				}
			}
		}

		private void TrickFire()
		{
			if (Main.player[Projectile.owner].HeldItem.ModItem is Reiterpallasch reiterpallasch)
				reiterpallasch.shells = 0;

			var position = Vector2.Lerp(Projectile.Center, GetEndPosition(), 0.4f);

			if (!Main.dedServ)
			{
				for (int i = 0; i < 10; i++)
					Dust.NewDustPerfect(position + Projectile.velocity + Main.rand.NextVector2Unit() * Main.rand.NextFloat(12f),
						DustID.Torch, Projectile.velocity * Main.rand.NextFloat(), 0, default, Main.rand.NextFloat(2f)).noGravity = true;

				for (int i = 0; i < 4; i++)
					Dust.NewDustPerfect(position + Projectile.velocity + Main.rand.NextVector2Unit() * Main.rand.NextFloat(10f),
						DustID.Smoke, Projectile.velocity.RotatedByRandom(1f) * Main.rand.NextFloat(), 240, default, Main.rand.NextFloat(1f, 2f));

				for (int i = 0; i < 3; i++)
					Dust.NewDustPerfect(Projectile.Center, DustID.Smoke, -Vector2.UnitY.RotatedByRandom(1f) * Main.rand.NextFloat(), 100, default, Main.rand.NextFloat(1f, 2f));

				SoundEngine.PlaySound(Main.rand.NextFromList(Blunderbuss.Fire, Blunderbuss.Fire2), Projectile.Center);
				SoundEngine.PlaySound(HuntingRifleProj.Ring with { Volume = 1, Pitch = 0.1f }, Projectile.Center);

				Dust.NewDustPerfect(Projectile.Center, ModContent.DustType<ShellDust>(), Projectile.velocity * -2 - Vector2.UnitY * 10).scale = 1;
			}

			if (Projectile.owner == Main.myPlayer)
			{
				for (int i = 0; i < 5; i++)
				{
					PreNewProjectile.New(Projectile.GetSource_FromAI(), position, (Projectile.velocity * Main.rand.NextFloat(5f, 10f)).RotatedByRandom(0.2f), ProjectileID.Bullet, Projectile.damage, Projectile.knockBack, Projectile.owner, preSpawnAction: (Projectile projectile) =>
					{
						if (projectile.TryGetGlobalProjectile(out BlunderbussProjectile bProj))
						{
							bProj.firedFromBlunderbuss = true;
							projectile.scale = Main.rand.NextFloat(0.25f, 1f);
							projectile.timeLeft = BlunderbussProjectile.timeLeftMax; //Shorten lifespan
						}
					});
				}
			}
		}

		public override float GetRotation(out float armRotation, out Player.CompositeArmStretchAmount stretch)
		{
			if (Move == MoveType.Flourish)
			{
				float progress = Math.Min(Counter / 20f, 1);
				float easeProgress = EaseFunction.EaseQuinticInOut.Ease(progress);
				float result = MathHelper.Lerp(Projectile.direction * -0.5f, -Projectile.direction, EaseFunction.EaseSine.Ease(progress));
				float value = Utils.AngleLerp(GetAbsoluteAngle() + MathHelper.PiOver2, result, easeProgress) - MathHelper.PiOver4;

				armRotation = value - ((Projectile.direction == -1) ? MathHelper.Pi : MathHelper.PiOver2);
				stretch = Player.CompositeArmStretchAmount.Full;

				return value;
			}
			else
			{
				float value = GetAbsoluteAngle();
				armRotation = value - 1.57f;
				stretch = ProgressiveStretch();

				if (Move == MoveType.Fire)
					value -= EaseFunction.EaseSine.Ease(Math.Min(Progress * 5, 1)) * 0.2f * Projectile.direction;

				return value + MathHelper.PiOver4 + SwingArc / 2 * Projectile.direction; //Correct rotation
			}
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (hitSweetSpot)
			{
				for (int i = 0; i < 5; i++)
				{
					float magnitude = Main.rand.NextFloat();
					ParticleHandler.SpawnParticle(new EmberParticle(GetEndPosition(), Projectile.velocity.RotatedByRandom(0.5f) * magnitude * -5f, Color.Silver, 0.4f * (1f - magnitude), 30, 3));
				}

				ParticleHandler.SpawnParticle(new RoarRing(GetEndPosition(), 1, 100, 14, EaseFunction.EaseCircularOut)
				{
					Opacity = 2f,
					TextureStretch = new(Main.rand.NextFloat(1, 4), 0.065f),
					Color = Color.White,
					UseLightColor = false
				}.WithSkew(0.75f, Projectile.velocity.ToRotation()));

				if (Main.player[Projectile.owner].HeldItem.ModItem is Reiterpallasch reiterpallasch && reiterpallasch.shells < MaxShells)
				{
					if (++reiterpallasch.shells >= MaxShells)
					{
						Move = MoveType.Flourish;
						Counter = 0;

						Projectile.netUpdate = true;
					}
				}
			}
		}

		public override bool PreDraw(ref Color lightColor)
		{
			float offset = _holdDistance;

			if (Move == MoveType.Flourish && Progress > 0.5f)
			{
				float opacity = 1f - (Progress - 0.5f) / 0.2f;
				DrawHelpers.DrawOutline(Main.spriteBatch, default, default, default, (outlineOffset) =>
				{
					Color color = Color.Red.Additive() * opacity;
					DrawHeld(color, new Vector2(0, TextureAssets.Projectile[Type].Value.Height) + new Vector2(-offset, offset) + outlineOffset, Projectile.rotation);
				});

				Texture2D glow = TextureAssets.Projectile[927].Value;
				Main.EntitySpriteDraw(glow, Projectile.Center - Main.screenPosition, null, Color.Red.Additive() * opacity, 0, glow.Size() / 2, new Vector2(3 - opacity, 0.75f * opacity), default, 0);
				Main.EntitySpriteDraw(glow, Projectile.Center - Main.screenPosition, null, Color.White.Additive() * opacity, 0, glow.Size() / 2, new Vector2(2.75f - opacity, 0.5f * opacity), default, 0);
			}

			DrawHeld(lightColor, new Vector2(0, TextureAssets.Projectile[Type].Value.Height) + new Vector2(-offset, offset), Projectile.rotation);

			if (Move == MoveType.Basic && 1f - Counter / 5f is float mult && mult > 0)
				DrawStar(lightColor, 0.8f, mult);

			return false;
		}

		public override bool? CanDamage() => (Move == MoveType.Basic && Counter <= 5) ? null : false;
	}

	private static readonly Asset<Texture2D> ShellTexture = DrawHelpers.RequestLocal<Reiterpallasch>("ReiterpallaschShell", false);

	public const int MaxShells = 3;
	public int shells;

	private float _swingArc;

	public override void Load() => On_PlayerDrawLayers.DrawPlayer_27_HeldItem += DrawHeldItem;

	private static void DrawHeldItem(On_PlayerDrawLayers.orig_DrawPlayer_27_HeldItem orig, ref PlayerDrawSet drawinfo)
	{
		int heldType = drawinfo.drawPlayer.HeldItem.type;

		if (!drawinfo.drawPlayer.ItemAnimationActive && drawinfo.drawPlayer.HeldItem.ModItem is Reiterpallasch reiterpallasch && reiterpallasch.shells >= MaxShells)
		{
			Texture2D texture = TextureAssets.Item[heldType].Value;
			Rectangle source = texture.Frame();
			Vector2 origin = new((drawinfo.itemEffect == SpriteEffects.FlipHorizontally) ? source.Width - 18 : 18, 40);

			bool animating = drawinfo.drawPlayer.bodyFrameCounter != 0;
			float sine = (float)Math.Sin(drawinfo.drawPlayer.bodyFrameCounter / 10f);
			float rotation = -(animating ? MathHelper.PiOver2 : 1.2f) * drawinfo.drawPlayer.direction;

			Vector2 location = (drawinfo.drawPlayer.RotatedRelativePoint(animating ? drawinfo.drawPlayer.Center : drawinfo.drawPlayer.Center - new Vector2(4 * drawinfo.drawPlayer.direction, 0)) + new Vector2((int)(sine * 2) * 2, 0)).Floor();
			Color color = drawinfo.drawPlayer.HeldItem.GetAlpha(Lighting.GetColor(location.ToTileCoordinates()));

			drawinfo.DrawDataCache.Add(new DrawData(texture, location - Main.screenPosition, source, color, rotation, origin, 1, drawinfo.itemEffect));
			return; //Skips orig
		}

		orig(ref drawinfo);
	}

	public override void SetStaticDefaults() => SpiritSets.IsSword[Type] = true;

	public override void SetDefaults()
	{
		Item.DefaultToSpear(ModContent.ProjectileType<ReiterpallaschSwing>(), 1f, 22);
		Item.SetShopValues(ItemRarityColor.Blue1, Item.sellPrice(gold: 1, silver: 25));
		Item.damage = 28;
		Item.knockBack = 2;
		Item.UseSound = RapierProjectile.DefaultSwing;
		Item.autoReuse = true;
		MoRHelper.SetSlashBonus(Item);
	}

	public override bool AltFunctionUse(Player player) => shells >= MaxShells;

	public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
	{
		ReiterpallaschSwing.MoveType moveType = ReiterpallaschSwing.MoveType.Basic;
		float swingArc = _swingArc;

		if (player.altFunctionUse == 2)
		{
			moveType = ReiterpallaschSwing.MoveType.Fire;
			swingArc = 0;
			damage *= 3;
		}

		SwungProjectile.Spawn(position, velocity, type, damage, knockback, player, swingArc, source, (int)moveType);

		_swingArc = _swingArc switch
		{
			0 => -0.1f,
			-0.1f => 0.2f,
			_ => 0
		};

		return false;
	}

	public override void PostDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
	{
		const float drawScale = 0.8f;

		if (Main.LocalPlayer.HeldItem != Item)
			return;

		for (int i = 0; i < shells; i++)
		{
			DrawSingleShell(spriteBatch, position + new Vector2((i - MaxShells / 2) * 20 * drawScale, 24));
		}

		static void DrawSingleShell(SpriteBatch spriteBatch, Vector2 position)
		{
			float positional = position.X + (float)Main.timeForVisualEffects;
			position += new Vector2(0, (float)Math.Sin(positional / 30f) * 3);

			Texture2D texture = ShellTexture.Value;

			DrawHelpers.DrawOutline(default, default, default, default, (offset) =>
			{
				Color outlineColor = Main.playerInventory ? Color.DodgerBlue : Color.Yellow;
				spriteBatch.Draw(TextureColorCache.ColorSolid(texture, Color.White), position.Floor() + offset, null, outlineColor, (float)Math.Sin(positional / 20f) * 0.1f, texture.Size() / 2, drawScale, 0, 0);
			});

			spriteBatch.Draw(texture, position.Floor(), null, Color.White, (float)Math.Sin(positional / 20f) * 0.3f, texture.Size() / 2, drawScale, 0, 0);
		}
	}

	public void DrawHeld(ref PlayerDrawSet drawinfo)
	{
		if (!drawinfo.drawPlayer.ItemAnimationActive && drawinfo.heldItem.ModItem is Reiterpallasch reiterpallasch && reiterpallasch.shells >= MaxShells)
		{
			Texture2D texture = TextureAssets.Item[drawinfo.heldItem.type].Value;
			Rectangle source = texture.Frame();
			Vector2 origin = new((drawinfo.itemEffect == SpriteEffects.FlipHorizontally) ? source.Width - 18 : 18, 40);

			bool animating = drawinfo.drawPlayer.bodyFrameCounter != 0;
			float sine = (float)Math.Sin(drawinfo.drawPlayer.bodyFrameCounter / 10f);
			float rotation = -(animating ? MathHelper.PiOver2 : 1.2f) * drawinfo.drawPlayer.direction;

			Vector2 location = (drawinfo.drawPlayer.RotatedRelativePoint(animating ? drawinfo.drawPlayer.Center : drawinfo.drawPlayer.Center - new Vector2(4 * drawinfo.drawPlayer.direction, 0)) + new Vector2((int)(sine * 2) * 2, 0)).Floor();
			Color color = drawinfo.drawPlayer.HeldItem.GetAlpha(Lighting.GetColor(location.ToTileCoordinates()));

			drawinfo.DrawDataCache.Add(new DrawData(texture, location - Main.screenPosition, source, color, rotation, origin, 1, drawinfo.itemEffect));
		}
	}

	public override void ModifyTooltips(List<TooltipLine> tooltips) => tooltips.RemoveAll(static x => x.Mod == "Terraria" && x.Name == "CritChance"); //Remove the line indicating crit chance

	public override void AddRecipes()
	{
		CreateRecipe().AddRecipeGroup("EarlyRapiers").AddIngredient(ItemID.ShadowScale, 5).AddTile(TileID.Anvils).Register();
		CreateRecipe().AddRecipeGroup("EarlyRapiers").AddIngredient(ItemID.TissueSample, 5).AddTile(TileID.Anvils).Register();
	}
}