using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Common.Visuals.Glowmasks;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Forest.Mage;

[AutoloadGlowmask("255,255,255")]
public class Sparkler : ModItem
{
	public class SparkleStar : ModProjectile, IDrawPixelated
	{
		public ref float Counter => ref Projectile.ai[0];

		public override string Texture => AssetLoader.EmptyTexture;

		private NPC _target;

		public override void SetDefaults()
		{
			Projectile.Size = new(16);
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Magic;
			Projectile.Opacity = 0;
			Projectile.penetrate = 3;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = -1;
		}

		public override void AI()
		{
			if (++Counter > 20)
			{
				if (_target == null || !_target.active)
				{
					const int max_distance = 500;
					foreach (NPC npc in Main.ActiveNPCs)
					{
						if (npc.CanBeChasedBy() && npc.DistanceSQ(Projectile.Center) < max_distance * max_distance)
						{
							_target = npc;
							break;
						}
					}
				}
				else
				{
					const int move_speed = 8;
					Projectile.velocity = Vector2.Lerp(Projectile.velocity, Projectile.DirectionTo(_target.Center) * move_speed, 0.05f);
				}
			}
			else
			{
				Projectile.velocity *= 0.97f;
			}
		}

		public override void OnKill(int timeLeft)
		{
		}

		public override bool PreDraw(ref Color lightColor)
		{
			return false;
		}

		void IDrawPixelated.DrawPixelated(SpriteBatch spriteBatch)
		{
			Texture2D starTexture = AssetLoader.LoadedTextures["Star"].Value;
			Texture2D bloomTexture = AssetLoader.LoadedTextures["Bloom"].Value;
			Vector2 position = Projectile.Center - Main.screenPosition;

			IDrawPixelated.PixelateDrawPosition(ref position);

			spriteBatch.Draw(bloomTexture, position, null, Color.Goldenrod.Additive() * 0.25f, 0, bloomTexture.Size() / 2, Projectile.scale * 0.1f, SpriteEffects.None, 0);
			spriteBatch.Draw(starTexture, position, null, Color.Goldenrod.Additive(), Projectile.rotation, starTexture.Size() / 2, Projectile.scale * 0.2f, SpriteEffects.None, 0);
		}
	}

	public override void SetDefaults()
	{
		Item.damage = 28;
		Item.mana = 10;
		Item.knockBack = 6.5f;
		Item.width = Item.height = 46;
		Item.useTime = Item.useAnimation = 34;
		Item.DamageType = DamageClass.Magic;
		Item.useStyle = ItemUseStyleID.Swing;
		Item.value = Item.sellPrice(gold: 1);
		Item.rare = ItemRarityID.Blue;
		Item.UseSound = SoundID.DD2_BookStaffCast with { Pitch = 0.3f };
		Item.shoot = ModContent.ProjectileType<SparkleStar>();
		Item.shootSpeed = 14f;
		Item.autoReuse = true;
		Item.useTurn = true;
		Item.noUseGraphic = true;
		Item.noMelee = true;
	}

	public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
	{
		Vector2 offset = Vector2.Normalize(velocity) * 30;

		if (Collision.CanHit(position, 2, 2, position + velocity, 2, 2))
			position += offset;
	}

	public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
	{
		float length = velocity.Length();

		for (int i = 0; i < 3; i++)
			Projectile.NewProjectile(source, position, Main.rand.NextVector2Circular(1, 1) * Main.rand.NextFloat(length * 0.8f, length * 1.2f), type, damage, knockback, player.whoAmI);

		return false;
	}
}