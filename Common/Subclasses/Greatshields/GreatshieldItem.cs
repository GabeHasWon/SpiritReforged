using SpiritReforged.Common.PlayerCommon;
using Terraria.DataStructures;

namespace SpiritReforged.Common.Subclasses.Greatshields;

internal readonly record struct GreatshieldAltInfo(int BoostHealth, int DelayDecay, int ParryTime, int AnimationTime);

internal abstract class GreatshieldItem : ModItem
{
	internal class GreatshieldHitbox : ModProjectile
	{
		public override string Texture => "Terraria/Images/Projectile_0";

		private Player Owner => Main.player[Projectile.owner];

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
			GreatshieldLayer.GetShieldAnimationData(Owner, rotation, out float factor);
			Projectile.Center = Owner.Center + new Vector2(26 * factor, 0).RotatedBy(rotation);
		}

		public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
		{
			modifiers.HitDirectionOverride = Owner.direction;
			modifiers.Knockback *= 2;
		}
	}

	public abstract GreatshieldAltInfo Info { get; }

	public static Dictionary<int, Asset<Texture2D>> ShieldToHeldTexture = [];

	public override void SetStaticDefaults() => ShieldToHeldTexture.Add(Type, ModContent.Request<Texture2D>(Texture + "_Held"));

	public virtual void ModifyLayerDrawing(ref DrawData data, bool isGuard) { }

	public override bool AltFunctionUse(Player player) => true;

	public override bool? UseItem(Player player)
	{
		if (player.altFunctionUse == 2)
		{
			if (player.GetModPlayer<GreatshieldPlayer>().parryTime <= 0)
			{
				player.GetModPlayer<GreatshieldPlayer>().Guard(Info);
				return true;
			}
			else
				return false;
		}

		return null;
	}

	public override void HoldItem(Player player)
	{
		if (Main.myPlayer == player.whoAmI)
		{
			player.ChangeDir(Math.Sign(Main.MouseWorld.X - player.Center.X));

			float rotation = player.AngleTo(PlayerMouseHandler.GetMouse(player.whoAmI)) + MathHelper.PiOver2 + MathHelper.Pi;
			GreatshieldLayer.GetShieldAnimationData(player, rotation, out float factor);

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
}
