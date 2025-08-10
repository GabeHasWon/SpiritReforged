using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Content.Particles;
using SpiritReforged.Content.Underground.Items.BigBombs;
using System.Runtime.CompilerServices;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.Graphics;
using Terraria.Graphics.Renderers;

namespace SpiritReforged.Content.Desert.Silk;

public class AfterimagePlayer : ModPlayer
{
	public const int ManaThreshold = 30;

	public static readonly SoundStyle Magic = new("SpiritReforged/Assets/SFX/Projectile/MagicJingle");

	[UnsafeAccessor(UnsafeAccessorKind.Method, Name = "ItemCheck_Shoot")]
	private static extern void ItemCheck_Shoot(Player player, int i, Item sItem, int weaponDamage);

	/// <summary> Whether a duplicate projectile was created on the local client. </summary>
	public static bool Duplicate { get; private set; }

	public bool setActive = false;
	private int _manaCounter;
	private byte _duplicateDelay;

	#region drawing and visuals
	private const int TrailLength = 8;

	public Vector2 ImagePosition => _positionCache[29 - TrailLength];
	private readonly Vector2[] _positionCache = new Vector2[30];
	private float _manaEase;

	public override void Load() => On_LegacyPlayerRenderer.DrawPlayerFull += static (orig, self, camera, player) =>
	{
		if (player.GetModPlayer<AfterimagePlayer>().setActive)
			DrawAfterimage(Main.spriteBatch, self, camera, player, player.GetModPlayer<AfterimagePlayer>().ImagePosition);

		orig(self, camera, player);
	};

	private static void DrawAfterimage(SpriteBatch sb, LegacyPlayerRenderer self, Camera camera, Player drawPlayer, Vector2 position)
	{
		sb.Begin(SpriteSortMode.Immediate, BlendState.Additive, camera.Sampler, DepthStencilState.None, camera.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

		var afterimager = drawPlayer.GetModPlayer<AfterimagePlayer>();
		float manaStrength = (afterimager._duplicateDelay > 0) ? 1 : (afterimager._manaEase / ManaThreshold);
		float mult = MathHelper.Clamp(drawPlayer.position.DistanceSQ(position) / 1000f, 0, 1) * manaStrength;

		Lighting.AddLight(position, new Vector3(0.5f, 0.3f, 0.05f) * mult);

		for (int a = 0; a < TrailLength; a++)
			self.DrawPlayer(camera, drawPlayer, afterimager._positionCache[29 - a], drawPlayer.fullRotation, drawPlayer.fullRotationOrigin, 1f - a / (float)TrailLength, 1);

		for (int i = 0; i < 3; i++)
			self.DrawPlayer(camera, drawPlayer, position, drawPlayer.fullRotation, drawPlayer.fullRotationOrigin, 1f - mult, 1);

		var glow = TextureAssets.Extra[98].Value;
		var bloom = AssetLoader.LoadedTextures["Bloom"].Value;

		float wave = 0.5f + (float)Math.Sin(Main.timeForVisualEffects / 20f) * 0.5f;
		var glowScale = new Vector2(0.5f + wave * 0.4f, 1 - wave * 0.4f) * 1.2f;
		var glowPosition = position + drawPlayer.Size / 2 - new Vector2(0, 12).RotatedBy(drawPlayer.fullRotation) - Main.screenPosition;

		sb.Draw(bloom, glowPosition, null, Color.Orange * mult, 0, bloom.Size() / 2, 0.3f, default, 0);

		sb.Draw(glow, glowPosition, null, Color.Orange * 0.5f * mult, 0, glow.Size() / 2, glowScale * 1.3f, default, 0);
		sb.Draw(glow, glowPosition, null, Color.Goldenrod * mult, 0, glow.Size() / 2, glowScale, default, 0);
		sb.Draw(glow, glowPosition, null, Color.White * mult, 0, glow.Size() / 2, glowScale * 0.7f, default, 0);

		float angled = MathHelper.PiOver2;
		sb.Draw(glow, glowPosition, null, Color.Orange * 0.5f * mult, angled, glow.Size() / 2, glowScale * 1.3f, default, 0);
		sb.Draw(glow, glowPosition, null, Color.Goldenrod * mult, angled, glow.Size() / 2, glowScale, default, 0);
		sb.Draw(glow, glowPosition, null, Color.White * mult, angled, glow.Size() / 2, glowScale * 0.7f, default, 0);

		sb.End();
	}

	public override void PostUpdateEquips()
	{
		if (setActive)
		{
			if (Main.rand.NextBool(5) && Player.position != ImagePosition)
			{
				float manaStrength = (_duplicateDelay > 0) ? 1 : ((float)_manaCounter / ManaThreshold);
				float strength = Main.rand.NextFloat();
				var position = Main.rand.NextVector2FromRectangle(new((int)ImagePosition.X, (int)ImagePosition.Y, Player.width, Player.height));
				var velocity = ImagePosition.DirectionTo(Player.position) * strength * 3;

				ParticleHandler.SpawnParticle(new EmberParticle(position, velocity, Color.Lerp(Color.OrangeRed, Color.Yellow, strength).Additive(), MathHelper.Lerp(0.5f, 2, strength) * manaStrength, 20, 1) { emitLight = false });
			}

			_manaEase = MathHelper.Lerp(_manaEase, _manaCounter, 0.1f);

			//Update old positions
			for (int i = _positionCache.Length - 1; i > 0; i--)
				_positionCache[i] = _positionCache[i - 1];

			_positionCache[0] = Player.position;
		}
	}
	#endregion

	public override void ResetEffects()
	{
		setActive = false;

		if (_duplicateDelay > 0 && --_duplicateDelay == 1)
			FireDuplicate(); //_duplicateDelay is only incremented locally
	}

	private void FireDuplicate()
	{
		SoundEngine.PlaySound(SoundID.DD2_DarkMageCastHeal with { Pitch = 0.8f }, ImagePosition);
		SoundEngine.PlaySound(SoundID.Item4 with { Volume = 0.2f, Pitch = 0.5f }, ImagePosition);
		SoundEngine.PlaySound(Magic with { Volume = 0.05f, Pitch = 0.3f, PitchVariance = 0.1f }, ImagePosition);

		var glowPos = ImagePosition + Player.Size / 2 - new Vector2(0, 12).RotatedBy(Player.fullRotation);
		var ease = Bomb.EffectEase;
		var stretch = Vector2.One;
		float angle = Main.rand.NextFloat(MathHelper.Pi);

		ParticleHandler.SpawnParticle(new TexturedPulseCircle(glowPos, Color.Goldenrod.Additive(), Color.OrangeRed.Additive(), 1f, 180, 20, "Smoke", stretch, ease)
		{
			Angle = angle
		});

		ParticleHandler.SpawnParticle(new TexturedPulseCircle(glowPos, Color.White.Additive(), Color.OrangeRed.Additive(), .5f, 180, 20, "Smoke", stretch, ease)
		{
			Angle = angle
		});

		Vector2 lineScale = new(0.8f, 2.5f);

		for (int i = 0; i < 8; i++)
		{
			Vector2 velocity = Vector2.UnitX.RotatedBy(i / 2 * MathHelper.PiOver2) * 2;
			Color color = ((i % 2 == 0) ? Color.Orange : Color.White).Additive();
			float scale = (i % 2 == 0) ? 1 : 0.7f;

			ParticleHandler.SpawnParticle(new ImpactLine(glowPos, velocity, color, lineScale * scale, 20));
		}

		for (int i = 0; i < 8; i++)
		{
			Vector2 velocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(1, 3);
			float scale = Main.rand.NextFloat(0.4f, 1);

			ParticleHandler.SpawnParticle(new GlowParticle(glowPos, velocity, Color.Goldenrod.Additive(), scale, 30, 3));
			ParticleHandler.SpawnParticle(new GlowParticle(glowPos, velocity, Color.White.Additive(), scale * 0.7f, 30, 3));
		}

		if (Player.whoAmI == Main.myPlayer)
		{
			Duplicate = true;
			Vector2 oldPosition = Player.position;
			Player.position = ImagePosition; //Briefly adjust the player position so that projectiles appear at the afterimage instead

			ItemCheck_Shoot(Player, Player.whoAmI, Player.HeldItem, Player.HeldItem.damage);

			Player.position = oldPosition;
			Duplicate = false;
		}
	}

	public override void ModifyShootStats(Item item, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
	{
		if (Duplicate)
			ProjectileEdits.ChangeStats(item, ref position, ref velocity, ref type, ref damage, ref knockback);
	}

	public override bool Shoot(Item item, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
	{
		if (setActive && !item.channel && item.DamageType.CountsAsClass(DamageClass.Magic) && _manaCounter == 0)
			_duplicateDelay = 20;

		return true;
	}

	public override void OnConsumeMana(Item item, int manaConsumed)
	{
		if (setActive)
		{
			if (_manaCounter >= ManaThreshold)
				_manaCounter = 0;

			_manaCounter += manaConsumed;
		}
	}

	public override void ModifyManaCost(Item item, ref float reduce, ref float mult)
	{
		if (_manaCounter >= ManaThreshold)
			reduce -= ManaThreshold;
	}
}