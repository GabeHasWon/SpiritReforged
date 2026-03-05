using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Multiplayer;
using SpiritReforged.Common.PlayerCommon;
using SpiritReforged.Common.SimpleEntity;
using System.IO;
using System.Linq;
using Terraria.Audio;

namespace SpiritReforged.Content.Savanna.NPCs;

/// <summary> Mimics an NPC. </summary>
public class DevourerOfSoil : SimpleEntity //Use SimpleEntity to avoid appearing on browsers
{
	private static readonly Point[] Dimensions = [new Point(30, 38), new Point(22, 18), new Point(14, 22)]; //Excludes 2px(y) padding

	private readonly Vector2[] positions = new Vector2[Length];
	private const int Length = 8;

	private bool _playingDeathAnimation;
	private bool _justDied = true;
	private bool _justSpawned = true;
	private float _rotation;
	private int _soundDelay;

	public override void Load()
	{
		Size = new Vector2(30);
		On_Player.ItemCheck_MeleeHitNPCs += CheckMeleeHit;
	}

	#region hit detection
	private static void CheckMeleeHit(On_Player.orig_ItemCheck_MeleeHitNPCs orig, Player self, Item sItem, Rectangle itemRectangle, int originalDamage, float knockBack)
	{
		orig(self, sItem, itemRectangle, originalDamage, knockBack);

		foreach (var entity in SimpleEntitySystem.Entities)
			if (MeleeCollide(entity, itemRectangle))
				return;
	}

	private static bool MeleeCollide(SimpleEntity entity, Rectangle meleeHitbox)
	{
		if (entity is not DevourerOfSoil dos || dos._playingDeathAnimation)
			return false;

		foreach (var position in dos.positions)
			if (meleeHitbox.Intersects(dos.GetHitbox(position)))
			{
				dos.OnHit();

				if (Main.netMode == NetmodeID.MultiplayerClient)
					new DoSHitData((short)dos.whoAmI).Send();

				return true;
			}

		return false;
	}

	private void CheckProjectileHit()
	{
		foreach (var projectile in Main.ActiveProjectiles)
			if (ProjectileCollide(projectile))
				return;
	}

	private bool ProjectileCollide(Projectile projectile)
	{
		if (!projectile.friendly)
			return false;

		foreach (var position in positions)
		{
			var hitbox = GetHitbox(position);

			if (Collision.CheckAABBvLineCollision(position - hitbox.Size() / 2, hitbox.Size(), projectile.Center - projectile.velocity, projectile.Center)
				|| projectile.ModProjectile is ModProjectile modProj && modProj.Colliding(projectile.getRect(), hitbox) is true)
			{
				OnHit();
				return true;
			}
		}

		return false;
	}
	#endregion

	public override void Update()
	{
		UpdatePositions();

		var target = Main.player.Where(x => x.whoAmI != Main.maxPlayers && x.active && !x.dead).OrderBy(x => x.Distance(Center)).FirstOrDefault();
		if (_justSpawned)
		{
			velocity = new Vector2(Math.Sign(target.Center.X - Center.X) * 2f, -4f); //Leap upwards on spawn
			_justSpawned = false;
		}

		if (_playingDeathAnimation)
			DoDeathAnimation();
		else
		{
			ChaseTarget(target.Center);
			CheckProjectileHit();
		}

		if (InsideTiles())
		{
			if (Main.rand.NextBool(2))
			{
				var tilePos = (Center / 16).ToPoint();
				var dust = Main.dust[WorldGen.KillTile_MakeTileDust(tilePos.X, tilePos.Y, Framing.GetTileSafely(tilePos))];
				dust.fadeIn = Main.rand.NextFloat(1f, 1.25f);
				dust.noGravity = true;
			} //Spawn travel dusts

			if (_soundDelay == 0)
			{
				int delay = (int)MathHelper.Clamp(Center.Distance(target.Center) / 16f, 10, 20);
				_soundDelay = delay;

				SoundEngine.PlaySound(SoundID.WormDig, Center);
			} //Play digging sounds based on distance
		}

		_rotation = velocity.ToRotation();
		position += velocity;
		_soundDelay = Math.Max(_soundDelay - 1, 0);

		if (Center.Distance(Main.LocalPlayer.Center) < 1500)
			ChooseMusic.SetMusic(MusicID.Boss1); //Play Boss 1
	}

	private void ChaseTarget(Vector2 targetPosition)
	{
		const float speed = 5.5f;

		if (InsideTiles())
			velocity = Vector2.Lerp(velocity, Center.DirectionTo(targetPosition) * speed, .015f);
		else
			velocity = new Vector2(velocity.X * .98f, velocity.Y + .1f);
	}

	public void OnHit()
	{
		if (_justSpawned)
			return; //Can't be damaged when just spawned

		velocity.Y -= 2f;
		SoundEngine.PlaySound(SoundID.NPCHit1, Center);
		_playingDeathAnimation = true; //Instantly die
	}

	private void UpdatePositions()
	{
		static Vector2 GetSegmentDims(int segment)
		{
			var dims = Dimensions[1];

			if (segment == 0)
				dims = Dimensions[0];
			else if (segment == Length - 1)
				dims = Dimensions[2];

			return dims.ToVector2();
		}

		if (_justSpawned)
		{
			for (int i = 0; i < positions.Length; i++)
				positions[i] = Center + Vector2.UnitY * i * GetSegmentDims(i).Y;

			return;
		} //Set comfortable segment spawn positions

		positions[0] = Center;
		for (int i = 1; i < positions.Length; i++)
		{
			var ahead = positions[i - 1];
			var angle = positions[i].DirectionTo(ahead);

			positions[i] = ahead - angle * GetSegmentDims(i).Y;
		}
	}

	private void DoDeathAnimation()
	{
		if (InsideTiles())
		{
			if (_justDied)
				velocity.Y = -8f; //Shoot out of the ground before dying
			else
			{
				DoDeathEffects();
				Kill();
			}
		}
		else
		{
			_justDied = false;
			velocity = new Vector2(velocity.X * .98f, velocity.Y + .2f);
		}

		void DoDeathEffects()
		{
			if (Main.dedServ)
				return;

			for (int i = 0; i < positions.Length; i++)
			{
				int id = i == positions.Length - 1 ? 3 : i == 0 ? 1 : 2;
				int goreType = SpiritReforgedMod.Instance.Find<ModGore>(nameof(DevourerOfSoil) + id).Type;

				Gore.NewGore(null, positions[i], Main.rand.NextVector2Unit() * Main.rand.NextFloat(2f), goreType);

				for (int d = 0; d < 10; d++)
					Dust.NewDustPerfect(positions[i] + Main.rand.NextVector2Unit() * Main.rand.NextFloat(10f),
						DustID.Blood, Main.rand.NextVector2Unit() * 2f, 0, default, Main.rand.NextFloat(1f, 1.5f));
			}

			SoundEngine.PlaySound(SoundID.NPCDeath1, Center);
			Main.LocalPlayer.SimpleShakeScreen(3f, 3f, 60, 16 * 30);
		}
	}

	public override void Draw(SpriteBatch spriteBatch)
	{
		if (_justSpawned)
			return; //Don't bother drawing the first frame alive because rotation and segment positions haven't been initialized

		var texture = Texture.Value;
		int length = positions.Length - 1;

		for (int i = length; i >= 0; i--)
		{
			int frameY = i == length ? 2 : i == 0 ? 0 : 1;
			var frame = texture.Frame(1, Dimensions.Length, 0, frameY) with { Width = Dimensions[frameY].X, Height = Dimensions[frameY].Y };

			var position = positions[i];
			float rot = i == 0 ? _rotation : positions[i].AngleTo(positions[i - 1]);

			var lightColor = Lighting.GetColor((int)(position.X / 16), (int)(position.Y / 16));

			spriteBatch.Draw(texture, position - Main.screenPosition, frame, lightColor, rot + 1.57f, new Vector2(frame.Width / 2, frame.Height), 1, SpriteEffects.None, 0);
		}
	}

	private Rectangle GetHitbox(Vector2 center) => new((int)center.X - width / 2, (int)center.Y - height / 2, width, height);

	private bool InsideTiles()
	{
		var tile = Framing.GetTileSafely(Center);
		return WorldGen.SolidOrSlopedTile(tile);
	}
}

/// <summary> Syncs Devourer of Soil melee strike damage. </summary>
internal class DoSHitData : PacketData
{
	private readonly short _entityIndex;

	public DoSHitData() { }
	public DoSHitData(short entityIndex) => _entityIndex = entityIndex;

	public override void OnReceive(BinaryReader reader, int whoAmI)
	{
		short entity = reader.ReadInt16();

		if (Main.netMode == NetmodeID.Server)
			new DoSHitData(entity).Send(ignoreClient: whoAmI);

		if (SimpleEntitySystem.Entities[entity] is DevourerOfSoil dos)
			dos.OnHit();
	}

	public override void OnSend(ModPacket modPacket) => modPacket.Write(_entityIndex);
}