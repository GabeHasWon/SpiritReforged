using SpiritReforged.Common.Easing;
using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.ItemCommon.MagazineSystem;
using SpiritReforged.Common.PlayerCommon;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Particles;
using System.IO;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ModLoader.IO;

namespace SpiritReforged.Content.Underground.Items;

public class LastWord : ModItem, IFlagged
{
	private class LastWordGlobalProjectile : GlobalProjectile
	{
		public override bool InstancePerEntity => true;

		private bool _active;

		public override bool AppliesToEntity(Projectile entity, bool lateInstantiation) => entity.friendly;

		public override void OnSpawn(Projectile projectile, IEntitySource source)
		{
			if (source is EntitySource_ItemUse_WithAmmo sourceWithAmmo)
			{
				Player player = sourceWithAmmo.Player;
				Item item = sourceWithAmmo.Item;

				if (player.HasFlag<LastWord>() && item.TryGetGlobalItem(out ReloadableGlobalItem global) && global.capacity == 0)
					_active = true;
			}
		}

		public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (_active)
			{
				SoundEngine.PlaySound(SoundID.NPCHit8 with { Volume = 0.33f, PitchVariance = 0.33f, Pitch = 0.5f }, projectile.Center);
				for (int i = 0; i < 2; i++)
				{
					ParticleRenderers.UnderProjectiles.Add(new SmokeCloud(projectile.Center + Main.rand.NextVector2Circular(15f, 15f), -Vector2.UnitY, Color.DarkRed * 0.2f, 0.05f, EaseFunction.EaseQuinticOut, 60)
					{
						Pixellate = true,
						PixelDivisor = 3,
					});

					Dust.NewDustPerfect(projectile.Center, DustID.Blood, Main.rand.NextVector2Circular(2.5f, 2.5f), 160, default, 1.35f).noGravity = true;
					Dust.NewDustPerfect(projectile.Center, DustID.Blood, Main.rand.NextVector2Circular(2.5f, 2.5f), 180, default, 1.25f);
				}
			}
		}

		public override void ModifyHitNPC(Projectile projectile, NPC target, ref NPC.HitModifiers modifiers)
		{
			if (_active)
				modifiers.SourceDamage *= 1 + DAMAGE_BONUS;
		}

		public override void SendExtraAI(Projectile projectile, BitWriter bitWriter, BinaryWriter binaryWriter) => bitWriter.WriteBit(_active);
		public override void ReceiveExtraAI(Projectile projectile, BitReader bitReader, BinaryReader binaryReader) => _active = bitReader.ReadBit();
	}

	public const float DAMAGE_BONUS = 0.5f;

	public override void SetDefaults()
	{
		Item.DefaultToAccessory();
		Item.rare = ItemRarityID.Green;
		Item.value = Item.sellPrice(gold: 2);
	}
}