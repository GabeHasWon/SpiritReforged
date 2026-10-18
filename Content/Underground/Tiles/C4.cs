using SpiritReforged.Common.Easing;
using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Particles;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.ObjectInteractions;
using TileHelper.Common;

namespace SpiritReforged.Content.Underground.Tiles;

public class C4 : ModTile, ILoadItem
{
	public class ArmedC4 : ModProjectile, IDrawPixelated
	{
		public const int DETONATION_TIME = 60;
		
		public override LocalizedText DisplayName => Language.GetText("Mods.SpiritReforged.Tiles.C4.MapEntry");

		public override string Texture => ModContent.GetInstance<C4>().Texture;

		public Rectangle Source => TextureAssets.Projectile[Type].Frame(3, 1, Frame, 0, -2, -2);

		public int Frame
		{
			get => (int)Projectile.ai[0];
			set => Projectile.ai[0] = value;
		}

		public override void SetDefaults()
		{
			Projectile.Size = new Vector2(16);
			Projectile.friendly = true;
			Projectile.hostile = true;
			Projectile.penetrate = -1;
			Projectile.ignoreWater = true;
			Projectile.tileCollide = false;
			Projectile.timeLeft = DETONATION_TIME;
		}

		public override void AI()
		{
			const int rotation_time = 10;
			if (Projectile.timeLeft >= DETONATION_TIME - rotation_time)
			{
				float progress = (Projectile.timeLeft - (DETONATION_TIME - rotation_time)) / (float)rotation_time;
				Projectile.rotation = EaseFunction.EaseSine.Ease(Projectile.timeLeft / 5f) * progress;
			}

			const int scale_time = 5;
			if (Projectile.timeLeft <= scale_time)
				Projectile.scale += 1f / scale_time;

			if (Projectile.timeLeft % 20 == 0)
				SoundEngine.PlaySound(SoundID.Mech with { Pitch = 0.2f }, Projectile.Center);

			if (Projectile.timeLeft <= 2)
				Projectile.PrepareBombToBlow();
		}

		public override void OnKill(int timeLeft)
		{
			Projectile.Resize(80, 80);
			Rectangle region = Projectile.Hitbox;

			if (Main.myPlayer == Projectile.owner)
			{
				(region.X, region.Y, region.Width, region.Height) = (region.X / 16, region.Y / 16, region.Width / 16, region.Height / 16); //Convert tile tile coordinates
				ExplodeWalls(region);
			}

			if (!Main.dedServ)
			{
				for (int i = 0; i < 5; i++)
				{
					FindMomentum(out Vector2 center, out Vector2 upwardVelocity);
					ParticleHandler.SpawnParticle(new CompositeSmoke(center, upwardVelocity, Color.LightGray, Main.rand.Next(50, 70), false, false));
				}

				for (int i = 0; i < 5; i++)
				{
					FindMomentum(out Vector2 center, out Vector2 upwardVelocity);
					ParticleHandler.SpawnParticle(new SmallCompositeSmoke(center, upwardVelocity, Color.DarkGray, 50, false, false));
				}

				for (int i = 0; i < 10; i++)
					ParticleHandler.SpawnParticle(new EmberParticle(Projectile.Center, Main.rand.NextVector2Circular(5, 5), Color.OrangeRed, Color.Red, 1, 10, 10));

				for (int i = 0; i < 10; i++)
					Dust.NewDustPerfect(Projectile.Center, DustID.Torch, Main.rand.NextVector2Circular(2, 2));

				SoundEngine.PlaySound(SoundID.DD2_ExplosiveTrapExplode, Projectile.Center);
				SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);
			}

			Projectile.Damage();

			void FindMomentum(out Vector2 position, out Vector2 velocity)
			{
				position = Projectile.Center + Main.rand.NextVector2Circular(region.Width / 2, region.Height / 2);
				velocity = Main.rand.NextVector2Circular(2, 2) - Vector2.UnitY * 2;
			}
		}

		public override bool? CanDamage() => (Projectile.timeLeft <= 2) ? null : false;

		public static void ExplodeWalls(Rectangle tileRegion)
		{
			Vector2 center = tileRegion.Center();
			float radius = tileRegion.Width / 2f;

			for (int x = tileRegion.Left; x < tileRegion.Right; x++)
			{
				for (int y = tileRegion.Top; y < tileRegion.Bottom; y++)
				{
					float num = Math.Abs(x - center.X);
					float num2 = Math.Abs(y - center.Y);

					if (!(Math.Sqrt(num * num + num2 * num2) < (double)radius))
						continue;

					WorldGen.KillWall(x, y);
					if (Main.netMode != NetmodeID.SinglePlayer)
						NetMessage.SendTileSquare(-1, x, y);
				}
			}
		}

		public override bool PreDraw(ref Color lightColor) => false;

		void IDrawPixelated.DrawPixelated(SpriteBatch spriteBatch)
		{
			Texture2D texture = TextureAssets.Projectile[Type].Value;
			Color color = Projectile.GetAlpha(Lighting.GetColor(Projectile.Center.ToTileCoordinates()));
			Vector2 position = Projectile.Center - Main.screenPosition;

			IDrawPixelated.PixelateDrawPosition(ref position);

			Main.EntitySpriteDraw(texture, position, Source, color, Projectile.rotation, Source.Size() / 2, Projectile.scale / 2, 0);
			Main.EntitySpriteDraw(TileHelperSets.TileGlowmask[ModContent.TileType<C4>()].Texture.Value, position, Source, Projectile.GetAlpha(Color.White), Projectile.rotation, Source.Size() / 2, Projectile.scale / 2, 0);
		}
	}

	public override void SetStaticDefaults()
	{
		Main.tileFrameImportant[Type] = true;
		Main.tileNoFail[Type] = true;
		Main.tileLighted[Type] = true;

		TileID.Sets.HasOutlines[Type] = true;
		TileID.Sets.DisableSmartCursor[Type] = true;
		TileID.Sets.FramesOnKillWall[Type] = true;
		TileHelperSets.TileGlowmask[Type] = Helpers.RequestGlowmask(this);

		TileObjectData.newTile.CopyFrom(TileObjectData.Style1x1);
		TileObjectData.newTile.AnchorBottom = AnchorData.Empty;
		TileObjectData.newTile.AnchorWall = true;
		TileObjectData.newTile.StyleHorizontal = true;

		TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile);
		TileObjectData.newAlternate.AnchorWall = false;
		TileObjectData.newAlternate.AnchorRight = new AnchorData(AnchorType.SolidTile, TileObjectData.newTile.Height, 0);
		TileObjectData.addAlternate(1);

		TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile);
		TileObjectData.newAlternate.AnchorWall = false;
		TileObjectData.newAlternate.AnchorLeft = new AnchorData(AnchorType.SolidTile, TileObjectData.newTile.Height, 0);
		TileObjectData.addAlternate(2);

		TileObjectData.addTile(Type);

		DustType = -1;
		AddMapEntry(new Color(180, 25, 0), CreateMapEntryName());
	}

	public override void MouseOver(int i, int j)
	{
		Player player = Main.LocalPlayer;

		player.noThrow = 2;
		player.cursorItemIconEnabled = true;
		player.cursorItemIconID = this.AutoItemType();
	}

	public override bool RightClick(int i, int j)
	{
		Vector2 center = new Vector2(i, j).ToWorldCoordinates();
		Projectile.NewProjectile(new EntitySource_TileBreak(i, j), center, Vector2.Zero, ModContent.ProjectileType<ArmedC4>(), 100, 10, -1, Main.tile[i, j].TileFrameX / 18);
		WorldGen.KillTile(i, j, noItem: true);

		SoundEngine.PlaySound(SoundID.Mech, center);
		return true;
	}

	public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b) => (r, g, b) = (0.2f, 0.2f, 0f);

	public override bool IsTileDangerous(int i, int j, Player player) => true;

	public override bool HasSmartInteract(int i, int j, SmartInteractScanSettings settings) => true;
}