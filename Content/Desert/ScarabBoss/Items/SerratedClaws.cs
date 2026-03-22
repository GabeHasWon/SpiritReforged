using SpiritReforged.Common.ModCompat;
using SpiritReforged.Common.PlayerCommon;

namespace SpiritReforged.Content.Desert.ScarabBoss.Items;

public class SerratedClaws : ModItem
{
	public class SerratedClawsHeld : ModProjectile
	{
		public override string Texture => AssetLoader.EmptyTexture;

		private float _animationTime;

		public override void SetDefaults()
		{
			Projectile.Size = new(20);
			Projectile.friendly = true;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.penetrate = -1;
			Projectile.timeLeft = 2;
		}

		public override void AI()
		{
			Player owner = Main.player[Projectile.owner];

			if (Main.netMode == NetmodeID.MultiplayerClient && Main.myPlayer == owner.whoAmI)
				new PlayerMouseHandler.ShareMouseData((byte)owner.whoAmI, Main.MouseWorld).Send();

			if (owner.channel)
			{
				Projectile.timeLeft++;
				Projectile.Center = owner.Center + Projectile.velocity;

				float rotation = owner.AngleTo(PlayerMouseHandler.GetMouse(owner.whoAmI)) - MathHelper.PiOver2;
				float time = (_animationTime += 0.6f) * 0.9f;

				owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, rotation + MathF.Sin(time) * 0.65f);
				owner.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, rotation + MathF.Cos(time) * 0.65f);

				Vector2 oldVelocity = Projectile.velocity;
				Projectile.velocity = owner.DirectionTo(PlayerMouseHandler.GetMouse(owner.whoAmI)) * 14;
					
				if (Projectile.velocity != oldVelocity)
					Projectile.netUpdate = true; //Sync velocity changes if necessary

				owner.ChangeDir(Math.Sign(Projectile.velocity.X));
			}
		}

		public override bool ShouldUpdatePosition() => false;
	}

	private static readonly int[] EquipSlots = new int[2];

	public override void Load()
	{
		EquipSlots[0] = EquipLoader.AddEquipTexture(Mod, Texture + "_Hands", EquipType.HandsOn, this, "SerratedHandsOn");
		EquipSlots[1] = EquipLoader.AddEquipTexture(Mod, Texture + "_Hands", EquipType.HandsOff, this, "SerratedHandsOff");
	}

	public override void SetDefaults()
	{
		Item.damage = 3;
		Item.Size = new Vector2(34, 28);
		Item.useTime = 5;
		Item.useAnimation = 12;
		Item.knockBack = 0.2f;
		Item.DamageType = DamageClass.Melee;
		Item.useTurn = true;
		Item.expert = true;
		Item.value = Item.sellPrice(gold: 1);
		Item.useStyle = ItemUseStyleID.Swing;
		Item.pick = 35;
		Item.noUseGraphic = true;
		Item.noMelee = true;
		Item.channel = true;
		Item.autoReuse = true;
		Item.shoot = ModContent.ProjectileType<SerratedClawsHeld>();

		MoRHelper.SetSlashBonus(Item);
	}

	public override void HoldItemFrame(Player player) => DisplayEquips(player);

	public override void UseItemFrame(Player player) => DisplayEquips(player);

	private static void DisplayEquips(Player player)
	{
		player.handon = EquipSlots[0];
		player.handoff = EquipSlots[1];
	}

	public override float UseSpeedMultiplier(Player player) => player.GetAttackSpeed(DamageClass.Melee) + (1 - player.pickSpeed) * 2;
}