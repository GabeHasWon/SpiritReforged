using SpiritReforged.Common.Easing;
using SpiritReforged.Common.ItemCommon.Abstract;
using SpiritReforged.Common.ItemCommon.MagazineSystem;
using SpiritReforged.Common.ProjectileCommon.Abstract;
using SpiritReforged.Content.Underground.Items;
using Terraria.Audio;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Evil.Items;

public class DuelingPistols : ReloadableItem
{
	public class DuelingPistolHeld : SwungProjectile
	{
		public static readonly PolynomialEase Easing = new(x => (float)(1 + (1.7 + 1) * Math.Pow(x - 1, 3) + 1.7 * Math.Pow(x - 1, 2)));

		public int Style
		{
			get => (int)Projectile.ai[0];
			set => Projectile.ai[0] = value;
		}

		public override LocalizedText DisplayName => ModContent.GetInstance<DuelingPistols>().DisplayName;

		public override string Texture => ModContent.GetInstance<DuelingPistols>().Texture;

		public override IConfiguration SetConfiguration()
		{
			PauseUseAnimation = false;
			return new BasicConfiguration(Easing, 50, 10);
		}

		public override bool PreDraw(ref Color lightColor)
		{
			Texture2D texture = TextureAssets.Projectile[Type].Value;
			SpriteEffects effects = (Projectile.spriteDirection == -1) ? SpriteEffects.FlipVertically : SpriteEffects.None;
			Rectangle source = texture.Frame(1, 2, 0, Style, 0, -2);
			Vector2 origin = new(12, (effects == SpriteEffects.FlipVertically) ? 6 : source.Height - 6); //The handle
			Vector2 position = Projectile.Center - Main.screenPosition + new Vector2(0, Projectile.gfxOffY);

			Color color = Projectile.GetAlpha(lightColor);
			if (Style == 0)
				Desaturate(ref color);

			Main.EntitySpriteDraw(texture, position, source, color, Projectile.rotation, origin, Projectile.scale, effects);

			if (SwingArc != 0) //Firing and has recoil
			{
				Texture2D flash = Mac10.FlashTexture.Value; //Draw muzzle flash
				Vector2 end = Main.player[Projectile.owner].MountedCenter + Vector2.Normalize(Projectile.velocity) * (source.Width - 8) - Main.screenPosition;
				float scale = Math.Max(1f - Progress * 3, 0);

				Main.EntitySpriteDraw(flash, end, null, Projectile.GetAlpha(Color.White), Projectile.velocity.ToRotation(), new Vector2(0, flash.Height / 2), scale * Projectile.scale, effects);
			}

			return false;
		}

		public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
		{
			if (Style == 0)
				behindNPCsAndTiles.Add(index);
		}

		public static void Desaturate(ref Color color, float strength = 1f)
		{
			Vector3 hsl = Main.rgbToHsl(color);
			hsl.Y -= 0.5f * strength;
			hsl.Z -= 0.2f * strength;

			color = Main.hslToRgb(hsl);
		}
	}

	public const float SAVE_AMMO_CHANCE = 0.2f;

	public override IReloadable.MagazineInfo Magazine => new(12, 100);

	public override InterfaceProfile? Profile => new(MagazinePlayer.StyleID.Bullet);

	private bool _backHand;

	public override void SetStaticDefaults() => Main.RegisterItemAnimation(Item.type, new DrawAnimationVertical(2, 2) { NotActuallyAnimating = true });

	public override void SetDefaults()
	{
		Item.DefaultToBow(12, 10, true);
		Item.UseSound = new SoundStyle("SpiritReforged/Assets/SFX/Item/Subclasses/Shotguns/ShotgunShoot", 3);
		Item.useAmmo = AmmoID.Bullet;
		Item.damage = 20;
	}

	public override bool? UseItem(Player player)
	{
		if (!Main.dedServ) //Spawn dusts on shoot
		{
			Vector2 endPosition = player.MountedCenter + new Vector2(48, 0).RotatedBy(ReloadInfo.LastRotation);
			Vector2 unit = player.MountedCenter.DirectionTo(endPosition);

			for (int i = 0; i < 5; i++)
				Dust.NewDustPerfect(endPosition, Main.rand.NextFromList(DustID.Smoke, DustID.Torch), unit * Main.rand.NextFloat(), 150, Scale: Main.rand.NextFloat(1, 2)).noGravity = true;
		}

		_backHand = !_backHand; //Switch hands
		return null;
	}

	public override bool CanConsumeAmmo(Item ammo, Player player) => Main.rand.NextFloat() > SAVE_AMMO_CHANCE; //20% chance to not consume ammo

	public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
	{
		SwungProjectile.Spawn(position, velocity, ModContent.ProjectileType<DuelingPistolHeld>(), damage, knockback, player, _backHand ? -0.5f : 0, source, 0);
		SwungProjectile.Spawn(position, velocity, ModContent.ProjectileType<DuelingPistolHeld>(), damage, knockback, player, _backHand ? 0 : - 0.5f, source, 1);

		return true;
	}

	public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
	{
		Texture2D texture = TextureAssets.Item[Type].Value;

		int index = Main.LocalPlayer.selectedItem;
		float parallax = (index < Main.hotbarScale.Length && Main.LocalPlayer.inventory[index] == Item) ? (Main.hotbarScale[index] - 0.75f) / 0.25f : 0;

		Color backColor = drawColor;
		DuelingPistolHeld.Desaturate(ref backColor, 0.5f);

		if (parallax != 0)
			spriteBatch.Draw(texture, position - new Vector2(8) * scale * parallax, texture.Frame(1, 2, 0, 0, 0, -2), backColor, 0, origin, scale, 0, 0);

		spriteBatch.Draw(texture, position + new Vector2(8) * scale * parallax, texture.Frame(1, 2, 0, 1, 0, -2), drawColor, 0, origin, scale, 0, 0);

		return false;
	}

	public override bool ModifyItemDraw(ref PlayerDrawSet drawInfo, ref DrawData drawData, ref DrawData? coloredDrawData, ref DrawData? glowMaskDrawData)
	{
		Player player = drawInfo.drawPlayer;
		Vector2 origin = new(2, 24);

		if (ReloadInfo.IsReloading(player) && Item.TryGetGlobalItem(out ReloadableGlobalItem global)) //Draw reload visuals
		{
			float progress = 1f - global.reloadTime / (float)Magazine.GetReloadDuration(player);
			Rectangle frontSource = drawData.texture.Frame(1, 2, 0, 1, 0, -2);
			Rectangle backSource = drawData.texture.Frame(1, 2, 0, 0, 0, -2);

			if (progress < 0.5f) //Toss the front weapon
			{
				drawInfo.DrawDataCache.Add(GetItemData(drawData with { sourceRect = frontSource }, player, progress * 2));
				player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, -(MathHelper.PiOver2 + 0.5f) * player.direction);
			}
			else //Toss the back weapon
			{
				if (progress is > 0.54f and < 0.96f)
				{
					Color color = drawData.color;
					DuelingPistolHeld.Desaturate(ref color);

					drawInfo.DrawDataCache.Add(GetItemData(drawData with { sourceRect = backSource, color = color }, player, (progress - 0.5f) * 2));
				}

				float rotation = drawData.rotation + EaseFunction.EaseInOutBack().Ease(progress) * player.direction;
				drawInfo.DrawDataCache.Add(GetDrawHeld(drawData with { rotation = rotation, sourceRect = frontSource }, drawInfo.drawPlayer, origin, out float armRotation));

				player.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, -(MathHelper.PiOver2 + 0.5f) * player.direction);
				player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, armRotation);
			}
		}

		return false;

		static DrawData GetItemData(DrawData input, Player player, float progress)
		{
			progress = Math.Clamp(progress, 0, 1);

			float rotation = input.rotation - Math.Min(EaseFunction.EaseOutBack().Ease(progress), 0.5f) * player.direction;
			Vector2 frontOffset = new(8 * player.direction, 100 * -EaseFunction.EaseSine.Ease(progress));
			Vector2 origin = input.sourceRect?.Size() / 2 ?? input.texture.Size() / 2;

			DrawData outputData = GetDrawHeld(input with { rotation = rotation }, player, origin, out _, offset: frontOffset);
			outputData.rotation = progress * 9 * -player.direction; //Do flips

			return outputData;
		}
	}

	public override void AddRecipes() => CreateRecipe()
		.AddIngredient(ItemID.Musket)
		.AddIngredient(ItemID.IllegalGunParts)
		.AddIngredient(ItemID.TissueSample, 14)
		.AddTile(TileID.Anvils)
		.Register();
}