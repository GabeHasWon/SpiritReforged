using SpiritReforged.Common.ModCompat;
using SpiritReforged.Common.ProjectileCommon;
using SpiritReforged.Content.Desert.Scarabeus.Items.Projectiles;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Desert.Scarabeus.Items;

public class RoyalKhopesh : ModItem
{
	public override bool IsLoadingEnabled(Mod mod) => false;

	public override void SetDefaults()
	{
		Item.damage = 28;
		Item.Size = new Vector2(48, 52);
		Item.useTime = Item.useAnimation = 40;
		Item.knockBack = 1f;
		Item.shootSpeed = 1;
		Item.noMelee = true;
		Item.channel = true;
		Item.noUseGraphic = true;
		Item.DamageType = DamageClass.Melee;
		Item.useTurn = false;
		Item.autoReuse = true;
		Item.rare = ItemRarityID.Green;
		Item.value = Item.sellPrice(gold: 2);
		Item.useStyle = ItemUseStyleID.Swing;
		Item.shoot = ModContent.ProjectileType<RoyalKhopeshHeld>();
		//Item.UseSound = SoundID.DD2_MonkStaffSwing;

		MoRHelper.SetSlashBonus(Item);
	}

	public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
	{
		PreNewProjectile.New(source, player.Center, Vector2.Zero, Item.shoot, Item.damage, Item.knockBack, player.whoAmI, preSpawnAction: (Projectile projectile) =>
		{
			KhopeshPlayer kPlayer = player.GetModPlayer<KhopeshPlayer>();
			var khopesh = projectile.ModProjectile as RoyalKhopeshHeld;

			if (kPlayer.Combo != 2) //Don't use standard item use sound on use, allow the final swing to do it at a specified time
				khopesh.DoSwingNoise();

			int useSpeed = (int)(Item.useTime / player.GetTotalAttackSpeed(DamageClass.Melee));
			khopesh.BaseDirection = velocity;
			khopesh.SwingDirection = (kPlayer.Combo % 3 == 1) ? -1 : 1;
			khopesh.SwingTime = (kPlayer.Combo == 2) ? (int)(useSpeed * 1.7f) : useSpeed;
			khopesh.SwingTime *= 1 + RoyalKhopeshHeld.EXTRA_UPDATES;
			khopesh.Combo = kPlayer.Combo;
			khopesh.SwingRadians = MathHelper.Pi * 1.75f;

			float scale = Item.scale;
			player.ApplyMeleeScale(ref scale);
			khopesh.SizeModifier = scale;

			kPlayer.IncrementCombo();
		});

		return false;
	}

	public override bool CanUseItem(Player player) => player.ownedProjectileCounts[Item.shoot] == 0;

	public override bool MeleePrefix() => true;
}

public class KhopeshPlayer : ModPlayer
{
	public int Combo { get; set; }

	private int _comboTimer;

	private const int COMBOTIMER_MAX = 60;

	public override bool IsLoadingEnabled(Mod mod) => false;

	public override void ResetEffects()
	{
		_comboTimer = Math.Max(--_comboTimer, 0);
		if (Player.HeldItem.type != ModContent.ItemType<RoyalKhopesh>())
		{
			_comboTimer = 0;
			Combo = 0;
		}

		else if (_comboTimer == 0)
			Combo = 0;
	}

	public void IncrementCombo()
	{
		Combo++;
		_comboTimer = COMBOTIMER_MAX;
		Combo %= 3;
	}
}