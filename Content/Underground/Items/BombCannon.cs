using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.PlayerCommon;
using SpiritReforged.Common.ProjectileCommon;
using SpiritReforged.Common.Visuals.Glowmasks;
using SpiritReforged.Content.Particles;
using SpiritReforged.Content.Underground.Items.BigBombs;
using System.IO;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ModLoader.IO;

namespace SpiritReforged.Content.Underground.Items;

[AutoloadGlowmask("255,255,255")]
public class BombCannon : ModItem
{
	public override void SetStaticDefaults() => AmmoDatabase.RegisterAmmo(ItemID.Bomb, ItemID.Bomb, ItemID.StickyBomb, ItemID.BouncyBomb, ItemID.BombFish);
	public override void SetDefaults()
	{
		Item.width = 44;
		Item.height = 48;
		Item.useTime = Item.useAnimation = 30;
		Item.useStyle = ItemUseStyleID.Shoot;
		Item.noMelee = true;
		Item.noUseGraphic = true;
		Item.value = Item.sellPrice(0, 1, 0, 0);
		Item.rare = ItemRarityID.Blue;
		Item.autoReuse = true;
		Item.useAmmo = ItemID.Bomb;
		Item.shoot = ModContent.ProjectileType<BombCannonHeld>();
		Item.channel = true;
		Item.shootSpeed = 8f;
	}

	public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
	{
		if (player.HasAccessory<BoomShroom>())
			type = BoomShroomPlayer.MakeLarge(type);

		Projectile.NewProjectile(source, position, velocity, Item.shoot, 0, 0, player.whoAmI, type);
		return false;
	}

	public override void AddRecipes() => CreateRecipe().AddRecipeGroup(RecipeGroupID.IronBar, 8)
		.AddIngredient(ItemID.Timer3Second).AddTile(TileID.Anvils).Register();
}

[AutoloadGlowmask("255,255,255", false)]
internal class BombCannonHeld : ModProjectile
{
	private const int ChargeTimeMax = 60 * 3;

	public int ShootType
	{
		get => (int)Projectile.ai[0];
		set => Projectile.ai[0] = value;
	}

	public ref float Charge => ref Projectile.ai[1];
	public float Progress => (float)Charge / ChargeTimeMax;

	public override string Texture => ModContent.GetInstance<BombCannon>().Texture;
	public override LocalizedText DisplayName => Language.GetText("Mods.SpiritReforged.Items.BombCannon.DisplayName");

	private bool _released;

	public override void SetDefaults()
	{
		Projectile.ignoreWater = true;
		Projectile.tileCollide = false;
		Projectile.timeLeft = 2;
	}

	public override void AI()
	{
		const int wait = 8;
		const int stagger = 10;
		var owner = Main.player[Projectile.owner];

		if (!_released)
		{
			if (!owner.channel || Progress == 1)
			{
				if (Progress > .3f) //Only fire if charged enough
					Fire();

				_released = true;
			}

			if (owner.whoAmI == Main.myPlayer) //Adjust to cursor direction
			{
				Projectile.velocity = (Vector2.UnitX * Projectile.velocity.Length()).RotatedBy(owner.AngleTo(Main.MouseWorld));
				Projectile.netUpdate = true;
			}

			if (Charge == ChargeTimeMax / 3)
			{
				var start = Projectile.Center + new Vector2(8, 10 * Projectile.direction).RotatedBy(Projectile.rotation);

				ParticleHandler.SpawnParticle(new ImpactLine(start, Vector2.Zero, Color.Red.Additive(), new Vector2(1, 2), 10, Projectile));
				ParticleHandler.SpawnParticle(new ImpactLine(start, Vector2.Zero, (Color.White * .3f).Additive(), new Vector2(1, 2) * .7f, 10, Projectile));

				ParticleHandler.SpawnParticle(new PulseCircle(Projectile, Color.Red, .1f, 50, 15, Common.Easing.EaseFunction.EaseQuadOut, start));
				SoundEngine.PlaySound(SoundID.MenuTick with { Pitch = Progress }, Projectile.Center);
			}

			Charge = Math.Min(Charge + 1, ChargeTimeMax);
			Projectile.timeLeft = stagger + wait;
		}

		int holdDistance = 20;
		float rotation = Projectile.velocity.ToRotation();

		if (_released) //Handle recoil visuals after firing
		{
			if (Progress > .3f)
			{
				float sProgress = Math.Clamp((Projectile.timeLeft - wait) / (float)stagger, 0, 1);

				holdDistance -= (int)(sProgress * 8f);
				rotation -= .4f * Projectile.direction * sProgress;
			}
		}

		Projectile.direction = Projectile.spriteDirection = owner.direction;
		var position = owner.MountedCenter + new Vector2(holdDistance, 5 * -Projectile.direction).RotatedBy(rotation);

		Projectile.Center = owner.RotatedRelativePoint(position);
		Projectile.rotation = rotation;

		owner.heldProj = Projectile.whoAmI;
		owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - 1.57f + .4f * owner.direction);
		owner.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - 1.57f + .4f * owner.direction);
		owner.direction = (Projectile.velocity.X < 0) ? -1 : 1;
		owner.itemAnimation = owner.itemTime = 2;
	}

	private void Fire()
	{
		if (Projectile.owner == Main.myPlayer)
		{
			var velocity = Projectile.velocity * Progress;
			PreNewProjectile.New(Projectile.GetSource_FromAI(), Projectile.Center, velocity, ShootType, owner: Projectile.owner, preSpawnAction: delegate (Projectile p)
			{
				p.timeLeft = (int)(p.timeLeft * (1f - Progress));
				p.GetGlobalProjectile<NullBombProjectile>().noExplode = true;
			});
		}

		var unit = Vector2.Normalize(Projectile.velocity);
		var muzzle = Projectile.Center + unit * 28f;

		for (int i = 0; i < 12; i++)
		{
			float mag = Main.rand.NextFloat();
			Dust.NewDustPerfect(muzzle, DustID.Torch, (unit * 10f * mag).RotatedByRandom(.5f * (1f - mag)), Scale: Main.rand.NextFloat(.5f, 2f)).noGravity = true;

			if (Main.rand.NextBool())
			{
				float scale = Main.rand.NextFloat(.5f, 1f);
				var velocity = (unit * Main.rand.NextFloat(3f)).RotatedByRandom(.5f);

				ParticleHandler.SpawnParticle(new GlowParticle(muzzle, velocity, Color.OrangeRed, scale, 20, 5));
				ParticleHandler.SpawnParticle(new GlowParticle(muzzle, velocity, Color.White, scale * .5f, 20, 5));
			}
		}

		SoundEngine.PlaySound(SoundID.Item61 with { Pitch = Progress / 2 }, Projectile.Center);
		_released = true;
	}

	public override bool ShouldUpdatePosition() => false;
	public override bool? CanCutTiles() => false;
	public override bool? CanDamage() => false;

	public override bool PreDraw(ref Color lightColor)
	{
		var texture = TextureAssets.Projectile[Type].Value;
		var glowmask = GlowmaskProjectile.ProjIdToGlowmask[Type].Glowmask.Value;

		var position = new Vector2((int)(Projectile.Center.X - Main.screenPosition.X), (int)(Projectile.Center.Y - Main.screenPosition.Y));
		var effects = (Projectile.spriteDirection == -1) ? SpriteEffects.FlipVertically : SpriteEffects.None;

		int width = 32 + (int)(6 * (Progress / .33f));
		var glowFrame = new Rectangle(0, 0, width, texture.Height);

		Main.EntitySpriteDraw(texture, position, null, Projectile.GetAlpha(lightColor), Projectile.rotation, texture.Size() / 2, Projectile.scale, effects);
		Main.EntitySpriteDraw(glowmask, position, glowFrame, Projectile.GetAlpha(Color.White), Projectile.rotation, texture.Size() / 2, Projectile.scale, effects);
		return false;
	}
}

/// <summary> Prevents <see cref="Projectile.ExplodeTiles"/> from exploding tiles when <see cref="noExplode"/> is <see cref="true"/>. </summary>
internal class NullBombProjectile : GlobalProjectile
{
	public override bool InstancePerEntity => true;
	public override bool AppliesToEntity(Projectile entity, bool lateInstantiation) => ProjectileID.Sets.Explosive[entity.type];

	public bool noExplode;

	public override void Load() => On_Projectile.ExplodeTiles += PreventTileDamage;
	private static void PreventTileDamage(On_Projectile.orig_ExplodeTiles orig, Projectile self, Vector2 compareSpot, int radius, int minI, int maxI, int minJ, int maxJ, bool wallSplode)
	{
		if (self.GetGlobalProjectile<NullBombProjectile>().noExplode)
			return;

		orig(self, compareSpot, radius, minI, maxI, minJ, maxJ, wallSplode);
	}

	public override void SendExtraAI(Projectile projectile, BitWriter bitWriter, BinaryWriter binaryWriter) => bitWriter.WriteBit(noExplode);
	public override void ReceiveExtraAI(Projectile projectile, BitReader bitReader, BinaryReader binaryReader) => noExplode = bitReader.ReadBit();
}