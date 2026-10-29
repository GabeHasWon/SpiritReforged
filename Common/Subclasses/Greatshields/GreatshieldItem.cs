using SpiritReforged.Common.Easing;
using SpiritReforged.Common.PlayerCommon;
using SpiritReforged.Common.ProjectileCommon.Abstract;
using Terraria.DataStructures;

namespace SpiritReforged.Common.Subclasses.Greatshields;

public abstract class GreatshieldItem : ModItem
{
	public readonly record struct ShieldInfo(int ShieldHealth, int DelayTime)
	{
		/// <summary> Gets the final shield health value modified by <paramref name="player"/> stats. </summary>
		public readonly int GetShieldHealth(Player player) => player.TryGetModPlayer(out GreatshieldPlayer shieldPlayer) ? (int)shieldPlayer.ShieldHealthStat.ApplyTo(ShieldHealth) : ShieldHealth;
	}

	/// <summary> Contains shield held textures by item type. </summary>
	private static readonly Dictionary<int, Asset<Texture2D>> TextureByType = [];

	public Texture2D HeldTexture => TextureByType[Type].Value;
	public ShieldInfo Info { get; private set; }
	public enum DrawLayer { FrontArm, BackArm }

	public virtual DrawLayer Layer { get; private set; } = DrawLayer.FrontArm;

	public override void SetStaticDefaults() => TextureByType.Add(Type, ModContent.Request<Texture2D>(Texture + "_Held"));

	public sealed override void SetDefaults()
	{
		Item.DamageType = ModContent.GetInstance<GreatshieldClass>();
		Item.channel = true;
		Item.useTime = Item.useAnimation = 30;
		Item.UseSound = SoundID.Item1;
		Item.noMelee = true;
		Item.noUseGraphic = true;
		Item.useStyle = -1;
		Item.shootSpeed = 1;

		Info = SetInfo();
	}

	public abstract ShieldInfo SetInfo();

	public virtual void OnBlockDamage(Player player, Player.HurtInfo info) { }

	public override bool? UseItem(Player player)
	{
		if (player.TryGetModPlayer(out GreatshieldPlayer shieldPlayer))
		{
			//SoundEngine.PlaySound(SoundID.Item36 with { Volume = 0.2f, Pitch = 1 }, player.Center); //Play blocking sound
			shieldPlayer.Blocking = true; //Start blocking
			return true;
		}

		return null;
	}

	public virtual void DrawShield(ref PlayerDrawSet drawInfo, bool guarding)
	{
		const int jump_frame = 5;

		Player player = drawInfo.drawPlayer;
		Texture2D texture = HeldTexture;
		Color color = Lighting.GetColor(player.Center.ToTileCoordinates());
		SpriteEffects effects = drawInfo.playerEffect;

		Vector2 offhand = GetOffhand(player, out int frame) + new Vector2(9 * player.direction, 3);
		Vector2 halfSize = player.Size / 2;

		float rotation = drawInfo.rotation;
		if (guarding)
		{
			rotation = player.AngleTo(PlayerMouseHandler.GetMouse(player.whoAmI)) + (player.direction == -1 ? MathHelper.Pi : 0);
			player.bodyFrame.Y = 0;
		}
		else if (frame == jump_frame)
		{
			rotation -= 0.3f * player.direction;
		}

		Vector2 position = drawInfo.Position + halfSize + (offhand - halfSize).RotatedBy(rotation) - Main.screenPosition;
		drawInfo.DrawDataCache.Add(new(texture, position.Floor(), null, color, rotation, texture.Size() / 2, 1, effects, 0));

		if (guarding)
		{
			float scale = 1 + (EaseFunction.EaseSine.Ease((float)Main.timeForVisualEffects / 30f) + 1) * 0.1f;
			drawInfo.DrawDataCache.Add(new(texture, position.Floor(), null, color * 0.3f, rotation, texture.Size() / 2, scale, effects, 0));
		}
	}

	public static Vector2 GetOffhand(Player player, out int frame)
	{
		Vector2 offhand = Main.OffsetsPlayerOffhand[frame = player.bodyFrame.Y / player.bodyFrame.Height];

		if (player.direction != 1)
			offhand.X = player.width - offhand.X;

		if (player.gravDir != 1f)
			offhand.Y -= player.height;

		return offhand;
	}

	/*public override void HoldItem(Player player)
	{
		if (player.TryGetModPlayer(out GreatshieldPlayer shieldPlayer) && shieldPlayer.Blocking) //Blocking
		{
			Vector2 mouse = PlayerMouseHandler.GetMouse(player.whoAmI);

			player.ChangeDir(Math.Sign(mouse.X - player.Center.X));
			player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, player.AngleTo(mouse) - MathHelper.PiOver2);
		}
	}*/
}

public abstract class GreatshieldProjectile : SwungProjectile
{
	public override string Texture => AssetLoader.EmptyTexture;

	public bool Released { get; set; }

	public override void AI()
	{
		Player owner = Main.player[Projectile.owner];

		Projectile.spriteDirection = Projectile.direction = owner.direction = (Projectile.velocity.X > 0) ? 1 : -1;
		Projectile.rotation = GetRotation(out float armRotation, out Player.CompositeArmStretchAmount stretch);
		Projectile.Center = owner.GetFrontHandPosition(stretch, armRotation);

		owner.SetCompositeArmFront(true, stretch, armRotation);
		owner.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - MathHelper.PiOver2);
		owner.heldProj = Projectile.whoAmI;

		if (Released)
		{
			Counter++; //Progress the use counter
		}
		else
		{
			if (!owner.channel)
				Released = true;

			if (Main.myPlayer == owner.whoAmI)
			{
				Projectile.velocity = owner.DirectionTo(Main.MouseWorld);
				Projectile.netUpdate = true;
			}
		}

		if (Counter < SwingTime - 2)
			owner.itemAnimation = owner.itemTime = Projectile.timeLeft = 2;
	}

	public override bool? CanDamage() => Released ? null : false;

	public override bool PreDraw(ref Color lightColor)
	{
		DrawShieldProjectile(this, lightColor, Released);
		return false;
	}

	public static void DrawShieldProjectile(SwungProjectile swung, Color lightColor, bool wave = false)
	{
		Projectile projectile = swung.Projectile;
		int direction = projectile.direction;
		Color color = projectile.GetAlpha(lightColor);
		BasicConfiguration config = swung.GetConfig<BasicConfiguration>();

		if (Main.player[projectile.owner].HeldItem.ModItem is GreatshieldItem shieldItem)
		{
			Texture2D texture = shieldItem.HeldTexture;
			SpriteEffects effects = (direction == -1) ? SpriteEffects.FlipVertically : SpriteEffects.None;
			float rotation = projectile.rotation + EaseFunction.EaseSine.Ease(swung.Progress * 2) * 0.2f * projectile.direction;

			Vector2 position = projectile.Center - Main.screenPosition + new Vector2(0, projectile.gfxOffY)
				+ (Vector2.UnitX * (config.Reach / 1.5f) * (EaseFunction.EaseSine.Ease(swung.Progress) + 0.25f)).RotatedBy(projectile.rotation);

			Main.EntitySpriteDraw(texture, position, null, color, rotation, texture.Size() / 2, projectile.scale, effects);
		}

		if (wave)
		{
			Main.instance.LoadProjectile(ProjectileID.DD2SquireSonicBoom);

			Texture2D waveTexture = TextureAssets.Projectile[ProjectileID.DD2SquireSonicBoom].Value;
			Vector2 wavePosition = projectile.Center - Main.screenPosition + (Vector2.UnitX * config.Reach * swung.Progress).RotatedBy(projectile.rotation);

			Main.EntitySpriteDraw(waveTexture, wavePosition, null, color * (1f - swung.Progress) * 0.3f, projectile.rotation + MathHelper.PiOver2,
				waveTexture.Size() / 2, new Vector2(0.7f + swung.Progress * 0.3f, 1f - swung.Progress * 0.3f) * projectile.scale, 0);
		}
	}
}