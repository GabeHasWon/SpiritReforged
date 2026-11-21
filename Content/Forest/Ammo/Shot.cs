using SpiritReforged.Common.Subclasses.Shotguns;
using Terraria.DataStructures;
using Terraria.Audio;
using SpiritReforged.Content.Particles;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.NPCCommon;
using SpiritReforged.Common.Visuals;

namespace SpiritReforged.Content.Forest.Ammo;

public class Shot : ModItem, IShotgunAmmo
{
	public ShotgunStats Stats => new(ShotgunStats.Additive(6), ShotgunStats.Multiple(0.4f));

	public override void SetStaticDefaults() => NPCShopHelper.AddEntry(NPCShopHelper.ConditionalEntry.FromNPC(NPCID.ArmsDealer, new NPCShop.Entry(Type)));

	public override void SetDefaults()
	{
		Item.DamageType = ModContent.GetInstance<ShotgunClass>();
		Item.ammo = ModContent.ItemType<Shot>();
		Item.maxStack = Item.CommonMaxStack;
		Item.consumable = true;
		Item.shootSpeed = 12.5f;
		Item.damage = 5;
		Item.value = Item.buyPrice(copper: 15);
	}

	void IShotgunAmmo.OnShoot(Item item, Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback, int index)
	{
		if (index == 0)
			velocity = position.DirectionTo(Main.MouseWorld) * velocity.Length(); //Remove all spread from the first shot

		Projectile.NewProjectile(source, position, velocity * Main.rand.NextFloat(0.75f, 1.5f), ModContent.ProjectileType<ShotProjectile>(), damage, knockback, player.whoAmI);
		
		for (int x = 0; x < 3; x++)
			Dust.NewDustPerfect(position, DustID.Torch, velocity.RotatedByRandom(Stats.GetShotSpread(player) * 1.25f) * Main.rand.NextFloat(1f, 3f), 0, default, Main.rand.NextFloat(1.5f)).noGravity = true;
		
		Dust.NewDustPerfect(position + velocity, DustID.Smoke, Vector2.Normalize(velocity).RotatedByRandom(0.4f) * Main.rand.NextFloat(3f), 240, default, Main.rand.NextFloat(3f, 6f));
	}
}

public class ShotProjectile : ModProjectile
{
	public const int MAX_TIMELEFT = 25;

	public Color BaseColor = new(230, 150, 0);

	public override string Texture => AssetLoader.EmptyTexture;

	public override void SetStaticDefaults()
	{
		ProjectileID.Sets.TrailingMode[Type] = 0;
		ProjectileID.Sets.TrailCacheLength[Type] = 5;
	}

	public override void SetDefaults()
	{
		Projectile.friendly = true;
		Projectile.DamageType = ModContent.GetInstance<ShotgunClass>();
		Projectile.extraUpdates = 1;
		Projectile.Size = new(4);
		Projectile.timeLeft = MAX_TIMELEFT;
		Projectile.scale = Main.rand.NextFloat(.25f, 1f);
	}

	public override void AI()
	{
		Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
	}

	public override bool PreDraw(ref Color lightColor)
	{
		Main.instance.LoadProjectile(873); //Ensure these textures are loaded before drawing
		Main.instance.LoadProjectile(686);

		float time = MathHelper.Min(Projectile.timeLeft / (float)MAX_TIMELEFT, 1f);
		float fadeIn = 1f;
		if (time > 0.5f)
			fadeIn = 1f - (time - 0.5f) / 0.5f;

		int trailLength = Projectile.oldPos.Length;

		for (int i = 0; i < trailLength; i++)
		{
			var texture = TextureAssets.Projectile[873].Value;

			float lerp = 1f - i / (float)(trailLength - 1);
			var color = (Color.Lerp(BaseColor.MultiplyRGBA(Color.Black * .5f), BaseColor, lerp) with { A = 0 }) * lerp;
			var position = Projectile.oldPos[i] - Main.screenPosition;
			var scale = new Vector2(time, 1f) * Projectile.scale;

			if (i == 0)
			{
				color = Color.White with { A = 0 };
				texture = TextureAssets.Projectile[686].Value;
				scale = new Vector2(MathHelper.Max(time, .25f), 1f) * Projectile.scale * .45f;
			}

			Main.EntitySpriteDraw(texture, position, null, color * fadeIn, Projectile.rotation, texture.Size() / 2, scale, SpriteEffects.None);
		}

		return false;
	}

	public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
	{
		SoundEngine.PlaySound(SoundID.Item10, target.Center);
		SoundEngine.PlaySound(new SoundStyle("SpiritReforged/Assets/SFX/Item/BulletHit") with { Volume = 0.25f, Pitch = 0.1f }, target.Center); //CACHE ME

		ParticleRenderers.UnderProjectiles.Add(new GlowParticle(Projectile.Center, Vector2.Zero, Color.Orange, 0.4f, 30)
		{
			TimeActive = 15
		});

		ParticleRenderers.UnderProjectiles.Add(new GlowParticle(Projectile.Center, Vector2.Zero, Color.White.Additive(), 0.3f, 25)
		{
			TimeActive = 12
		});
	}
}
