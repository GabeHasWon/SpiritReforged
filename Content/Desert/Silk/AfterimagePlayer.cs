using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Content.Particles;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.Graphics;
using Terraria.Graphics.Renderers;

namespace SpiritReforged.Content.Desert.Silk;

public class AfterimagePlayer : ModPlayer
{
	public const int ManaThreshold = 30;

	public bool setActive = false;
	private int _manaCounter;

	#region drawing and visuals
	private const int TrailLength = 8;

	public Vector2 AfterPosition => _positionCache[29 - TrailLength];
	private readonly Vector2[] _positionCache = new Vector2[30];

	public override void Load() => On_LegacyPlayerRenderer.DrawPlayerFull += static (orig, self, camera, player) =>
	{
		if (player.GetModPlayer<AfterimagePlayer>().setActive)
			DrawAfterimage(Main.spriteBatch, self, camera, player, player.GetModPlayer<AfterimagePlayer>().AfterPosition);

		orig(self, camera, player);
	};

	private static void DrawAfterimage(SpriteBatch sb, LegacyPlayerRenderer self, Camera camera, Player drawPlayer, Vector2 position)
	{
		sb.Begin(SpriteSortMode.Immediate, BlendState.Additive, camera.Sampler, DepthStencilState.None, camera.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

		Lighting.AddLight(position, new Vector3(0.5f, 0.3f, 0.05f));

		for (int a = 0; a < TrailLength; a++)
			self.DrawPlayer(camera, drawPlayer, drawPlayer.GetModPlayer<AfterimagePlayer>()._positionCache[29 - a], drawPlayer.fullRotation, drawPlayer.fullRotationOrigin, 1f - a / (float)TrailLength, 1);

		for (int i = 0; i < 3; i++)
			self.DrawPlayer(camera, drawPlayer, position, drawPlayer.fullRotation, drawPlayer.fullRotationOrigin, 0, 1);

		var glow = TextureAssets.Extra[98].Value;
		var bloom = AssetLoader.LoadedTextures["Bloom"].Value;

		float wave = 0.5f + (float)Math.Sin(Main.timeForVisualEffects / 20f) * 0.5f;
		var scale = new Vector2(0.5f + wave * 0.4f, 1 - wave * 0.4f) * 1.2f;
		var glowPosition = position + new Vector2(drawPlayer.width / 2, 8) - Main.screenPosition;

		sb.Draw(bloom, glowPosition, null, Color.Orange, 0, bloom.Size() / 2, 0.3f, default, 0);

		sb.Draw(glow, glowPosition, null, Color.Orange * 0.5f, 0, glow.Size() / 2, scale * 1.3f, default, 0);
		sb.Draw(glow, glowPosition, null, Color.Goldenrod, 0, glow.Size() / 2, scale, default, 0);
		sb.Draw(glow, glowPosition, null, Color.White, 0, glow.Size() / 2, scale * 0.7f, default, 0);

		float angled = MathHelper.PiOver2;
		sb.Draw(glow, glowPosition, null, Color.Orange * 0.5f, angled, glow.Size() / 2, scale * 1.3f, default, 0);
		sb.Draw(glow, glowPosition, null, Color.Goldenrod, angled, glow.Size() / 2, scale, default, 0);
		sb.Draw(glow, glowPosition, null, Color.White, angled, glow.Size() / 2, scale * 0.7f, default, 0);

		sb.End();
	}

	public override void PostUpdateEquips()
	{
		if (setActive && Main.rand.NextBool(5) && Player.position != AfterPosition)
		{
			float strength = Main.rand.NextFloat();
			var position = Main.rand.NextVector2FromRectangle(new((int)AfterPosition.X, (int)AfterPosition.Y, Player.width, Player.height));
			var velocity = AfterPosition.DirectionTo(Player.position) * strength * 3;

			ParticleHandler.SpawnParticle(new EmberParticle(position, velocity, Color.Lerp(Color.OrangeRed, Color.Yellow, strength).Additive(), MathHelper.Lerp(0.5f, 2, strength), 20, 1) { emitLight = false });
		}

		for (int i = _positionCache.Length - 1; i > 0; i--)
			_positionCache[i] = _positionCache[i - 1];

		_positionCache[0] = Player.position;
	}
	#endregion

	public override void ResetEffects() => setActive = false;

	public override bool Shoot(Item item, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
	{
		if (setActive && _manaCounter >= ManaThreshold)
		{
			Vector2 newPosition = position + (AfterPosition - Player.position);
			Projectile.NewProjectile(source, newPosition, velocity, type, damage, knockback);

			SoundEngine.PlaySound(SoundID.DD2_DarkMageCastHeal with { Pitch = 0.8f }, AfterPosition);
			SoundEngine.PlaySound(new SoundStyle("SpiritReforged/Assets/SFX/Ambient/PositiveOutcome") with { Pitch = 0.5f, PitchVariance = 0.2f }, AfterPosition);
		}

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