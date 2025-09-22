using SpiritReforged.Common.ProjectileCommon;
using SpiritReforged.Common.VerletChains;
using Terraria.Audio;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Desert.Tiles.Chains;

public class ChainObject : IGrappleable
{
	public const float Gravity = 0.3f;
	public const float GroundBounce = 0.5f;
	public const float Drag = 0.9f;

	public static readonly SoundStyle Rattle = new("SpiritReforged/Assets/SFX/Tile/ChainRattle");

	public virtual Texture2D Texture => TextureAssets.Chain40.Value;
	public Rectangle Hitbox => new((int)Position.X - 8, (int)Position.Y - 8, 16, 16);
	public virtual int FinalLength => 16 * segments;

	public Vector2 Position
	{
		get => _positions[0];
		set => _positions[0] = value;
	}

	private readonly Vector2[] _positions = new Vector2[3];

	private Vector2 _lastDelta;

	public readonly Chain chain;
	public readonly byte segments;
	public readonly Point16 anchor;

	public ChainObject(Point16 anchor, byte segments)
	{
		this.anchor = anchor;
		this.segments = segments;

		Position = this.anchor.ToWorldCoordinates() + new Vector2(2);
		
		if (!Main.dedServ)
		{
			chain = CreateChain();
		}
	}

	public virtual void Draw(SpriteBatch spriteBatch) => chain?.Draw(spriteBatch, Texture);
	public virtual Chain CreateChain()
	{
		int length = Texture.Height;
		return new Chain(length - 2, segments + 1, Position, new ChainPhysics(Drag, GroundBounce, Gravity));
	}

	public virtual void Update()
	{
		Vector2 anchorWorldCoords = anchor.ToWorldCoordinates(8, 0);
		Vector2 oldPosition = _positions[2];
		Vector2 position = Position;

		position += GetTotalForce() * 0.05f;

		if (oldPosition != Vector2.Zero)
		{
			Vector2 delta = (position - oldPosition) * Drag;
			position += Vector2.Lerp(_lastDelta, delta * 1.5f, 0.1f);

			_lastDelta = delta;
		}

		position += new Vector2(0, Gravity);
		position -= Constraint(anchorWorldCoords, Position);

		if (!position.HasNaNs())
			Position = position;

		chain?.Update(anchorWorldCoords, Position);

		for (int i = _positions.Length - 1; i > 0; i--)
			_positions[i] = _positions[i - 1];
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
			if (p.Hitbox.Intersects(Hitbox))
				result += p.velocity;
		}

		foreach (NPC n in Main.ActiveNPCs)
		{
			if (n.Hitbox.Intersects(Hitbox))
				result += n.velocity;
		}

		return result;
	}

	public bool CanGrapple(Projectile hook)
	{
		if (hook.Hitbox.Intersects(Hitbox))
		{
			hook.Center = Position;
			GrappleHelper.Latch(hook);

			return true;
		}

		return false;
	}

	public virtual void OnKill()
	{
		if (!Main.dedServ && chain != null)
		{
			Mod mod = SpiritReforgedMod.Instance;

			foreach (var vertex in chain.Vertices)
				Gore.NewGoreDirect(new EntitySource_Misc("Chain"), vertex.Position, Vector2.Zero, mod.Find<ModGore>("Chain" + Main.rand.Next(1, 4)).Type);
		}
	}
}