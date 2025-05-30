using SpiritReforged.Common.ModCompat;
using SpiritReforged.Common.PlayerCommon;
using SpiritReforged.Common.Visuals;
using System.IO;
using Terraria.Audio;
using static Microsoft.Xna.Framework.MathHelper;
using static SpiritReforged.Common.Easing.EaseFunction;

namespace SpiritReforged.Common.ProjectileCommon.Abstract;

public abstract partial class BaseClubProj(Vector2 textureSize) : ModProjectile
{
	internal const int MAX_FLICKERTIME = 20;

	internal readonly Vector2 Size = textureSize;

	public float DamageScaling { get; private set; }
	public float KnockbackScaling { get; private set; }

	public int ChargeTime { get; private set; }
	public int SwingTime { get; private set; }
	public float MeleeSizeModifier { get; private set; }

	internal int WindupTime => (int)(ChargeTime * WindupTimeRatio / ChargeSpeedMult);
	internal int LingerTime => (int)(SwingTime * LingerTimeRatio);

	public float Charge { get => Projectile.ai[0]; set => Projectile.ai[0] = value; }
	public float AiState { get => Projectile.ai[1]; set => Projectile.ai[1] = value; }
	public float BaseRotation { get => Projectile.ai[2]; set => Projectile.ai[2] = value; }
	public float BaseScale { get => Projectile.scale; set => Projectile.scale = value; }

	protected int _lingerTimer;
	protected int _swingTimer;
	protected int _windupTimer;
	protected int _flickerTime;

	private bool _hasFlickered = false;

	/// <summary><inheritdoc cref="ModProjectile.DisplayName"/><para/>
	/// Automatically attempts to use the associated item localization. </summary>
	public override LocalizedText DisplayName => Language.GetText("Mods.SpiritReforged.Items." + Name.Replace("Proj", string.Empty) + ".DisplayName");
	/// <summary><inheritdoc cref="ModProjectile.Texture"/><para/>
	/// Automatically attempts to use the associated item texture. </summary>
	public override string Texture
	{
		get
		{
			//Return default if the club projectile has its own texture
			if (ModContent.HasAsset(base.Texture))
				return base.Texture;

			string def = base.Texture;
			return def[..^4]; //Remove 'proj'
		}
	}

	public sealed override void SetStaticDefaults()
	{
		ProjectileID.Sets.TrailCacheLength[Type] = 6;
		ProjectileID.Sets.TrailingMode[Type] = 2;

		SafeSetStaticDefaults();
	}

	public sealed override void SetDefaults()
	{
		Projectile.hostile = false;
		Projectile.DamageType = DamageClass.Melee;
		Projectile.width = Projectile.height = 16;
		Projectile.aiStyle = -1;
		Projectile.friendly = true;
		Projectile.penetrate = -1;
		Projectile.tileCollide = false;
		//Projectile.ownerHitCheck = true;
		Projectile.usesLocalNPCImmunity = true;
		Projectile.localNPCHitCooldown = -1;

		MoRHelper.SetHammerBonus(Projectile);
		SafeSetDefaults();
	}

	public override bool? CanDamage() => CheckAIState(AIStates.SWINGING) ? null : false;

	public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
	{
		//Create a new hitbox at the intended tip of the club, and scale it up
		var endPoint = Owner.MountedCenter + Owner.DirectionTo(Projectile.Center) * TotalScale * Size;
		int width = (int)(projHitbox.Width * MeleeSizeModifier);
		int height = (int)(projHitbox.Height * MeleeSizeModifier);
		var newProjHitbox = new Rectangle((int)(endPoint.X - width / 2), (int)(endPoint.Y - height / 2), width, height);

		if (newProjHitbox.Intersects(targetHitbox))
			return true;

		//Do line collision if the target is between the hitbox and the player
		float dummy = 0;
		float lineWidth = Size.Length() / 2;

		return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Owner.MountedCenter, endPoint, lineWidth, ref dummy);
	}
	
	public override void CutTiles()
	{
		//Prevent tile cutting if the projectile isn't allowed to hit anything
		if (CanDamage() is false)
			return;

		//Tile cutting logic adapted from example mod, plots a tile line across the projectile as if it was a laser and cuts tiles that intersect with it
		DelegateMethods.tilecut_0 = TileCuttingContext.AttackProjectile;
		var cut = new Utils.TileActionAttempt(DelegateMethods.CutTiles);
		var endPoint = Owner.MountedCenter + Owner.DirectionTo(Projectile.Center) * MeleeSizeModifier * Size;

		Utils.PlotTileLine(Owner.MountedCenter, endPoint, Projectile.width * MeleeSizeModifier, cut);

		//Additional line plotted between the projectile's current and last position, to catch instances where it moves super fast
		var startCenter = Vector2.Lerp(Projectile.position, Owner.MountedCenter, 0.5f);
		var oldCenter = Vector2.Lerp(Projectile.oldPosition, Owner.MountedCenter, 0.5f);

		Utils.PlotTileLine(startCenter, oldCenter, Projectile.width * MeleeSizeModifier, cut);
	}

	public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
	{
		if (FullCharge)
		{
			modifiers.FinalDamage *= DamageScaling;
			modifiers.Knockback *= KnockbackScaling;
		}

		SafeModifyHitNPC(target, ref modifiers);
	}

	public override void ModifyHitPlayer(Player target, ref Player.HurtModifiers modifiers)
	{
		if (FullCharge)
		{
			modifiers.FinalDamage *= DamageScaling;
			modifiers.Knockback *= KnockbackScaling;
		}

		SafeModifyHitPlayer(target, ref modifiers);
	}

	public sealed override void AI()
	{
		SafeAI();

		if (Owner.dead)
			Projectile.Kill();

		Owner.heldProj = Projectile.whoAmI;
		Owner.direction = Math.Sign(Projectile.direction);

		if (AllowUseTurn && Projectile.owner == Main.myPlayer)
		{
			int newDir = Math.Sign(Main.MouseWorld.X - Owner.Center.X);
			Projectile.velocity.X = newDir == 0 ? Owner.direction : newDir;

			if (newDir != Owner.direction)
				Projectile.netUpdate = true;
		}

		switch (AiState)
		{
			case (float)AIStates.CHARGING: 
				Charging(Owner);
				break;

			case (float)AIStates.SWINGING:
				Swinging(Owner);
				break;

			case (float)AIStates.POST_SMASH:
				AfterCollision();
				break;
		}

		if (!Owner.controlUseItem && _windupTimer >= WindupTime && CheckAIState(AIStates.CHARGING) && AllowRelease)
		{
			SetAIState(AIStates.SWINGING);
			OnSwingStart();

			if (!Main.dedServ)
				SoundEngine.PlaySound(DefaultSwing, Owner.Center);
		}

		TranslateRotation(Owner, out float clubRotation, out float armRotation);
		Projectile.rotation = clubRotation + Owner.fullRotation;

		float rotation = armRotation - PiOver2;
		Projectile.Center = Owner.RotatedRelativePoint(Owner.Center - new Vector2((int)(Math.Cos(rotation) * Size.X), (int)(Math.Sin(rotation) * Size.Y)));

		Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.ThreeQuarters, armRotation);
		Owner.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.ThreeQuarters, armRotation + 0.4f * Owner.direction);
		Owner.itemAnimation = Owner.itemTime = 2;
	}

	public sealed override bool PreDraw(ref Color lightColor)
	{
		Texture2D texture = TextureAssets.Projectile[Type].Value;
		Vector2 handPos = Owner.GetHandRotated(); //GetHandRotated also gets gfxOffY
		Vector2 drawPos = handPos - Main.screenPosition;
		Color drawColor = Projectile.GetAlpha(lightColor);
		Rectangle frame = texture.Frame(1, Main.projFrames[Type], 0, Projectile.frame);

		if (!OverrideDraw(Main.spriteBatch, texture, lightColor, handPos, drawPos))
		{
			//Aftertrail during swing
			float trailOpacity = 1;
			if (AllowedAftertrailDraw(ref trailOpacity))
				DrawAftertrail(texture, lightColor * trailOpacity, drawPos);

			Main.EntitySpriteDraw(texture, drawPos, frame, drawColor, Projectile.rotation, HoldPoint, TotalScale, Effects, 0);

			SafeDraw(Main.spriteBatch, texture, lightColor, handPos, drawPos);

			//Flash when fully charged
			if (CheckAIState(AIStates.CHARGING) && _flickerTime > 0)
			{
				Texture2D flash = TextureColorCache.ColorSolid(texture, Color.White);
				float alpha = EaseQuadIn.Ease(EaseSine.Ease(_flickerTime / (float)MAX_FLICKERTIME));

				Main.EntitySpriteDraw(flash, drawPos, frame, Color.White * alpha, Projectile.rotation, HoldPoint, TotalScale, Effects, 0);
			}
		}

		return false;
	}

	//Multiplayer syncing
	public override void SendExtraAI(BinaryWriter writer)
	{
		writer.Write((Half)DamageScaling);
		writer.Write((Half)KnockbackScaling);
		writer.Write((Half)MeleeSizeModifier);

		writer.Write((ushort)ChargeTime);
		writer.Write((ushort)SwingTime);

		writer.Write((ushort)_lingerTimer);
		writer.Write((ushort)_swingTimer);
		writer.Write((ushort)_windupTimer);

		SendExtraDataSafe(writer);
	}

	public override void ReceiveExtraAI(BinaryReader reader)
	{
		DamageScaling = (float)reader.ReadHalf();
		KnockbackScaling = (float)reader.ReadHalf();
		MeleeSizeModifier = (float)reader.ReadHalf();

		ChargeTime = reader.ReadUInt16();
		SwingTime = reader.ReadUInt16();

		_lingerTimer = reader.ReadUInt16();
		_swingTimer = reader.ReadUInt16();
		_windupTimer = reader.ReadUInt16();

		ReceiveExtraDataSafe(reader);
	}
}