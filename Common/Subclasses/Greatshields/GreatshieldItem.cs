using SpiritReforged.Common.Easing;
using SpiritReforged.Common.PlayerCommon;
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
		if (player.altFunctionUse == 2 && player.TryGetModPlayer(out GreatshieldPlayer shieldPlayer))
		{
			shieldPlayer.Blocking = true; //Start blocking
			return true;
		}

		return null;
	}

	public override bool AltFunctionUse(Player player) => true;

	public override bool CanShoot(Player player) => player.altFunctionUse != 2;

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
		if (guarding)
		{
			float scale = 1f + EaseFunction.EaseSine.Ease((float)Main.timeForVisualEffects / 30f) * 0.3f;
			drawInfo.DrawDataCache.Add(new(texture, position.Floor(), null, color * (1f - (scale - 1f) / 0.4f), rotation, texture.Size() / 2, scale, effects, 0));
		}

		drawInfo.DrawDataCache.Add(new(texture, position.Floor(), null, color, rotation, texture.Size() / 2, 1, effects, 0));
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

	public override void HoldItem(Player player)
	{
		if (player.TryGetModPlayer(out GreatshieldPlayer shieldPlayer) && shieldPlayer.Blocking) //Blocking
		{
			Vector2 mouse = PlayerMouseHandler.GetMouse(player.whoAmI);

			player.ChangeDir(Math.Sign(mouse.X - player.Center.X));
			player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, player.AngleTo(mouse) - MathHelper.PiOver2);
		}
	}
}