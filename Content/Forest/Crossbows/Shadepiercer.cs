using SpiritReforged.Common.ProjectileCommon;
using SpiritReforged.Common.ProjectileCommon.Abstract;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Dusts;
using SpiritReforged.Content.Particles;
using System.IO;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ModLoader.IO;

namespace SpiritReforged.Content.Forest.Crossbows;

public class Shadepiercer : ModItem, ReloadPlayer.IPerfectReload
{
	public class ShadeGlobalProjectile : GlobalProjectile
	{
		public override bool InstancePerEntity => true;

		private bool _shaded;

		public void EnableShade(Projectile projectile)
		{
			if (_shaded)
				return;

			_shaded = true;

			projectile.usesLocalNPCImmunity = true;
			projectile.localNPCHitCooldown = 20;
			projectile.penetrate++; //Withstand one additional strike
		}

		public override bool AppliesToEntity(Projectile entity, bool lateInstantiation) => BoltCache.IsCrossbowBolt[entity.type];

		public override bool PreDraw(Projectile projectile, ref Color lightColor)
		{
			if (_shaded)
			{
				DrawHelpers.DrawOutline((offset) =>
				{
					Texture2D texture = TextureAssets.Projectile[projectile.type].Value;
					Vector2 origin = new(texture.Width / 2, projectile.height / 2);

					Main.EntitySpriteDraw(texture, projectile.Center - Main.screenPosition + offset.RotatedBy(Main.timeForVisualEffects / 30f), null, Color.Black * 0.3f, projectile.rotation, origin, projectile.scale, 0);
				});
			}

			return true;
		}

		public override void SendExtraAI(Projectile projectile, BitWriter bitWriter, BinaryWriter binaryWriter) => bitWriter.WriteBit(_shaded);

		public override void ReceiveExtraAI(Projectile projectile, BitReader bitReader, BinaryReader binaryReader)
		{
			if (_shaded = bitReader.ReadBit())
				EnableShade(projectile);
		}
	}

	public class ShadeCloak : ModBuff
	{
		public override void Update(Player player, ref int buffIndex)
		{
			player.aggro = 0;
			player.jumpBoost = true;
			player.moveSpeed += 0.3f;

			int buffTime = player.buffTime[buffIndex];
			float invisStrength = (buffTime <= 2) ? 1 : 0.2f;
			player.opacityForAnimation = invisStrength;
		}
	}

	public class ShadepiercerHeld : Crossbow.CrossbowHeld
	{
		public override LocalizedText DisplayName => ModContent.GetInstance<Shadepiercer>().DisplayName;

		public override string Texture => ModContent.GetInstance<Shadepiercer>().Texture;

		public override IConfiguration SetConfiguration() => new Crossbow.CrossbowConfiguration(null, 30, 10, 40);
	}

	public bool PerfectReload { get; set; }

	void ReloadPlayer.IPerfectReload.OnPerfectReload(Player player)
	{
		player.AddBuff(ModContent.BuffType<ShadeCloak>(), 60 * 5);

		for (int i = 0; i < 8; i++)
		{
			ParticleRenderers.OverPlayers.Add(new CompositeSmoke(Main.rand.NextVector2FromRectangle(player.Hitbox), -Vector2.UnitY, Color.Lerp(Color.Black, Color.Gray, Main.rand.NextFloat()), 30));
			Dust.NewDustPerfect(Main.rand.NextVector2FromRectangle(player.Hitbox), ModContent.DustType<BoneDust>(), Vector2.UnitY * -4f);
		}

		SoundEngine.PlaySound(SoundID.DD2_WyvernDiveDown, player.Center);
	}

	public override void SetDefaults()
    {
		Item.DefaultToBow(50, 10, true);
		Item.UseSound = SoundID.DD2_BallistaTowerShot with { Pitch = 0.5f };
		Item.useAmmo = ModContent.ItemType<Bolt>();
		Item.damage = 10;
		Item.knockBack = 4.5f;
		Item.noUseGraphic = true;
		Item.rare = ItemRarityID.Blue;
	}

	public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
		PerfectReload = player.HasBuff(ModContent.BuffType<ShadeCloak>()); //PerfectReload uniqely only expired when the buff does

		SwungProjectile.Spawn(position, velocity, ModContent.ProjectileType<ShadepiercerHeld>(), damage, knockback, player, 0, source);
		PreNewProjectile.New(source, position, velocity, type, damage, knockback, player.whoAmI, preSpawnAction: (projectile) =>
		{
			if (PerfectReload && projectile.TryGetGlobalProjectile(out ShadeGlobalProjectile global))
				global.EnableShade(projectile);
		});

		return false;
    }

	public override void AddRecipes() => CreateRecipe().AddIngredient(ItemID.Obsidian, 25).AddIngredient(ItemID.Bone, 14).AddIngredient(ItemID.Cobweb, 30).AddTile(TileID.Anvils).Register();
}