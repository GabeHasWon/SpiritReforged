using SpiritReforged.Common.ProjectileCommon;
using SpiritReforged.Common.ProjectileCommon.Abstract;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Particles;
using System.IO;
using Terraria.DataStructures;
using Terraria.ModLoader.IO;

namespace SpiritReforged.Content.Forest.Crossbows;

public class Bolter : ModItem, ReloadPlayer.IPerfectReload
{
	public class EmbedGlobalProjectile : GlobalProjectile
	{
		public override bool InstancePerEntity => true;

		private bool _fromBolter;

		public void EnableBleed(Projectile projectile)
		{
			if (_fromBolter)
				return;

			_fromBolter = true;
			projectile.penetrate++; //Withstand an additional strike
		}

		public override bool AppliesToEntity(Projectile entity, bool lateInstantiation) => BoltCache.IsCrossbowBolt[entity.type];

		public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (_fromBolter)
			{
				target.AddBuff(BuffID.Bleeding, 60 * 5);

				for (int i = 0; i < 10; i++)
					Dust.NewDustPerfect(projectile.Center, DustID.Blood, (projectile.velocity * -Main.rand.NextFloat(0.2f, 0.5f)).RotatedByRandom(0.5f));

				Vector2 offset = target.Center - projectile.Center;
				projectile.velocity = offset;
				projectile.hide = true;

				ParticleRenderers.OverNPCs.Add(new BloodHit(target, offset, 20, offset.ToRotation(), 1));

				if (projectile.ModProjectile is Bolt.BoltProjectile boltProjectile)
					boltProjectile.Target = target; //Force a target
			}
		}

		public override void SendExtraAI(Projectile projectile, BitWriter bitWriter, BinaryWriter binaryWriter) => bitWriter.WriteBit(_fromBolter);

		public override void ReceiveExtraAI(Projectile projectile, BitReader bitReader, BinaryReader binaryReader)
		{
			if (_fromBolter = bitReader.ReadBit())
				EnableBleed(projectile);
		}
	}

	public class BolterHeld : Crossbow.CrossbowHeld
	{
		public override LocalizedText DisplayName => ModContent.GetInstance<Bolter>().DisplayName;

		public override string Texture => ModContent.GetInstance<Bolter>().Texture;

		public override IConfiguration SetConfiguration() => new Crossbow.CrossbowConfiguration(null, 30, 10, 40);
	}

	public bool PerfectReload { get; set; }

	public override void SetDefaults()
    {
		Item.DefaultToBow(20, 10, true);
		Item.UseSound = SoundID.DD2_BallistaTowerShot with { Pitch = 0.5f };
		Item.useAmmo = ModContent.ItemType<Bolt>();
		Item.damage = 10;
		Item.knockBack = 4.5f;
		Item.noUseGraphic = true;
		Item.rare = ItemRarityID.Blue;
	}

	public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
		SwungProjectile.Spawn(position, velocity, ModContent.ProjectileType<BolterHeld>(), damage, knockback, player, 0, source);
		PreNewProjectile.New(source, position, velocity, type, damage, knockback, player.whoAmI, preSpawnAction: (projectile) =>
		{
			if (PerfectReload && projectile.TryGetGlobalProjectile(out EmbedGlobalProjectile global))
				global.EnableBleed(projectile);
		});

		PerfectReload = false;
		return false;
    }
}