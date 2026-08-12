using SpiritReforged.Common.PlayerCommon;
using Terraria.DataStructures;

namespace SpiritReforged.Common.Subclasses.Greatshields;

internal readonly record struct GreatshieldAltInfo(int BoostHealth, int DelayDecay, int ParryTime, int AnimationTime);

internal abstract class GreatshieldItem : ModItem
{
	internal class GreatshieldHitbox : ModProjectile
	{
		private Player Owner => Main.player[Projectile.owner];

		public bool Release
		{
			get => Projectile.ai[0] == 1;
			set => Projectile.ai[0] = value ? 1 : 0;
		}

		private ref float TimeLeft => ref Projectile.ai[1];

		public override void SetDefaults()
		{
			Projectile.Size = new Vector2(44);
			Projectile.timeLeft = 60;
			Projectile.aiStyle = -1;
			Projectile.friendly = true;
			Projectile.hostile = false;
			Projectile.penetrate = -1;
			Projectile.tileCollide = false;
			Projectile.localNPCHitCooldown = -1;
			Projectile.usesLocalNPCImmunity = true;
		}

		public override void AI()
		{
			if (Owner.ItemTimeIsZero)
			{
				Projectile.Kill();
				return;
			}

			float rotation = Owner.AngleTo(PlayerMouseHandler.GetMouse(Owner.whoAmI));
			GreatshieldLayer.GetShieldAnimationData(Owner, rotation, out float factor, out bool throwingOut);
			Projectile.rotation = rotation + MathHelper.Pi;

			if (!Release)
			{
				Projectile.Opacity = factor;

				if (Owner.itemTime <= 8)
				{
					Projectile.Opacity = Owner.itemTime / 8f;
				}
			}

			if (Release)
			{
				if (!throwingOut)
				{
					return;
				}

				Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.Pi;
				Projectile.velocity *= 0.9f;
				Projectile.Opacity = Projectile.velocity.Length() / 4f;

				return;
			}

			Projectile.Center = Owner.Center + new Vector2(26 * factor, 0).RotatedBy(rotation);
		}

		public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
		{
			modifiers.HitDirectionOverride = Owner.direction;
			modifiers.Knockback *= 2;
		}

		public override bool ShouldUpdatePosition()
		{
			GreatshieldLayer.GetShieldAnimationData(Owner, 0, out _, out bool throwingOut);
			return Release && throwingOut;
		}
	}

	public abstract GreatshieldAltInfo Info { get; }
	public abstract bool Release { get; }

	public static Dictionary<int, Asset<Texture2D>> ShieldToHeldTexture = [];

	public override void SetStaticDefaults() => ShieldToHeldTexture.Add(Type, ModContent.Request<Texture2D>(Texture + "_Held"));

	public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
	{
		Projectile.NewProjectile(source, position, player.DirectionTo(Main.MouseWorld) * 8, type, damage, knockback, player.whoAmI, Release ? 1 : 0);
		return false;
	}

	public virtual void ModifyLayerDrawing(ref DrawData data, bool isGuard) { }

	public override bool AltFunctionUse(Player player) => true;

	public override bool? UseItem(Player player)
	{
		GreatshieldPlayer shieldPlr = player.GetModPlayer<GreatshieldPlayer>();

		if (shieldPlr.parryAnim > 0)
			return false;

		if (player.altFunctionUse == 2)
		{
			if (shieldPlr.parryTime <= 0)
			{
				shieldPlr.Guard(Info);
				return true;
			}

			return false;
		}

		return null;
	}

	public override bool CanShoot(Player player) => player.altFunctionUse != 2;

	public override void HoldItem(Player player)
	{
		player.GetModPlayer<GreatshieldPlayer>().lastBlockHook = OnBlock;

		if (Main.myPlayer == player.whoAmI)
		{
			player.ChangeDir(Math.Sign(Main.MouseWorld.X - player.Center.X));

			float rotation = player.AngleTo(PlayerMouseHandler.GetMouse(player.whoAmI)) + MathHelper.PiOver2 + MathHelper.Pi;
			GreatshieldLayer.GetShieldAnimationData(player, rotation, out float factor, out _);

			player.SetCompositeArmBack(true, FactorToStretch(factor), rotation);
			int parry = player.GetModPlayer<GreatshieldPlayer>().parryAnim;

			if (parry > 0)
				player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, rotation);
		}
	}

	private static Player.CompositeArmStretchAmount FactorToStretch(float factor) => factor switch
	{
		< 0 => Player.CompositeArmStretchAmount.None,
		< 0.3f => Player.CompositeArmStretchAmount.Quarter,
		< 0.6f => Player.CompositeArmStretchAmount.ThreeQuarters,
		_ => Player.CompositeArmStretchAmount.Full
	};

	public virtual void OnBlock(Player player, Player.HurtInfo info) { }
}
