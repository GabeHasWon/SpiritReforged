using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.Particle;
using SpiritReforged.Content.Particles;
using Terraria.Audio;

namespace SpiritReforged.Content.Forest.Glyphs.Radiant;

public class RadiantGlyph : GlyphItem
{
	public sealed class RadiantPlayer : ModPlayer
	{
		public float radiantCooldown;

		public override void PreUpdate()
		{
			if (Player.HeldItem.GetGlyph().ItemType == ModContent.ItemType<RadiantGlyph>())
			{
				if (++radiantCooldown > Player.HeldItem.useTime * 3f)
				{
					int radiantBuff = ModContent.BuffType<DivineStrike>();
					if (!Player.HasBuff(radiantBuff))
					{
						ParticleHandler.SpawnParticle(new StarParticle(Player.Center + new Vector2(0, -10 * Player.gravDir), Vector2.Zero, Color.White, Color.Yellow, 0.2f, 10, 0));
						SoundEngine.PlaySound(SoundID.MaxMana, Player.Center);
					}

					Player.AddBuff(radiantBuff, 10);
				}
			}
			else
			{
				radiantCooldown = 0;
			}
		}

		public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
		{
			if (Player.HasBuff(ModContent.BuffType<DivineStrike>()))
			{
				modifiers.FinalDamage *= 1.5f;

				SoundEngine.PlaySound(SoundID.DD2_FlameburstTowerShot with { Volume = 0.4f, Pitch = 0.8f }, target.Center);
				Projectile.NewProjectile(Player.GetSource_OnHit(target), target.Center, Vector2.Zero, ModContent.ProjectileType<RadiantEnergy>(), 0, 0, Player.whoAmI, target.whoAmI);

				for (int i = 0; i < 5; i++)
					ParticleHandler.SpawnParticle(new StarParticle(target.Center, Main.rand.NextVector2Unit() * Main.rand.NextFloat() * 2f, Color.Yellow, Main.rand.NextFloat(0.1f, 0.25f), Main.rand.Next(15, 30), 0.1f));

				Player.ClearBuff(ModContent.BuffType<DivineStrike>());
			}

			radiantCooldown = 0;
		}
	}

	public override void SetDefaults()
	{
		Item.width = Item.height = 28;
		Item.rare = ItemRarityID.LightRed;
		Item.maxStack = Item.CommonMaxStack;
		settings = new(new(234, 167, 51));
	}
}