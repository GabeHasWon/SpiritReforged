using SpiritReforged.Common.ModCompat;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Desert.Scarabeus.Items;

public class SerratedClaws : ModItem
{
	public class SerratedClawsPlayer : ModPlayer
	{
		internal float animationTimer = 0;
		internal bool hasClaws = false;

		public override void ResetEffects() => hasClaws = false;

		public override void PostUpdateEquips()
		{
			if (!Player.channel || Player.HeldItem.type != ModContent.ItemType<SerratedClaws>())
			{
				animationTimer = 0;
				return;
			}

			animationTimer += 0.6f;
		}

		public override void FrameEffects() //This way, players can be seen wearing backpacks in the selection screen
		{
			if (hasClaws)
			{
				Player.handon = EquipLoader.GetEquipSlot(Mod, HandsOnName, EquipType.HandsOn);
				Player.handoff = EquipLoader.GetEquipSlot(Mod, HandsOffName, EquipType.HandsOff);
			}
		}
	}

	public class ClawHitbox : ModProjectile
	{
		public override string Texture => "Terraria/Images/NPC_0";

		public bool Initialized
		{
			get => Projectile.ai[2] == 1;
			set => Projectile.ai[2] = value ? 1 : 0;
		}

		public ref float Width => ref Projectile.ai[0];
		public ref float Height => ref Projectile.ai[1];

		public override void SetDefaults()
		{
			Projectile.friendly = true;
			Projectile.timeLeft = 2;
			Projectile.ignoreWater = true;
			Projectile.penetrate = -1;
		}

		public override void AI()
		{
			if (!Initialized)
			{
				Initialized = true;
				Projectile.Resize((int)Width, (int)Height);
			}

			Player owner = Main.player[Projectile.owner];

			if (!owner.channel)
				return;

			Projectile.timeLeft++;
			Projectile.velocity = Vector2.Zero;

			if (Main.myPlayer == Projectile.owner)
			{
				Projectile.Center = owner.Center + owner.DirectionTo(Main.MouseWorld) * 14;
				Projectile.netUpdate = true;
			}
		}
	}

	public const string HandsOnName = "SpiritReforged:SerratedHandsOn";
	public const string HandsOffName = "SpiritReforged:SerratedHandsOff";

	public override void Load()
	{
		EquipLoader.AddEquipTexture(Mod, Texture + "_Hands", EquipType.HandsOn, this, HandsOnName);
		EquipLoader.AddEquipTexture(Mod, Texture + "_Hands", EquipType.HandsOff, this, HandsOffName);
	}

	public override void SetDefaults()
	{
		Item.damage = 3;
		Item.Size = new Vector2(34, 28);
		Item.useTime = Item.useAnimation = 6;
		Item.knockBack = 0.2f;
		Item.DamageType = DamageClass.Melee;
		Item.useTurn = true;
		Item.rare = ItemRarityID.Expert;
		Item.value = Item.sellPrice(gold: 1);
		Item.useStyle = ItemUseStyleID.Swing;
		Item.pick = 50;
		Item.noUseGraphic = true;
		Item.noMelee = true;
		Item.channel = true;
		Item.autoReuse = true;
		Item.shoot = ModContent.ProjectileType<ClawHitbox>();

		MoRHelper.SetSlashBonus(Item);
	}

	public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
	{
		Projectile.NewProjectile(source, position, Vector2.Zero, type, damage, knockback, player.whoAmI, 20, 20);
		return false;
	}

	public override void HoldItem(Player player)
	{
		player.GetModPlayer<SerratedClawsPlayer>().hasClaws = true;

		if (!player.channel)
			return;

		float rotation = player.AngleTo(Main.MouseWorld) - MathHelper.PiOver2;
		float animationTimer = player.GetModPlayer<SerratedClawsPlayer>().animationTimer * 0.9f;
		
		player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, rotation + MathF.Sin(animationTimer) * 0.65f);
		player.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, rotation + MathF.Cos(animationTimer) * 0.65f);
		player.direction = Math.Sign(Main.MouseWorld.X - player.Center.X);
	}
}