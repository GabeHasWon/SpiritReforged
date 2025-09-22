using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.Multiplayer;
using SpiritReforged.Common.ProjectileCommon;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.TileCommon.PresetTiles;
using SpiritReforged.Common.VerletChains;
using System.IO;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Desert.Tiles.Chains;

public class ChainLoop : EntityTile<ChainEntity>, IAutoloadTileItem
{
	public class PhysicsChain : ModProjectile, IGrappleable
	{
		public const float Gravity = 0.3f;
		public const float GroundBounce = 0.5f;
		public const float Drag = 0.9f;

		public virtual int FinalLength => 16 * (GetSegmentCount() - 1);
		public override string Texture => "Terraria/Images/Chain40";

		public Point16 anchor;
		public Chain chain;

		private Vector2 _lastDelta;
		private int _segments = -1;

		public int GetSegmentCount()
		{
			if (_segments != -1)
				return _segments;

			int id = ModContent.GetInstance<ChainEntity>().Find(anchor.X, anchor.Y);
			return _segments = (id == -1) ? 6 : (TileEntity.ByID[id] as ChainEntity).segments;
		}

		public override void SetStaticDefaults()
		{
			ProjectileID.Sets.TrailingMode[Type] = 0;
			ProjectileID.Sets.TrailCacheLength[Type] = 2;
		}

		public override void SetDefaults()
		{
			Projectile.Size = new Vector2(16);
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
			Projectile.timeLeft = 2;
		}

		public override void AI()
		{
			if (!CheckActive())
			{
				Projectile.Kill();
				return;
			}

			Vector2 anchorWorldCoords = anchor.ToWorldCoordinates(8, 0);
			Vector2 oldPosition = Projectile.oldPos[1];
			Vector2 position = Projectile.position;

			position += GetTotalForce() * 0.05f;

			if (oldPosition != Vector2.Zero)
			{
				Vector2 delta = (position - oldPosition) * Drag;
				position += Vector2.Lerp(_lastDelta, delta * 1.5f, 0.1f);

				_lastDelta = delta;
			}

			position += new Vector2(0, Gravity);
			position -= Constraint(anchorWorldCoords, Projectile.Center);

			if (!position.HasNaNs())
				Projectile.position = position;

			if (!Main.dedServ)
			{
				int length = TextureAssets.Projectile[Type].Value.Height;

				chain ??= new Chain(length - 2, GetSegmentCount(), anchorWorldCoords, new ChainPhysics(Drag, GroundBounce, Gravity));
				chain.Update(anchorWorldCoords, Projectile.Center);
			}

			Projectile.timeLeft++;
		}

		private Vector2 Constraint(Vector2 start, Vector2 end)
		{
			Vector2 delta = start - end;
			float distance = delta.Length();
			float finalDistance = FinalLength - distance;

			if (finalDistance > 0) //Compact indefinitely
			{
				return Vector2.Zero;
			}

			float fraction = finalDistance / Math.Max(distance, 1) / 2;
			delta *= fraction;

			return delta;
		}

		private Vector2 GetTotalForce()
		{
			Vector2 result = Vector2.Zero;
			foreach (Player p in Main.ActivePlayers)
			{
				if (p.Hitbox.Intersects(Projectile.Hitbox))
					result += p.velocity;
			}

			foreach (NPC n in Main.ActiveNPCs)
			{
				if (n.Hitbox.Intersects(Projectile.Hitbox))
					result += n.velocity;
			}

			return result;
		}

		public bool CheckActive() => ChainEntity.IsValidForChain(anchor.X, anchor.Y);

		public override void OnKill(int timeLeft)
		{
			if (!Main.dedServ && chain != null)
			{
				foreach (var vertex in chain.Vertices)
					Gore.NewGoreDirect(Projectile.GetSource_Death(), vertex.Position, Vector2.Zero, Mod.Find<ModGore>("Chain" + Main.rand.Next(1, 4)).Type);
			}
		}

		public bool CanGrapple(Projectile hook)
		{
			if (hook.Hitbox.Intersects(Projectile.Hitbox))
			{
				hook.Center = Projectile.Center;
				GrappleHelper.Latch(hook);

				return true;
			}

			return false;
		}

		public override bool PreDraw(ref Color lightColor)
		{
			chain?.Draw(Main.spriteBatch, TextureAssets.Projectile[Type].Value);
			return false;
		}

		public override void SendExtraAI(BinaryWriter writer) => writer.WritePoint16(anchor);
		public override void ReceiveExtraAI(BinaryReader reader) => anchor = reader.ReadPoint16();

		public override bool ShouldUpdatePosition() => false;
	}

	public int ChainType { get; protected set; }

	public override void SetStaticDefaults()
	{
		base.SetStaticDefaults();

		Main.tileNoFail[Type] = true;
		Main.tileSolid[Type] = false;
		Main.tileFrameImportant[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style1x2Top);
		TileObjectData.newTile.Height = 1;
		TileObjectData.newTile.CoordinateHeights = [16];
		TileObjectData.newTile.HookPostPlaceMyPlayer = Hook;
		TileObjectData.newTile.DrawYOffset = -8;
		TileObjectData.addTile(Type);

		AddMapEntry(new Color(150, 150, 150));
		DustType = -1;
		ChainType = ModContent.ProjectileType<PhysicsChain>();

		this.AutoItem().ResearchUnlockCount = 100;
	}

	public override void PostDrawPlacementPreview(int i, int j, SpriteBatch spriteBatch, Rectangle frame, Vector2 position, Color color, bool validPlacement, SpriteEffects spriteEffects)
	{
		Texture2D chainTexture = TextureAssets.Projectile[ChainType].Value;
		position.X += 8;
		position.Y += 8;

		for (int y = 0; y < GetSegmentCount(); y++)
		{
			position.Y += chainTexture.Height - 2;
			spriteBatch.Draw(chainTexture, position, null, color, 0, chainTexture.Size() / 2, 1, spriteEffects, 0);
		}
	}

	public static int GetSegmentCount() => 1 + Player.FlexibleWandCycleOffset % 6;
}