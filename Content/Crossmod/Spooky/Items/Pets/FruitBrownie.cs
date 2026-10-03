using SpiritReforged.Common.BuffCommon;
using SpiritReforged.Common.ProjectileCommon;
using Terraria.Audio;

namespace SpiritReforged.Content.Crossmod.Spooky.Items.Pets;

internal class FruitBrownie : ModItem
{
	public sealed class Columbat : ModProjectile
	{
		private ref float FrameCounter => ref Projectile.ai[0];
		private ref float DieTimer => ref Projectile.ai[1];

		public override void SetStaticDefaults()
		{
			Main.projFrames[Type] = 5;
			Main.projPet[Type] = true;

			HeldProjectileSet.SkipAutoHeldCheck[Type] = true;
		}

		public override void SetDefaults()
		{
			Projectile.Size = new Vector2(38, 20);
			Projectile.timeLeft = 2;
			Projectile.tileCollide = false;
		}

		public override void AI()
		{
			SetFrame();

			Player owner = Main.player[Projectile.owner];
			var modPlayer = owner.GetModPlayer<PetPlayer>();
			modPlayer.pets.Add(Projectile.type);

			if (owner.dead || !owner.HasBuff<ColumbatBuff>())
				modPlayer.pets.Remove(Projectile.type);

			if (modPlayer.pets.Contains(Projectile.type) && DieTimer == 0)
				Projectile.timeLeft = 2;
			else
			{
				DieTimer++;
				Projectile.timeLeft = 2;

				if (DieTimer == 8)
				{
					SoundStyle style = new SoundStyle("Spooky/Content/Sounds/ColumboThud", SoundType.Sound);
					SoundEngine.PlaySound(style, Projectile.Center);
				}

				if (DieTimer > 15)
					Projectile.active = false;

				return;
			}

			float off = owner.Center.X - Projectile.Center.X;
			float sign = MathF.Abs(off);

			if (sign > 1f)
			{
				if (off < 0)
					Projectile.spriteDirection = 1;
				else
					Projectile.spriteDirection = -1;
			}

			Projectile.rotation = Projectile.velocity.X * 0.06f;
			Projectile.Center = Vector2.Lerp(Projectile.Center, owner.Center - new Vector2(owner.direction * 40, 80), 0.3f);
		}

		private void SetFrame()
		{
			FrameCounter++;
			Projectile.frame = (int)(FrameCounter / 4f % 4);

			if (DieTimer > 0)
				Projectile.frame = 4;
		}
	}

	public class ColumbatBuff : PetBuff<Columbat>
	{
		protected override (string, string) BuffInfo => ("Columbat", "The being of bat");
	}

	public override void SetDefaults()
	{
		Item.CloneDefaults(ItemID.Fish);
		Item.shoot = ModContent.ProjectileType<Columbat>();
		Item.buffType = ModContent.BuffType<ColumbatBuff>();
		Item.UseSound = SoundID.NPCDeath6;
	}

	public override void UseStyle(Player player, Rectangle heldItemFrame)
	{
		if (player.whoAmI == Main.myPlayer && player.itemTime == 0)
			player.AddBuff(Item.buffType, 3600, true);
	}

	public override bool CanUseItem(Player player) => player.miscEquips[0].IsAir;
}
