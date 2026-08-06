using SpiritReforged.Common.Easing;
using SpiritReforged.Common.ItemCommon.Abstract;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.PlayerCommon;
using SpiritReforged.Common.PrimitiveRendering;
using SpiritReforged.Common.PrimitiveRendering.Trail_Components;
using SpiritReforged.Common.PrimitiveRendering.Trails;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Particles;

namespace SpiritReforged.Content.Forest.Rapiers;

public class VolcanicWhetstone : EquippableItem
{
	public sealed class WhetstoneSpark : ModProjectile, IDrawPixelated
	{
		public int SourceIndex => (int)Projectile.ai[0];

		public int TimeActive
		{
			get => (int)Projectile.localAI[0];
			set => Projectile.localAI[0] = value;
		}

		private bool _createdTrail;

		public override void SetDefaults()
		{
			Projectile.Size = new Vector2(10);
			Projectile.friendly = true;
			Projectile.scale = 0.12f;
			Projectile.extraUpdates = 1;
		}

		public override void AI()
		{
			Projectile.velocity.X *= 0.99f;
			Projectile.velocity.Y += 0.1f;
			Projectile.rotation = Projectile.velocity.ToRotation();

			if (!Main.dedServ && !_createdTrail)
			{
				float scale = Projectile.scale * 10;
				EntityTrailPosition position = new(Projectile);
				ProjectileTrailRenderer renderer = TrailSystem.ProjectileRenderer;

				renderer.CreateTrail(Projectile, new VertexTrail(new StandardColorTrail(Color.Goldenrod.Additive(50) * 0.5f), new RoundCap(), position, new DefaultShader(), 5 * scale, 20 * scale));

				ParticleHandler.SpawnParticle(new FireParticle(Projectile.Center, Projectile.velocity * 1.5f, [Color.White, Color.Orange, Color.Red], 1, Main.rand.NextFloat(0.05f, 0.1f), EaseFunction.EaseCircularOut, 30));

				_createdTrail = true;
			}

			TimeActive++;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => base.OnHitNPC(target, hit, damageDone);

		public override bool? CanHitNPC(NPC target) => target.whoAmI != SourceIndex || TimeActive > 10;

		public override void OnKill(int timeLeft)
		{
			for (int i = 0; i < 2; i++)
				Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Smoke, 0, -1, 100, default, Main.rand.NextFloat(1, 2)).noGravity = true;
		}

		public override bool PreDraw(ref Color lightColor) => false;

		void IDrawPixelated.DrawPixelated(SpriteBatch spriteBatch)
		{
			Texture2D texture = TextureAssets.Projectile[Type].Value;
			Texture2D star = AssetLoader.LoadedTextures["Star"].Value;
			Vector2 position = Projectile.Center - Main.screenPosition;

			float length = Projectile.velocity.Length() / 4f;
			Vector2 scale = new Vector2(1 + length, Math.Max(1 - length, 0.1f)) * Projectile.scale;

			IDrawPixelated.PixelateDrawPosition(ref position);

			spriteBatch.Draw(texture, position, null, Color.Red.Additive() * 0.3f, Projectile.rotation, texture.Size() / 2, Projectile.scale, 0, 0);
			spriteBatch.Draw(texture, position, null, Color.OrangeRed.Additive(), Projectile.rotation, texture.Size() / 2, scale, 0, 0);
			spriteBatch.Draw(texture, position, null, Color.White.Additive(), Projectile.rotation, texture.Size() / 2, scale * 0.75f, 0, 0);

			float diminishedLength = 3 - Projectile.velocity.Length();
			if (diminishedLength > 0)
			{
				spriteBatch.Draw(star, position, null, Color.Yellow.Additive() * diminishedLength * 0.5f, 0, star.Size() / 2, Projectile.scale * 0.5f, 0, 0);
				spriteBatch.Draw(star, position, null, Color.White.Additive() * diminishedLength * 0.5f, 0, star.Size() / 2, Projectile.scale * 0.4f, 0, 0);
			}
		}
	}

	public sealed class VolcanicSparkPlayer : ModPlayer
	{
		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (Player.HasEquip<VolcanicWhetstone>() && hit.Crit && SharpeningStone.SweetspotBonusPlayer.HoldingRapier(Player))
			{
				int damage = (int)(Player.HeldItem.damage * 0.8f);

				for (int i = 0; i < 3; i++)
				{
					Vector2 velocity = (Vector2.UnitY * -Main.rand.NextFloat(3, 6)).RotatedByRandom(0.5f);
					Projectile.NewProjectile(Player.GetSource_OnHit(target), target.Center, velocity, ModContent.ProjectileType<WhetstoneSpark>(), damage, 1, Player.whoAmI, target.whoAmI);
				}
			}
		}
	}

	public const float CRIT_BONUS = 0.15f;

	public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(Math.Round(CRIT_BONUS * 100));

	public override void SetDefaults()
	{
		Item.width = 32;
		Item.height = 22;
		Item.value = Item.sellPrice(gold: 1, silver: 30);
		Item.rare = ItemRarityID.Blue;
		Item.accessory = true;
	}

	public override void UpdateAccessory(Player player, bool hideVisual)
	{
		if (player.TryGetModPlayer(out SharpeningStone.SweetspotBonusPlayer bonusPlayer))
			bonusPlayer.sweetspotAdditive += CRIT_BONUS;
	}

	public override void AddRecipes() => CreateRecipe().AddIngredient(ModContent.ItemType<SharpeningStone>())
		.AddIngredient(ItemID.MagmaStone).AddTile(TileID.TinkerersWorkbench).Register();
}