using SpiritReforged.Common.BuffCommon;
using SpiritReforged.Common.ProjectileCommon;
using Terraria.Audio;

namespace SpiritReforged.Content.Crossmod.Spooky.Items.Pets;

internal class SandBrownie : ModItem
{
	public sealed class Columbald : ModProjectile
	{
		private ref float DieTimer => ref Projectile.ai[0];

		public override void SetStaticDefaults()
		{
			Main.projFrames[Type] = 2;
			Main.projPet[Type] = true;

			HeldProjectileSet.SkipAutoHeldCheck[Type] = true;
		}

		public override void SetDefaults()
		{
			Projectile.Size = new Vector2(26);
			Projectile.timeLeft = 2;
			Projectile.tileCollide = true;
		}

		public override void AI()
		{
			const int FlyDist = 400 * 400;

			SetFrame();

			Player owner = Main.player[Projectile.owner];
			var modPlayer = owner.GetModPlayer<PetPlayer>();
			modPlayer.pets.Add(Projectile.type);

			if (owner.dead || !owner.HasBuff<ColumbaldBuff>())
				modPlayer.pets.Remove(Projectile.type);

			if (modPlayer.pets.Contains(Projectile.type) && DieTimer == 0)
				Projectile.timeLeft = 2;
			else
			{
				DieTimer++;
				Projectile.timeLeft = 2;
				Projectile.velocity.X = 0;
				Projectile.rotation = 0;

				if (DieTimer == 8)
				{
					SoundStyle style = new SoundStyle("Spooky/Content/Sounds/ColumboThud", SoundType.Sound);
					SoundEngine.PlaySound(style, Projectile.Center);
				}

				if (DieTimer > 15)
					Projectile.active = false;

				return;
			}

			float distSq = owner.DistanceSQ(Projectile.Center);

			if (!Projectile.tileCollide)
			{
				if (!Collision.SolidCollision(Projectile.position, Projectile.width, Projectile.height))
					Projectile.tileCollide = true;

				if (!Projectile.tileCollide)
					distSq = FlyDist + 1;
			}

			if (distSq < FlyDist)
			{
				Projectile.velocity.Y += 0.2f;
				Projectile.rotation += Projectile.velocity.X * 0.02f;

				Collision.StepUp(ref Projectile.position, ref Projectile.velocity, Projectile.width, Projectile.height, ref Projectile.stepSpeed, ref Projectile.gfxOffY);

				if (!Projectile.tileCollide && !Collision.SolidCollision(Projectile.position, Projectile.width, Projectile.height))
					Projectile.tileCollide = true;
			}
			else
			{
				Projectile.rotation += Projectile.velocity.X * 0.05f;
				Projectile.tileCollide = false;
			}

			if (distSq <= 120 * 120)
				Projectile.velocity.X *= 0.9f;
			else if (distSq < FlyDist)
				Projectile.velocity.X = MathHelper.Lerp(Projectile.velocity.X, Math.Sign(owner.Center.X - Projectile.Center.X) * 6, 0.15f);
			else if (distSq >= FlyDist)
			{
				Projectile.velocity = Vector2.Lerp(Projectile.velocity, Projectile.DirectionTo(owner.Center) * 9, 0.2f);

				if (distSq > 1500 * 1500)
					Projectile.Center = owner.Center;
			}

			if (Math.Abs(Projectile.velocity.X) > 0.01f)
				Projectile.spriteDirection = Projectile.direction = -Math.Sign(Projectile.velocity.X);
		}

		private void SetFrame()
		{
			if (DieTimer > 0)
				Projectile.frame = 1;
		}
	}

	public class ColumbaldBuff : PetBuff<Columbald>
	{
		protected override (string, string) BuffInfo => ("Columbald", "The being of bald");
	}

	public override void SetDefaults()
	{
		Item.CloneDefaults(ItemID.Fish);
		Item.shoot = ModContent.ProjectileType<Columbald>();
		Item.buffType = ModContent.BuffType<ColumbaldBuff>();
		Item.UseSound = SoundID.NPCDeath6;
	}

	public override void UseStyle(Player player, Rectangle heldItemFrame)
	{
		if (player.whoAmI == Main.myPlayer && player.itemTime == 0)
			player.AddBuff(Item.buffType, 3600, true);
	}

	public override bool CanUseItem(Player player) => player.miscEquips[0].IsAir;
}
