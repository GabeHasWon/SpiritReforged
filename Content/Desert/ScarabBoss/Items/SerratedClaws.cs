using SpiritReforged.Common.Easing;
using SpiritReforged.Common.ModCompat;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.PlayerCommon;
using SpiritReforged.Common.PlayerCommon.Interfaces;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Content.Particles;
using Terraria.Audio;

namespace SpiritReforged.Content.Desert.ScarabBoss.Items;

public class SerratedClaws : ModItem
{
	public class SerratedClawsHeld : ModProjectile
	{
		public override string Texture => AssetLoader.EmptyTexture;

		private ref float AnimationTimer => ref Projectile.ai[0];
		private ref float SoundTimer => ref Projectile.ai[1];

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
				SoundTimer++;

				if (SoundTimer > 8 / MathHelper.Lerp(GetSpeedModifier(owner), 1, 0.4f))
				{
					SoundEngine.PlaySound(SoundID.Item1 with { PitchRange = (0.5f, 0.8f), Volume = 0.8f }, Projectile.Center + Projectile.velocity * 4);
					SoundTimer = 0;
				}

				if (owner.HeldItem.type != ModContent.ItemType<SerratedClaws>())
					owner.channel = false;

				owner.ChangeDir(Math.Sign(Projectile.velocity.X));

				Projectile.timeLeft++;
				Projectile.Center = owner.Center + Projectile.velocity;

				float rotation = owner.AngleTo(PlayerMouseHandler.GetMouse(owner.whoAmI)) - MathHelper.PiOver2;
				float time = (AnimationTimer += 0.6f) * 0.9f;

				owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, rotation + MathF.Sin(time) * 0.65f);
				owner.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, rotation + MathF.Cos(time) * 0.65f);

				Vector2 oldVelocity = Projectile.velocity;
				Projectile.velocity = owner.DirectionTo(PlayerMouseHandler.GetMouse(owner.whoAmI)) * 14;
					
				if (Projectile.velocity != oldVelocity)
					Projectile.netUpdate = true; //Sync velocity changes if necessary
			}
		}

		public override bool ShouldUpdatePosition() => false;
	}

	public class SerratedClawsPlayer : ModPlayer, IOnMineTile
	{
		public void OnMineTile(int x, int y, int pickPower, int priorType, bool killed)
		{
			if (Player.HeldItem.ModItem is SerratedClaws && killed)
			{
				Vector2 dir = Player.DirectionFrom(new Vector2(x, y).ToWorldCoordinates());
				Tile tile = Main.tile[x, y];
				tile.TileType = (ushort)priorType;

				Vector2 velocity = dir.RotatedByRandom(0.4f) * Main.rand.NextFloat(0.2f, 1f);
				SpawnSmoke(new Vector2(x, y).ToWorldCoordinates(), velocity, 0.1f, Main.rand.Next(45, 60), EaseFunction.EaseCircularInOut, tile);
				tile.TileType = TileID.Dirt;
			}
		}

		private static void SpawnSmoke(Vector2 position, Vector2 velocity, float scale, int duration, EaseFunction ease, Tile tile)
		{
			var material = TileMaterial.FindMaterial(tile.TileType);
			var hsl = Main.rgbToHsl(material.Color);

			ParticleHandler.SpawnParticle(new SmokeCloud(position, velocity, material.Color, scale, ease, duration)
			{
				Pixellate = true,
				PixelDivisor = 4,
				TertiaryColor = Main.hslToRgb(hsl with { X = hsl.X - 0.1f, Z = 0.5f }),
				Layer = ParticleLayer.AbovePlayer
			});
		}
	}

	private static readonly int[] EquipSlots = new int[2];

	public override void Load()
	{
		EquipSlots[0] = EquipLoader.AddEquipTexture(Mod, Texture + "_Hands", EquipType.HandsOn, this, "SerratedHandsOn");
		EquipSlots[1] = EquipLoader.AddEquipTexture(Mod, Texture + "_Hands", EquipType.HandsOff, this, "SerratedHandsOff");
	}

	public override void SetDefaults()
	{
		Item.damage = 10;
		Item.Size = new Vector2(34, 28);
		Item.useTime = 12;
		Item.useAnimation = 12;
		Item.knockBack = 0.2f;
		Item.DamageType = DamageClass.Melee;
		Item.useTurn = true;
		Item.expert = true;
		Item.value = Item.sellPrice(gold: 1);
		Item.useStyle = ItemUseStyleID.Swing;
		Item.pick = 50;
		Item.noUseGraphic = true;
		Item.noMelee = true;
		Item.channel = true;
		Item.autoReuse = true;
		Item.shoot = ModContent.ProjectileType<SerratedClawsHeld>();

		MoRHelper.SetSlashBonus(Item);
	}

	public override bool MeleePrefix() => true;
	public override void HoldItemFrame(Player player) => DisplayEquips(player);
	public override void UseItemFrame(Player player) => DisplayEquips(player);
	public override bool CanShoot(Player player) => player.ownedProjectileCounts[ModContent.ProjectileType<SerratedClawsHeld>()] == 0; // Only spawn one projectile

	private static void DisplayEquips(Player player)
	{
		player.handon = EquipSlots[0];
		player.handoff = EquipSlots[1];
	}

	public override float UseSpeedMultiplier(Player player) => GetSpeedModifier(player);

	/// <summary>
	/// Helper method for use in the item and the projectile.
	/// </summary>
	public static float GetSpeedModifier(Player player) => player.GetAttackSpeed(DamageClass.Melee) + (1 - player.pickSpeed) * 2.5f;
}