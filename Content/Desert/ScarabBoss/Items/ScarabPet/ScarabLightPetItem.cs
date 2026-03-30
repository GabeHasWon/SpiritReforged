using SpiritReforged.Common.BuffCommon;
using SpiritReforged.Common.Visuals.Glowmasks;

namespace SpiritReforged.Content.Desert.ScarabBoss.Items.ScarabPet;

public class ScarabLightPetItem : ModItem
{
	[AutoloadGlowmask("255,255,255", false)]
	public sealed class ScarabLightPet : ModProjectile 
	{
		private ref float AdditiveFade => ref Projectile.ai[0];
		private ref float FrameCounter => ref Projectile.ai[1];

		public override void SetStaticDefaults()
		{
			Main.projFrames[Type] = 7;
			Main.projPet[Type] = true;
		}

		public override void SetDefaults()
		{
			Projectile.Size = new Vector2(58, 50);
			Projectile.timeLeft = 2;
			Projectile.tileCollide = false;
		}

		public override void AI()
		{
			Player owner = Main.player[Projectile.owner];
			owner.GetModPlayer<PetPlayer>().PetFlag<ScarabLightPetBuff>(Projectile);

			if (Projectile.velocity.X > 0)
				Projectile.spriteDirection = -1;
			else if (Projectile.velocity.X < 0)
				Projectile.spriteDirection = 1;

			Projectile.rotation = Projectile.velocity.X * 0.06f;

			Lighting.AddLight(Projectile.Center, new Vector3(0.9f, 0.4f, 0.1f) * 1.5f);

			Vector2 targetPosition = owner.Center - new Vector2(0, 80);
			float distance = Projectile.Center.DistanceSQ(targetPosition);
			bool far = distance > 700 * 700;
			(float mod, float factor, float fade) = far ? (9, 0.25f, 0) : (6, 0.15f, 1);

			AdditiveFade = MathHelper.Lerp(AdditiveFade, fade, 0.1f);

			if (distance < 40 * 40)
				Projectile.velocity *= 0.97f;
			else if (distance < 2500 * 2500f)
				Projectile.velocity = Vector2.SmoothStep(Projectile.velocity, (targetPosition - Projectile.Center).SafeNormalize(Vector2.Zero) * mod, factor);
			else
			{
				AdditiveFade = MathHelper.Lerp(AdditiveFade, 0, 0.01f);
				Projectile.Opacity = AdditiveFade;

				if (Projectile.Opacity <= 0.02f)
					Projectile.Center = targetPosition - new Vector2(0, 10);
			}

			if (distance < 2500 * 2500f)
				Projectile.Opacity = MathHelper.Lerp(Projectile.Opacity, 1, 0.1f);

			SetFrame();
		}

		private void SetFrame()
		{
			FrameCounter += 1 + (1 - AdditiveFade) * 0.33f;
			Projectile.frame = (int)(FrameCounter / 4f % Main.projFrames[Type]);
		}

		public override bool PreDraw(ref Color lightColor)
		{
			Texture2D tex = TextureAssets.Projectile[Type].Value;
			Texture2D glow = GlowmaskProjectile.ProjIdToGlowmask[Type].Glowmask.Value;
			Vector2 drawPos = Projectile.Center - Main.screenPosition;
			int frameHeight = tex.Height / Main.projFrames[Type];
			Rectangle src = new(0, frameHeight * Projectile.frame, tex.Width, frameHeight);
			SpriteEffects effects = Projectile.spriteDirection == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
			Color color = Projectile.GetAlpha(lightColor) with { A = (byte)(255 * AdditiveFade) } * Projectile.Opacity;
			
			Main.spriteBatch.Draw(tex, drawPos, src, color, Projectile.rotation, src.Size() / 2f, 1f, effects, 0);

			Color glowColor = Projectile.GetAlpha(Color.White) with { A = (byte)(255 * AdditiveFade) } * Projectile.Opacity;
			Main.spriteBatch.Draw(glow, drawPos, src, glowColor, Projectile.rotation, src.Size() / 2f, 1f, effects, 0);
			return false;
		}
	}

	public class ScarabLightPetBuff : PetBuff<ScarabLightPet>
	{
		protected override (string, string) BuffInfo => ("Scarababy", "'It's plotting...'");
		protected override bool IsLightPet => true;
	}

	public override void SetDefaults()
	{
		Item.CloneDefaults(ItemID.Fish);
		Item.shoot = ModContent.ProjectileType<ScarabLightPet>();
		Item.buffType = ModContent.BuffType<ScarabLightPetBuff>();
		Item.UseSound = SoundID.NPCDeath6;
		Item.rare = ItemRarityID.Master;
		Item.master = true;
	}

	public override void UseStyle(Player player, Rectangle heldItemFrame)
	{
		if (player.whoAmI == Main.myPlayer && player.itemTime == 0)
			player.AddBuff(Item.buffType, 3600, true);
	}

	public override bool CanUseItem(Player player) => player.miscEquips[1].IsAir;
}