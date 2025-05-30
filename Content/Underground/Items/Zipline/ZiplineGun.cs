using SpiritReforged.Common.Misc;
using SpiritReforged.Common.ProjectileCommon;
using SpiritReforged.Common.Visuals.Glowmasks;
using System.Linq;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.UI;
using System.Collections.ObjectModel;

namespace SpiritReforged.Content.Underground.Items.Zipline;

public class ZiplineGun : ModItem
{
	public const int ExceedDist = 150;
	public const int UseTime = 60;

	private static Asset<Texture2D> X_Texture;

	#region checks
	/// <summary> Gets the cursor position in tile coordinates optionally adjusted by <see cref="MagnetCursor"/>. </summary>
	/// <param name="magnet"> Whether the final position should adjust to nearby solid tiles. </param>
	/// <param name="failed"> Whether <see cref="MagnetCursor"/> found a nearby solid tile. </param>
	private static Point16 GetCursor(bool magnet, out bool failed)
	{
		if (magnet && Main.SmartCursorIsUsed)
			return MagnetCursor.Magnetize(3, out failed);
		else
		{
			var coords = Main.MouseWorld.ToTileCoordinates16();
			failed = !MagnetCursor.ScanSurrounding(coords);

			return coords;
		}
	}

	/// <summary> Checks whether <see cref="Main.MouseWorld"/> has valid tile surroundings for a zipline. </summary>
	/// <param name="wall"> Whether the check is a wall. </param>
	private static bool CheckTile(out bool wall)
	{
		var coords = Main.MouseWorld.ToTileCoordinates();
		wall = false;

		if (WorldGen.SolidTile(Framing.GetTileSafely(coords)))
			return false;

		if (Framing.GetTileSafely(coords).WallType != WallID.None)
		{
			wall = true;
			return true;
		}

		GetCursor(true, out bool failed);
		return !failed;
	}

	/// <summary> Checks whether <see cref="Main.MouseWorld"/> is hovering over a removeable zipline. </summary>
	private static bool CheckRemoveable()
	{
		foreach (var zipline in ZiplineHandler.Ziplines)
		{
			if (zipline.Owner == Main.LocalPlayer && zipline.Contains(Main.MouseWorld.ToPoint(), out _))
				return true;
		}

		return false;
	}

	/// <summary> Checks whether <see cref="Main.MouseWorld"/> in within distance of an existing zipline. </summary>
	/// <param name="exceedsRange"> Whether the cursor is excedingly far away from the last zipline. Returns true in this case. </param>
	private static bool CheckDistance(out bool exceedsRange)
	{
		const int minDistance = 70;

		exceedsRange = false;
		var myZipline = ZiplineHandler.Ziplines.Where(x => x.Owner == Main.LocalPlayer).FirstOrDefault();

		if (myZipline == default || myZipline.points.Count == 0)
			return true;

		var last = myZipline.points.Last();
		if ((last / 16).Distance(GetCursor(true, out _).ToVector2()) > ExceedDist + .5f)
		{
			exceedsRange = true;
			return true;
		}

		return (last / 16).Distance(GetCursor(true, out _).ToVector2()) <= minDistance + .5f;
	}

	/// <summary> Checks if the angle of the zipline is within <see cref="Zipline.MaxAngle"/>. </summary>
	private static bool CheckAngle()
	{
		var myZipline = ZiplineHandler.Ziplines.Where(x => x.Owner == Main.LocalPlayer).FirstOrDefault();

		if (myZipline == default || myZipline.points.Count == 0)
			return true;

		return Math.Abs(myZipline.Angle(GetCursor(true, out _).ToWorldCoordinates())) <= Zipline.MaxAngle;
	}
	#endregion

	public override void Load()
	{
		X_Texture = ModContent.Request<Texture2D>(Texture + "_Cancel");
		On_Main.DrawInterface_6_TileGridOption += DrawAssistant;
	}

	private static void DrawAssistant(On_Main.orig_DrawInterface_6_TileGridOption orig)
	{
		bool oldMouseShowGrid = Main.MouseShowBuildingGrid;

		if (!Main.LocalPlayer.mouseInterface && Main.LocalPlayer.HeldItem?.ModItem is ZiplineGun ziplineGun && Main.LocalPlayer.GetModPlayer<ZiplinePlayer>().assistant)
		{
			var grid = TextureAssets.CursorRadial.Value;

			var cursorPos = GetCursor(false, out _).ToWorldCoordinates() - Main.screenPosition; //The position of the cursor
			var finalPos = GetCursor(true, out _).ToWorldCoordinates() - Main.screenPosition; //The position of placement (cursor adjusted by Magnetize)

			float rotation = (float)Math.Sin(Main.timeForVisualEffects / 50f) * .1f;
			bool exceedsRange = false;

			if (Main.LocalPlayer.gravDir == -1f)
				cursorPos.Y = Main.screenHeight - cursorPos.Y;

			if (CheckRemoveable())
			{
				var outline = TextureAssets.Extra[2].Value;
				var source = new Rectangle(0, 0, 16, 16);

				Main.spriteBatch.Draw(grid, cursorPos - grid.Size() / 2, (Color.Green * .5f).Additive());
				Main.spriteBatch.Draw(outline, finalPos, source, Color.Green.Additive(), rotation, source.Size() / 2, 1 + rotation, default, 0);
			}
			else if (!CheckDistance(out exceedsRange) || !CheckTile(out _))
			{
				if (!exceedsRange)
					DrawDottedLine(Color.Red.Additive());

				var x = X_Texture.Value;

				Main.spriteBatch.Draw(grid, cursorPos - grid.Size() / 2, (Color.Red * .5f).Additive());
				Main.spriteBatch.Draw(x, finalPos, null, Color.Red.Additive(), rotation, x.Size() / 2, 1 + rotation, default, 0);
			}
			else
			{
				var color = CheckAngle() ? Color.Cyan : Color.Yellow;

				if (!exceedsRange)
					DrawDottedLine(color.Additive());

				var outline = TextureAssets.Extra[2].Value;
				var source = new Rectangle(0, 0, 16, 16);

				Main.spriteBatch.Draw(grid, cursorPos - grid.Size() / 2, (color * .5f).Additive());
				Main.spriteBatch.Draw(outline, finalPos, source, color.Additive(), rotation, source.Size() / 2, 1 + rotation, default, 0);
			}

			Main.MouseShowBuildingGrid = false; //Always temporarily disable the default building grid before it is drawn
		}

		orig();

		Main.MouseShowBuildingGrid = oldMouseShowGrid;

		static void DrawDottedLine(Color color)
		{
			const int chunkWidth = 16;

			var myZipline = ZiplineHandler.Ziplines.Where(x => x.Owner == Main.LocalPlayer).FirstOrDefault();
			if (myZipline == default || myZipline.points.Count == 0)
				return;

			var start = GetCursor(true, out _).ToWorldCoordinates();
			var end = myZipline.points.Last();
			float totalDistance = start.Distance(end);

			var dirUnit = start.DirectionTo(end);
			float motionUnit = (float)Main.timeForVisualEffects / 50 % 2;

			var texture = TextureAssets.MagicPixel.Value;
			var source = new Rectangle(0, 0, 1, 4);

			int chunks = (int)(totalDistance / chunkWidth);

			for (int i = 0; i < chunks; i++)
			{
				if (i % 2 == 0)
					continue;

				var position = Vector2.Lerp(end + dirUnit * chunkWidth, start, (i + motionUnit) / chunks);

				float scaleWidth = totalDistance / chunks;
				if (i >= chunks - 2)
					scaleWidth *= 1f - motionUnit;
				else if (i <= 1)
					scaleWidth *= motionUnit / 2;

				var scale = new Vector2(scaleWidth, 1);

				Main.spriteBatch.Draw(texture, position - Main.screenPosition, source, color, start.AngleTo(end), source.Size() / 2, scale, default, 0);
			}
		}
	}

	public override void SetDefaults()
	{
		Item.width = 44;
		Item.height = 48;
		Item.useTime = Item.useAnimation = UseTime;
		Item.useStyle = ItemUseStyleID.Shoot;
		Item.noMelee = true;
		Item.noUseGraphic = true;
		Item.value = Item.sellPrice(0, 1, 0, 0);
		Item.rare = ItemRarityID.Blue;
		Item.autoReuse = true;
		Item.shoot = ModContent.ProjectileType<ZiplineProj>();
		Item.shootSpeed = 8f;
	}

	public override bool CanUseItem(Player player) =>  player.altFunctionUse == 2 || CheckTile(out _) && CheckDistance(out _) || CheckRemoveable();

	public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
	{
		var targetPos = GetCursor(true, out _).ToWorldCoordinates();
		velocity = new Vector2(velocity.Length(), 0).RotatedBy(player.AngleTo(targetPos));

		Projectile.NewProjectile(source, position, Vector2.Normalize(velocity), ModContent.ProjectileType<ZiplineGunHeld>(), 0, 0, player.whoAmI);
		SoundEngine.PlaySound(SoundID.DD2_BallistaTowerShot with { Pitch = .5f }, position);

		var muzzle = position + Vector2.Normalize(velocity) * 30f;
		for (int i = 0; i < 5; i++)
		{
			float mag = Main.rand.NextFloat();
			Dust.NewDustPerfect(muzzle, DustID.AmberBolt, (velocity * mag).RotatedByRandom(.5f * (1f - mag)), Scale: Main.rand.NextFloat(.5f, 1f)).noGravity = true;
		}

		if (player.altFunctionUse != 2)
		{
			PreNewProjectile.New(source, position, velocity, type, preSpawnAction: delegate (Projectile p)
			{
				if (p.ModProjectile is ZiplineProj zipline)
					zipline.cursorPoint = targetPos;
			});
		}

		return false;
	}

	public override bool AltFunctionUse(Player player) => ZiplineHandler.Ziplines.Where(x => x.Owner == player).Any();
	public override bool? UseItem(Player player)
	{
		if (player.altFunctionUse == 2)
		{
			if (Main.netMode == NetmodeID.MultiplayerClient)
				new ZipRemovalData((short)player.whoAmI).Send();

			ZipRemovalData.RemoveZiplines((short)player.whoAmI);
		}

		return true;
	}

	public override bool CanRightClick() => true;
	public override bool ConsumeItem(Player player) => false;

	public override void RightClick(Player player)
	{
		var mPlayer = Main.LocalPlayer.GetModPlayer<ZiplinePlayer>();

		mPlayer.assistant = !mPlayer.assistant;
		SoundEngine.PlaySound(SoundID.Mech);
	}

	public override bool PreDrawTooltip(ReadOnlyCollection<TooltipLine> lines, ref int x, ref int y) //Assistant tooltip
	{
		const int padding = 17;

		if (Item.tooltipContext != ItemSlot.Context.InventoryItem)
			return true;

		var ruler = TextureAssets.BuilderAcc.Value;
		var source = new Rectangle(0, 14, 14, 14);
		var position = new Vector2(x - 14, y + 5);

		bool assistant = Main.LocalPlayer.GetModPlayer<ZiplinePlayer>().assistant;
		string text = Language.GetTextValue("Mods.SpiritReforged.Items.ZiplineGun.Assistant" + (assistant ? "Off" : "On"));
		int textLength = (int)FontAssets.MouseText.Value.MeasureString(text).X + padding;

		foreach (var line in lines)
			position.Y += FontAssets.MouseText.Value.MeasureString(line.Text).Y; //Position vertically

		if (Main.SettingsEnabled_OpaqueBoxBehindTooltips)
		{
			var bgSource = new Rectangle((int)position.X, (int)position.Y, textLength + padding, 34);
			Utils.DrawInvBG(Main.spriteBatch, bgSource, new Color(23, 25, 81, 255) * 0.925f);
		}

		Main.spriteBatch.Draw(ruler, position + new Vector2(4, 15), source, assistant ? Color.White : Color.Gray, 0, source.Size() / 2, 1, default, 0);
		Utils.DrawBorderString(Main.spriteBatch, text, position + new Vector2(padding, 6), Main.MouseTextColorReal);

		return true;
	}
}

[AutoloadGlowmask("255,255,255", false)]
internal class ZiplineGunHeld : ModProjectile
{
	public static readonly SoundStyle LMG = new("SpiritReforged/Assets/SFX/Item/LMG");

	private static int TimeLeftMax => ZiplineGun.UseTime;

	public override LocalizedText DisplayName => Language.GetText("Mods.SpiritReforged.Items.ZiplineGun.DisplayName");

	public override void SetStaticDefaults() => Main.projFrames[Type] = 11;
	public override void SetDefaults()
	{
		Projectile.timeLeft = TimeLeftMax;
		Projectile.ignoreWater = true;
		Projectile.tileCollide = false;
	}

	public override void AI()
	{
		const int fireTime = 10;
		const int idleTime = 20;

		int holdDistance = 20;
		float rotation = Projectile.velocity.ToRotation();

		var owner = Main.player[Projectile.owner];
		Projectile.direction = Projectile.spriteDirection = owner.direction;

		if (Projectile.timeLeft < idleTime)
		{
			Projectile.UpdateFrame(30, Main.projFrames[Type] - 1);
			rotation += .3f * Projectile.direction * Math.Clamp((Projectile.timeLeft - 16f) / 4, 0, 1);
		}
		else if (Projectile.timeLeft < TimeLeftMax - fireTime)
		{
			Projectile.UpdateFrame(30, 5, 8);

			var dustPos = Projectile.Center - new Vector2(6, 4 * Projectile.direction).RotatedBy(rotation);
			Dust.NewDustPerfect(dustPos, DustID.Torch, -(Projectile.velocity * Main.rand.NextFloat()).RotatedByRandom(1)).noGravity = !Main.rand.NextBool(4);

			rotation -= (Projectile.timeLeft < TimeLeftMax - fireTime - 3 ? .5f : .25f) * Projectile.direction;

			if (Projectile.timeLeft == idleTime)
				SoundEngine.PlaySound(LMG with { Volume = .4f, Pitch = .8f }, Projectile.Center);
			else if (Projectile.timeLeft % 8 == 0)
				SoundEngine.PlaySound(LMG with { Volume = .1f, Pitch = MathHelper.Lerp(1f, -.5f, ((float)Projectile.timeLeft - fireTime) / ((float)TimeLeftMax - idleTime)) }, Projectile.Center);
		}
		else
		{
			const int recoilDuration = 2;

			float recoil = Math.Clamp(1f - (Projectile.timeLeft - (TimeLeftMax - recoilDuration)) / (float)recoilDuration, 0, 1) * 8f;
			holdDistance -= (int)recoil;
		}

		var position = owner.MountedCenter + new Vector2(holdDistance, 5 * -Projectile.direction).RotatedBy(rotation);

		Projectile.Center = owner.RotatedRelativePoint(position);
		Projectile.rotation = rotation;

		owner.heldProj = Projectile.whoAmI;
		owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - 1.57f + .4f * owner.direction);
		owner.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - 1.57f + .4f * owner.direction);
	}

	public override bool ShouldUpdatePosition() => false;
	public override bool? CanCutTiles() => false;
	public override bool? CanDamage() => false;

	public override bool PreDraw(ref Color lightColor)
	{
		var texture = TextureAssets.Projectile[Type].Value;
		var glowmask = GlowmaskProjectile.ProjIdToGlowmask[Type].Glowmask.Value;

		var position = new Vector2((int)(Projectile.Center.X - Main.screenPosition.X), (int)(Projectile.Center.Y - Main.screenPosition.Y));
		var frame = texture.Frame(1, Main.projFrames[Type], 0, Math.Min(Projectile.frame, Main.projFrames[Type] - 1), sizeOffsetY: -2);
		var effects = (Projectile.spriteDirection == -1) ? SpriteEffects.FlipVertically : SpriteEffects.None;

		Main.EntitySpriteDraw(texture, position, frame, Projectile.GetAlpha(lightColor), Projectile.rotation, frame.Size() / 2, Projectile.scale, effects);
		Main.EntitySpriteDraw(glowmask, position, frame, Projectile.GetAlpha(Color.White), Projectile.rotation, frame.Size() / 2, Projectile.scale, effects);
		return false;
	}
}