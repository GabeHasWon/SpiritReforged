using SpiritReforged.Common.Easing;
using SpiritReforged.Common.ModCompat;
using SpiritReforged.Common.Particle;
using SpiritReforged.Content.Ocean.Items.Reefhunter.Particles;
using SpiritReforged.Content.Ocean.Items.Reefhunter.Projectiles;
using SpiritReforged.Content.Particles;
using Terraria.Audio;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Ocean.Items.Reefhunter;

public class ClawCannon : ModItem
{
	public override void SetStaticDefaults() => MoRHelper.AddElement(Item, MoRHelper.Water, true);
	public override void SetDefaults()
	{
		Item.damage = 15;
		Item.width = 38;
		Item.height = 26;
		Item.useTime = Item.useAnimation = 30;
		Item.useStyle = ItemUseStyleID.Shoot;
		Item.knockBack = 4;
		Item.value = Item.sellPrice(0, 2, 0, 0);
		Item.rare = ItemRarityID.Blue;
		Item.crit = 6;
		Item.autoReuse = true;
		Item.noMelee = true;
		Item.DamageType = DamageClass.Ranged;
		Item.shootSpeed = 15f;
		Item.UseSound = SoundID.Item85;
		Item.shoot = ModContent.ProjectileType<Cannonbubble>();
	}

	public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
	{
		if (!Main.dedServ)
		{
			SoundEngine.PlaySound(new SoundStyle("SpiritReforged/Assets/SFX/Item/Woosh_1") with { PitchVariance = 0.4f, Pitch = -1.5f, Volume = 1.2f, MaxInstances = 3 }, player.Center);

			PulseCircle[] pulseCircles =
			[
				new PulseCircle(position + velocity, Cannonbubble.RINGCOLOR, Cannonbubble.RINGCOLOR * 0.5f, 0.5f, 80, 60, EaseFunction.EaseCircularOut),
				new PulseCircle(position + velocity * 1.5f, Cannonbubble.RINGCOLOR, Cannonbubble.RINGCOLOR * 0.5f, 0.5f, 110, 60, EaseFunction.EaseCircularOut),
			];

			for (int i = 0; i < pulseCircles.Length; i++)
			{
				pulseCircles[i].Velocity = 0.5f * Vector2.Normalize(velocity) / (1 + 2 * i);
				ParticleHandler.SpawnParticle(pulseCircles[i].WithSkew(0.85f, velocity.ToRotation() - MathHelper.Pi).UsesLightColor());
			}

			for (int i = 0; i < 4; ++i)
				ParticleHandler.SpawnParticle(new BubbleParticle(position + velocity + player.velocity / 2, Vector2.Normalize(velocity).RotatedByRandom(MathHelper.Pi / 6) * Main.rand.NextFloat(2f, 4), Main.rand.NextFloat(0.2f, 0.4f), 40));
		}

		return true;
	}

	public override Vector2? HoldoutOffset() => new Vector2(-10, 0);

	public override void AddRecipes() => CreateRecipe().AddIngredient(ModContent.ItemType<MineralSlag>(), 14)
		.AddRecipeGroup("Shells", 2).AddTile(TileID.Anvils).Register();
}
