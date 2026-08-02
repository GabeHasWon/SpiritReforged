using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Glyphs.Shock;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.Shaders;

namespace SpiritReforged.Content.Forest.Katanas.LightningSword;

public class VajraLightning : ModProjectile, IDrawPixelated
{
	public int NPCWhoAmI
	{
		get => (int)Projectile.ai[0];
		set => Projectile.ai[0] = value;
	}

	private LightningChain _chain, _reverseChain;

	public override LocalizedText DisplayName => ModContent.GetInstance<Vajra>().DisplayName;

	public override string Texture => AssetLoader.EmptyTexture;

	public override void SetDefaults()
	{
		Projectile.tileCollide = false;
		Projectile.friendly = true;
		Projectile.penetrate = -1;
		Projectile.usesLocalNPCImmunity = true;
		Projectile.localNPCHitCooldown = -1;
		Projectile.timeLeft = 20;
	}

	public override void AI()
	{
		if (Projectile.localAI[0] == 0)
		{
			Projectile.localAI[0] = 1;
			Projectile.rotation = Main.rand.NextFloat(-1f, 1f);

			Vector2 start = Projectile.Center;

			if (Main.npc[NPCWhoAmI] is NPC npc)
				Projectile.Center = npc.Center; //Snap to position

			if (!Main.dedServ)
			{
				_chain = new(start, Projectile.Center, Color.Goldenrod.Additive(), 60);
				_reverseChain = new(Projectile.Center, start, Color.DarkGoldenrod.Additive(), 60);
			}

			SoundEngine.PlaySound(ShockGlyph.ElectricZap with { Pitch = 0.7f }, Projectile.Center);
			SoundEngine.PlaySound(SoundID.DD2_LightningBugZap, Projectile.Center);

			for (int i = 0; i < 8; i++)
			{
				Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.GoldCoin, Main.rand.NextVector2Circular(4, 4) * Main.rand.NextFloat(), 0, Color.Yellow.Additive(), 1);
				dust.fadeIn = 1.2f;
				dust.noGravity = true;
			}
		}

		if (!Main.dedServ)
		{
			_chain.Update();
			_reverseChain.Update();
		}
	}

	public override bool? CanHitNPC(NPC target) => target.whoAmI == NPCWhoAmI;

	public override bool PreDraw(ref Color lightColor) => false;

	void IDrawPixelated.DrawPixelated(SpriteBatch spriteBatch)
	{
		_chain?.Draw(spriteBatch, Matrix.Identity);
		_reverseChain.Draw(spriteBatch, Matrix.Identity);

		Texture2D bloom = AssetLoader.LoadedTextures["Bloom"].Value;
		Vector2 position = Projectile.Center - Main.screenPosition;

		float progress = Projectile.timeLeft / 20f;
		Vector2 scale = new Vector2(Math.Max(1, (progress - 0.8f) * 20), 1) * Projectile.scale;
		float opacity = progress * 1.2f;

		IDrawPixelated.PixelateDrawPosition(ref position);

		spriteBatch.Draw(bloom, position, null, Color.Goldenrod.Additive() * opacity, Projectile.rotation, bloom.Size() / 2, scale * 0.1f, 0, 0);
		spriteBatch.Draw(bloom, position, null, Color.White.Additive() * opacity, Projectile.rotation, bloom.Size() / 2, scale * 0.05f, 0, 0);
	}
}