using SpiritReforged.Common.Easing;
using SpiritReforged.Common.ItemCommon.MagazineSystem;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.Subclasses.Shotguns;
using SpiritReforged.Content.Particles;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Underground.Items;
public class LastWord : ModItem
{
	public const float DAMAGE_BONUS = 0.5f; // 50% damage bonus. Will be spread evenly between all pellets of a shotgun.
	public const float MINIMUM_DAMAGE_BONUS = 0.1f; // Minimum amount of damage to add to a shot. Used so shotguns atleast get 10% damage.

	public override void SetDefaults()
	{
		Item.DefaultToAccessory();
		Item.rare = ItemRarityID.Green;

		Item.value = Item.sellPrice(gold: 2);
	}

	public override void UpdateEquip(Player player)
	{
		player.GetModPlayer<LastWordPlayer>().equipped = true;
	}

	class LastWordPlayer : ModPlayer
	{
		public bool equipped;
		public bool canEmpower;

		public override void ResetEffects() => equipped = false;
		public override void PostUpdateEquips() // we must use PostUpdateEquips to ensure it works with magazine changes
		{
			if (equipped && MagazinePlayer.TryGetMagazineWeapon(Player, out var magazineWeapon))
			{
				if (magazineWeapon.AmmoRemaining(Player) == 1 && MagazinePlayer.empoweredCount <= 0 && !magazineWeapon.Reloading && canEmpower)
				{
					MagazinePlayer.EmpowerShot();
					canEmpower = false;
				}
				else if (magazineWeapon.AmmoRemaining(Player) == Player.GetModPlayer<MagazinePlayer>().GetMagazineSize())
					canEmpower = true;
			}
		}
	}

	class LastWordGlobalProjectile : GlobalProjectile
	{
		float _damageBonus;

		public override bool InstancePerEntity => true;
		public override bool AppliesToEntity(Projectile entity, bool lateInstantiation) => entity.DamageType.CountsAsClass(DamageClass.Ranged);
		public override void OnSpawn(Projectile projectile, IEntitySource source)
		{
			Player player = Main.player[projectile.owner];

			if (!player.GetModPlayer<LastWordPlayer>().equipped)
				return;

			if (projectile.DamageType.CountsAsClass(DamageClass.Ranged) && source is EntitySource_ItemUse_WithAmmo ammoSource && MagazinePlayer.TryGetMagazineWeapon(player, out var magazineWeapon) && magazineWeapon.AmmoRemaining(player) == 0)
			{
				Item ammoItem = ammoSource.AmmoItemIdUsed > 0 ? ContentSamples.ItemsByType[ammoSource.AmmoItemIdUsed] : null;

				if (ammoItem.ModItem is not null and ShotgunAmmoItem shotItem)
				{
					float damageBonusToAdd = Math.Max(MINIMUM_DAMAGE_BONUS, DAMAGE_BONUS / player.GetModPlayer<ShotgunPlayer>().GetShotCount(shotItem));

					_damageBonus += damageBonusToAdd;
					projectile.netUpdate = true; // unsure if needed since ModifyHitNPC should sync automatically
				}
			}
		}

		public override void AI(Projectile projectile)
		{
			if (_damageBonus > 0)
			{
				// TODO: do something yogurt
			}
		}

		public override void ModifyHitNPC(Projectile projectile, NPC target, ref NPC.HitModifiers modifiers)
		{
			if (_damageBonus > 0)
				modifiers.FinalDamage += _damageBonus;
		}

		public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (_damageBonus > 0)
			{
				SoundEngine.PlaySound(SoundID.NPCHit8 with { Volume = 0.33f, PitchVariance = 0.33f, Pitch = 0.5f}, projectile.Center);

				for (int i = 0; i < 2; i++)
				{
					ParticleHandler.SpawnParticle(new SmokeCloud(projectile.Center + Main.rand.NextVector2Circular(15f, 15f), -Vector2.UnitY, Color.DarkRed * 0.2f, 0.05f, EaseFunction.EaseQuinticOut, 60)
					{
						Pixellate = true,
						PixelDivisor = 3,
					});

					Dust.NewDustPerfect(projectile.Center, DustID.Blood, Main.rand.NextVector2Circular(2.5f, 2.5f), 160, default, 1.35f).noGravity = true;

					Dust.NewDustPerfect(projectile.Center, DustID.Blood, Main.rand.NextVector2Circular(2.5f, 2.5f), 180, default, 1.25f);
				}
			}							
		}
	}
}
