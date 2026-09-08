using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.NPCCommon;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.PrimitiveRendering;
using SpiritReforged.Common.PrimitiveRendering.Trail_Components;
using SpiritReforged.Common.PrimitiveRendering.Trails;
using SpiritReforged.Common.Subclasses.Greatshields;
using SpiritReforged.Common.Subclasses.Shotguns;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Forest.Ammo;
using SpiritReforged.Content.Particles;
using SpiritReforged.Content.SaltFlats.Items.Ammo;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.Graphics.Renderers;

namespace SpiritReforged.Content.Evil.Items.Ammo;
public class Slug : ShotgunAmmoItem
{
	public override void SetStaticDefaults() => NPCShopHelper.AddEntry(NPCShopHelper.ConditionalEntry.FromNPC(NPCID.ArmsDealer, new NPCShop.Entry(Type, Condition.DownedBrainOfCthulhu)));

	static List<Projectile> Behavior(Item item, Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 direction, int shotCount, float spreadAmount, float speed, int damage, float knockback)
	{
		float damageIncrease = 1f;

		// 10% damage increase per shot count after 1
		// Only happens with accessories and such
		for (int i = 1; i < shotCount; i++)
			damageIncrease += 0.1f;

		return [Projectile.NewProjectileDirect(source, position, direction.RotatedByRandom(spreadAmount) * speed * Main.rand.NextFloat(1.25f, 1.5f), ModContent.ProjectileType<SlugProjectile>(), damage, knockback, player.whoAmI)];
	}

	public Slug() : base(Behavior, 1, .05f, 15f) { }

	public override void SafeSetDefaults()
	{
		Item.damage = 12;
		Item.value = Item.buyPrice(copper: 50);
	}

	public override void AddRecipes()
	{
		CreateRecipe(75).
			AddIngredient<Shot>(150).
			AddIngredient(ItemID.TissueSample, 3).
			AddTile(TileID.Anvils).
			Register();
	}
}

public class SlugProjectile : ModProjectile
{
	public static readonly Asset<Texture2D> BaseTexture = DrawHelpers.RequestLocal<SlugProjectile>("SlugProjectile", false);
	public static readonly Asset<Texture2D> OutlineTexture = DrawHelpers.RequestLocal<SlugProjectile>("SlugProjectile_Outline", false);

	public const int MAX_TIMELEFT = 360;
	public const int TIME_TILL_GRAVITY = 45; // how many frames before gravity kicks in, and the fire effects fade off
	public override string Texture => AssetLoader.EmptyTexture;
	
	private readonly ParticleRenderer _trailRenderer = new();
	private VertexTrail[] _trails;

	public override void SetStaticDefaults()
	{
		ProjectileID.Sets.TrailingMode[Type] = 0;
		ProjectileID.Sets.TrailCacheLength[Type] = 7;
	}

	public override void SetDefaults()
	{
		Projectile.friendly = true;
		Projectile.DamageType = ModContent.GetInstance<ShotgunClass>();
		Projectile.extraUpdates = 1;
		Projectile.stopsDealingDamageAfterPenetrateHits = true;
		Projectile.penetrate = 1;
		Projectile.Size = new(4);
		Projectile.timeLeft = MAX_TIMELEFT;
		Projectile.scale = 1f;
		Projectile.hide = true;
	}

	public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI) => behindNPCs.Add(index);

	public override void AI()
	{
		if (!Main.dedServ)
		{
			if (_trails == null)
				CreateTrail();

			foreach (VertexTrail trail in _trails)
				trail.Update();
		}

		Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

		if (Projectile.timeLeft < MAX_TIMELEFT - TIME_TILL_GRAVITY)
		{
			Projectile.velocity *= 0.98f;
			Projectile.velocity.Y += 0.05f;

			if (Projectile.velocity.Y > 0)
				Projectile.velocity.Y *= 1.05f;

			if (Projectile.velocity.Y > 16f)
				Projectile.velocity.Y = 16f;
		}
		else
			Projectile.velocity *= 0.985f;
	}

	public override bool PreDraw(ref Color lightColor)
	{
		var tex = BaseTexture.Value;
		var texOutline = OutlineTexture.Value;
		var texWhite = TextureColorCache.ColorSolid(tex, Color.White);
		var bloom = AssetLoader.LoadedTextures["Bloom"].Value;

		float fadeOut = 1f;

		if (Projectile.timeLeft < 120)
			fadeOut = Projectile.timeLeft / 120f;

		int fadeTime = MAX_TIMELEFT - TIME_TILL_GRAVITY;

		if (Projectile.timeLeft > fadeTime)
		{
			Main.instance.LoadProjectile(873); //Ensure these textures are loaded before drawing
			Main.instance.LoadProjectile(ProjectileID.FallingStar);

			var starAura = TextureAssets.Extra[ExtrasID.FallingStar].Value;
			var glowLine = TextureAssets.Projectile[873].Value;

			float time = MathHelper.Min((Projectile.timeLeft - fadeTime) / (float)TIME_TILL_GRAVITY, 1f);
			float fadeIn = 1f;
			if (time > 0.75f)
				fadeIn = 1f - (time - 0.75f) / 0.25f;

			int trailLength = Projectile.oldPos.Length;

			for (int i = 0; i < trailLength; i++)
			{
				float lerp = i / (float)trailLength;

				Vector2 drawPos = Projectile.oldPos[i] + Projectile.Size / 2f - Projectile.velocity - Main.screenPosition;

				Color color = Color.Lerp(new(255, 100, 100), new(255, 60, 0), lerp).Additive();

				Vector2 scale = new Vector2(Projectile.scale) * 0.75f * (1f - lerp);

				Main.spriteBatch.Draw(glowLine, drawPos, null, color * 0.85f * fadeIn * (1f - lerp) * time, Projectile.rotation, glowLine.Size() / 2f, new Vector2(scale.X * 1.15f, scale.Y * 1.5f), 0f, 0f);

				Main.spriteBatch.Draw(starAura, drawPos, null, color * fadeIn * (1f - lerp) * time, Projectile.rotation, starAura.Size() / 2f, scale * 0.75f , 0f, 0f);
			}

			Main.spriteBatch.Draw(texWhite, Projectile.Center - Main.screenPosition, null, Color.Orange.Additive() * 0.25f * time, Projectile.rotation, texWhite.Size() / 2f, Projectile.scale, 0f, 0f);
		}

		const int bloomFade = 100;
		int bloomTime = MAX_TIMELEFT - bloomFade;

		if (Projectile.timeLeft > MAX_TIMELEFT - bloomFade)
		{
			float fade = (Projectile.timeLeft - bloomTime) / (float)bloomFade;

			_trailRenderer.Draw(Main.spriteBatch);

			if (_trails != null)
			{
				foreach (VertexTrail trail in _trails)
				{
					trail.Opacity = fade;

					if (Projectile.penetrate == -1)
						trail.Opacity *= 0.2f;

					trail?.Draw(TrailSystem.TrailShaders, Main.spriteBatch.GraphicsDevice);
				}
			}

			Main.spriteBatch.Draw(bloom, Projectile.Center - Main.screenPosition, null, Color.Lerp(Color.Yellow, Color.DarkOrange, 1f - fade).Additive() * fade * 0.25f, Projectile.rotation, bloom.Size() / 2f, Projectile.scale * 0.2f, 0f, 0f);
			
			Main.spriteBatch.Draw(bloom, Projectile.Center - Main.screenPosition, null, Color.Lerp(Color.Orange, Color.OrangeRed, 1f - fade).Additive() * fade * 0.25f, Projectile.rotation, bloom.Size() / 2f, Projectile.scale * 0.15f, 0f, 0f);

			Main.spriteBatch.Draw(texOutline, Projectile.Center - Main.screenPosition, null, Color.Lerp(Color.Orange, Color.DarkOrange, 1f - fade).Additive() * fade, Projectile.rotation, texOutline.Size() / 2f, Projectile.scale, 0f, 0f);
		}

		Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, lightColor * fadeOut, Projectile.rotation, tex.Size() / 2f, Projectile.scale, 0f, 0f);

		return false;
	}

	public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
	{
		if (Projectile.timeLeft > MAX_TIMELEFT - TIME_TILL_GRAVITY + 20)
			Projectile.timeLeft = MAX_TIMELEFT - TIME_TILL_GRAVITY + 20;

		Projectile.velocity *= Main.rand.NextFloat(0.05f);

		SoundEngine.PlaySound(SoundID.Item10, target.Center);

		SoundEngine.PlaySound(new SoundStyle("SpiritReforged/Assets/SFX/Item/BulletHit") with { Volume = 0.45f, Pitch = 0.1f }, target.Center);

		ParticleHandler.SpawnParticle(new GlowParticle(Projectile.Center, Vector2.Zero, Color.Orange, 0.4f, 30)
		{
			TimeActive = 15
		});

		ParticleHandler.SpawnParticle(new GlowParticle(Projectile.Center, Vector2.Zero, Color.White.Additive(), 0.3f, 25)
		{
			TimeActive = 12
		});
	}
	
	private void CreateTrail()
	{
		ITrailCap tCap = new RoundCap();
		ITrailPosition tPos = new EntityTrailPosition(Projectile);
		ITrailShader tShader = new ImageShader(AssetLoader.LoadedTextures["GlowTrail"].Value, Vector2.One, 1);

		_trails =
		[
			new VertexTrail(new GradientTrail(Color.DarkOrange.Additive(), Color.Transparent, EaseFunction.EaseQuarticOut), tCap, tPos, tShader, 35, 120, -2),
			new VertexTrail(new GradientTrail(Color.OrangeRed.Additive(), Color.Orange.Additive() * 0.3f, EaseFunction.EaseQuarticOut), tCap, tPos, tShader, 27, 110, -2),
		];
	}
}
