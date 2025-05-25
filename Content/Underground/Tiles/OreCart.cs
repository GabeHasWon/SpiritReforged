using SpiritReforged.Common.Particle;
using SpiritReforged.Common.WorldGeneration;
using Terraria.Audio;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Underground.Tiles;

public class OreCarts : ModTile
{
	public const int FrameWidth = 54;

	public static readonly SoundStyle Break = new("SpiritReforged/Assets/SFX/Tile/StoneCrack1")
	{
		Volume = .25f,
		PitchVariance = .3f
	};

	public override void SetStaticDefaults()
	{
		Main.tileFrameImportant[Type] = true;
		Main.tileNoAttach[Type] = true;
		Main.tileLavaDeath[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style3x3);
		TileObjectData.newTile.Origin = new(1, 2);
		TileObjectData.newTile.CoordinateHeights = [16, 16, 16];
		TileObjectData.newTile.StyleHorizontal = true;
		TileObjectData.newTile.RandomStyleRange = 8;
		TileObjectData.newTile.DrawYOffset = 8;
		TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.AlternateTile, TileObjectData.newTile.Width, 0);
		TileObjectData.newTile.AnchorAlternateTiles = [TileID.MinecartTrack];
		TileObjectData.addTile(Type);

		DustType = DustID.WoodFurniture;

		AddMapEntry(new Color(152, 107, 73), CreateMapEntryName());
	}

	public override void NearbyEffects(int i, int j, bool closer)
	{
		if (!TileObjectData.IsTopLeft(i, j) || Main.dedServ)
			return;

		if (!closer)
		{
			var world = new Vector2(i, j).ToWorldCoordinates(-8, 0);
			var area = new Rectangle((int)world.X, (int)world.Y, 72, 48);
			var p = Main.LocalPlayer;

			if (p.getRect().Intersects(area))
			{
				int style = Main.tile[i, j].TileFrameX / FrameWidth;
				var velocity = new Vector2(p.velocity.X * 1.2f, 0);

				Deactivate(i, j);
				NetMessage.SendTileSquare(-1, i, j, 3, 2);

				Projectile.NewProjectile(new EntitySource_TileBreak(i, j), new Vector2(i, j).ToWorldCoordinates(32, 32),
					velocity, ModContent.ProjectileType<RollingCart>(), 0, 0, Main.myPlayer, style);
			}
		}

		static void Deactivate(int i, int j)
		{
			for (int x = i; x < i + 3; x++)
			{
				for (int y = j; y < j + 3; y++)
				{
					var t = Framing.GetTileSafely(x, y);

					if (t.TileType == ModContent.TileType<OreCarts>())
						t.HasTile = false;
				}
			}
		}
	}

	public override IEnumerable<Item> GetItemDrops(int i, int j)
	{
		int frame = Main.tile[i, j].TileFrameX / FrameWidth;
		return GetItemsByStyle(frame);
	}

	public override void KillMultiTile(int i, int j, int frameX, int frameY)
	{
		int style = frameX / FrameWidth;
		EffectsByStyle(new EntitySource_TileBreak(i, j), new Rectangle(i * 16, j * 16, 48, 32), Vector2.Zero, style);
	}

	public static IEnumerable<Item> GetItemsByStyle(int style)
	{
		int itemID = style switch
		{
			0 => ItemID.CopperOre,
			1 => ItemID.IronOre,
			2 => ItemID.SilverOre,
			3 => ItemID.GoldOre,
			4 => ItemID.TinOre,
			5 => ItemID.LeadOre,
			6 => ItemID.TungstenOre,
			7 => ItemID.PlatinumOre,
			_ => ItemID.Amber
		};

		yield return new Item(itemID) { stack = Main.rand.Next(22, 31) };
		yield return new Item(ItemID.Wood) { stack = Main.rand.Next(4, 12) };
	}

	public static void EffectsByStyle(IEntitySource source, Rectangle area, Vector2 velocity, int style)
	{
		SoundEngine.PlaySound(SoundID.Item127 with { PitchVariance = .4f }, area.Center());
		SoundEngine.PlaySound(SoundID.Item89 with { Pitch = .5f }, area.Center());
		SoundEngine.PlaySound(Break, area.Center());

		ParticleHandler.SpawnParticle(new Particles.SmokeCloud(area.Bottom(), Vector2.UnitY * -.5f, Color.SandyBrown * .5f, .15f, Common.Easing.EaseFunction.EaseQuarticInOut, 150));

		for (int i = 0; i < 20; i++)
		{
			var vel = new Vector2(0, -Main.rand.NextFloat(2f)).RotatedByRandom(.25f);
			Dust.NewDustDirect(area.BottomLeft(), area.Width, 4, DustID.WoodFurniture, vel.X, vel.Y, Scale: Main.rand.NextFloat() + .25f);
		}

		int dustType = style switch
		{
			0 => DustID.Copper,
			1 => DustID.Iron,
			2 => DustID.Silver,
			3 => DustID.Gold,
			4 => DustID.Tin,
			5 => DustID.Lead,
			6 => DustID.Tungsten,
			7 => DustID.Platinum,
			_ => DustID.GemAmber
		};

		for (int i = 0; i < 10; i++)
		{
			var vel = new Vector2(0, -Main.rand.NextFloat(2f)).RotatedByRandom(.25f);
			Dust.NewDustDirect(area.BottomLeft(), area.Width, 4, dustType, vel.X, vel.Y, Scale: Main.rand.NextFloat() + .5f);
		}

		var mod = SpiritReforgedMod.Instance;

		for (int i = 1; i < 6; i++)
			Gore.NewGore(source, Main.rand.NextVector2FromRectangle(area),
				velocity * Main.rand.NextFloat(), mod.Find<ModGore>("Cart" + i).Type, 1f);

		for (int a = 0; a < 2; a++)
		{
			for (int i = 0; i < 2; i++)
			{
				int type = i + 1 + style * 2;
				var vel = (velocity + new Vector2(0, -Main.rand.NextFloat(3f))) * Main.rand.NextFloat(.5f, 1f);

				Gore.NewGore(source, Main.rand.NextVector2FromRectangle(area), vel, mod.Find<ModGore>("Ore" + type).Type, 1f);
			}
		}
	}
}

internal class RollingCart : ModProjectile
{
	public const int timeLeftMax = 600;

	public ref float Style => ref Projectile.ai[0];
	public ref float Counter => ref Projectile.ai[1];

	/// <summary> Whether this cart is going to be killed next frame. </summary>
	private bool _hide = false;

	public override void SetStaticDefaults() => Main.projFrames[Type] = 9; //Frame 9 is not reserved for Style
	public override void SetDefaults()
	{
		Projectile.Size = new Vector2(48, 32);
		Projectile.penetrate = -1;
		Projectile.timeLeft = timeLeftMax;
	}

	public override void AI()
	{
		if (Projectile.timeLeft == timeLeftMax) //Play a sound on spawn
			SoundEngine.PlaySound(SoundID.Item53, Projectile.Center);

		if (Surface())
		{
			CheckPlayerCollision();
			TryPlace();

			Projectile.velocity.Y = 0;
			Projectile.velocity.X *= .985f;
			float speed = Math.Abs(Projectile.velocity.X);

			if (speed > 9f)
			{
				Projectile.Kill();
			}
			else if (speed > 3f)
			{
				for (int i = 0; i < 2; i++)
				{
					var vel = -Projectile.velocity.RotatedByRandom(1);

					var d = Dust.NewDustDirect(Projectile.BottomLeft - Vector2.UnitX * Projectile.velocity.X, Projectile.width, 0, DustID.MinecartSpark, vel.X, vel.Y);
					d.noGravity = true;
					d.fadeIn = 1.5f;
				}
			}
		}
		else
		{
			Projectile.velocity.Y += .5f;
		}

		Projectile.direction = Projectile.spriteDirection = (Projectile.velocity.X < 0) ? -1 : 1;

		float target = Projectile.velocity.ToRotation() - (Projectile.spriteDirection == -1 ? MathHelper.Pi : 0);
		Projectile.rotation = Utils.AngleLerp(Projectile.rotation, target, .1f);
	}

	private void CheckPlayerCollision()
	{
		foreach (var p in Main.ActivePlayers)
		{
			var hitbox = p.getRect();

			if (hitbox.Intersects(Projectile.getRect()))
			{
				int push = p.direction;
				Projectile.velocity.X = p.velocity.X * 1.1f;

				int x = (int)(p.Center.X + (Projectile.width + hitbox.Width) / 2 * push);
				Projectile.Center = new Vector2(x, Projectile.Center.Y);

				break;
			}
		}
	}

	private void TryPlace()
	{
		if (_hide && (Projectile.Opacity -= .2f) <= 0)
		{
			Projectile.Kill();
			return;
		}

		//Revert to a tile if velocity is low enough and squarely in the tile grid
		if (Projectile.velocity.LengthSquared() > .25f * .25f || (int)Projectile.Center.X % 16 != 8)
			return;

		var coords = Projectile.Center.ToTileCoordinates();

		if (WorldMethods.AreaClear(coords.X, coords.Y, 3, 2))
		{
			int type = ModContent.TileType<OreCarts>();
			WorldGen.PlaceTile(coords.X, coords.Y, type, true, true, style: (int)Style);

			if (Framing.GetTileSafely(coords).TileType == type)
				_hide = true;
		}
	}

	private bool Surface()
	{
		var a = GetNearest(Projectile.Bottom, out bool f1);
		Projectile.Bottom = a;

		return !f1;

		static Vector2 GetNearest(Vector2 origin, out bool failed) //Rounds the given origin to a rail, if any
		{
			const int type = TileID.MinecartTrack;
			failed = true;

			if (Framing.GetTileSafely(origin).TileType == type)
			{
				origin.Y = (int)(origin.Y / 16) * 16;
				failed = false;
			}

			return origin;
		}
	}

	public override bool OnTileCollide(Vector2 oldVelocity)
	{
		const float capacity = 2f;

		if (oldVelocity.LengthSquared() > capacity * capacity)
			return true;

		Projectile.velocity = -(oldVelocity * .5f);
		return false;
	}

	public override void OnKill(int timeLeft)
	{
		if (_hide)
			return;

		if (Main.netMode != NetmodeID.MultiplayerClient)
		{
			foreach (var item in OreCarts.GetItemsByStyle((int)Style))
				Item.NewItem(Entity.GetSource_Death(), Projectile.Center, item);
		} //Item drops on the server/singleplayer

		if (!Main.dedServ)
			OreCarts.EffectsByStyle(Projectile.GetSource_Death(), Projectile.getRect(), Projectile.velocity, (int)Style);
	}

	public override bool PreDraw(ref Color lightColor)
	{
		var texture = TextureAssets.Projectile[Type].Value;
		var source = GetSource((int)Style);
		var gfx = new Vector2(0, Projectile.gfxOffY);
		var effects = SpriteEffects.None;

		Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition + gfx, source, Projectile.GetAlpha(lightColor), Projectile.rotation, source.Size() / 2, Projectile.scale, effects);

		source = GetSource(8);
		float rotation = Projectile.velocity.X * -10;

		for (int i = 0; i < 2; i++) //Draw wheels
		{
			var position = Projectile.Center + new Vector2((i == 0) ? -10 : 10, 16).RotatedBy(Projectile.rotation);
			Main.EntitySpriteDraw(texture, position - Main.screenPosition + gfx, source, Projectile.GetAlpha(lightColor), rotation, source.Size() / 2, Projectile.scale, effects);
		}

		return false;

		Rectangle GetSource(int frame) => texture.Frame(Main.projFrames[Type], 1, frame, 0, -2, 0);
	}
}