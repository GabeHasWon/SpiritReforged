using System.IO;
using System.Linq;

namespace SpiritReforged.Common.ProjectileCommon.Abstract;

public abstract class BaseMinion(float TargettingRange, float DeaggroRange, Vector2 Size) : ModProjectile
{
	public Player Player => Main.player[Projectile.owner];
	internal int IndexOfType => Main.projectile.Where(x => x.active && x.owner == Projectile.owner && x.type == Projectile.type && x.whoAmI < Projectile.whoAmI).Count();
	public bool CanRetarget { get; set; }

	private bool HadTarget
	{
		get => _hadTarget;
		set
		{
			if (_hadTarget != value)
			{
				_hadTarget = value;
				Projectile.netUpdate = true;
			}
		}
	}

	protected NPC _targetNPC;

	private readonly float TargettingRange = TargettingRange;
	private readonly float DeaggroRange = DeaggroRange;
	private readonly Vector2 Size = Size;

	private bool _hadTarget = false;

	public override void SetStaticDefaults()
	{
		ProjectileID.Sets.MinionSacrificable[Type] = true;
		ProjectileID.Sets.MinionTargettingFeature[Type] = true;
		ProjectileID.Sets.CultistIsResistantTo[Type] = true;
		Main.projPet[Type] = true;

		AbstractSetStaticDefaults();
	}

	public virtual void AbstractSetStaticDefaults() { }

	public override void SetDefaults()
	{
		Projectile.netImportant = true;
		Projectile.minion = true;
		Projectile.minionSlots = 1;
		Projectile.Size = Size;
		Projectile.friendly = true;
		Projectile.penetrate = -1;
		Projectile.ignoreWater = true;
		Projectile.tileCollide = false;
		Projectile.usesLocalNPCImmunity = true;
		Projectile.localNPCHitCooldown = 10;
		Projectile.DamageType = DamageClass.Summon;

		AbstractSetDefaults();
	}

	public virtual void AbstractSetDefaults() { }

	public override void AI()
	{
		float maxdist = TargettingRange;
		var miniontarget = Projectile.OwnerMinionAttackTargetNPC;

		if (miniontarget != null && CanReachTarget(miniontarget, false))
			_targetNPC = miniontarget;
		else
		{
			var validtargets = Main.npc.Where(x => x != null && CanReachTarget(x, true));

			if (!validtargets.Contains(_targetNPC))
				_targetNPC = null;

			if (CanRetarget)
			{
				foreach (NPC npc in validtargets)
				{
					if (npc.Distance(Projectile.Center) <= maxdist)
					{
						maxdist = npc.Distance(Projectile.Center);
						_targetNPC = npc;
					}
				}
			}
		}

		CanRetarget = true;
		if (_targetNPC == null)
		{
			IdleMovement(Player);
			HadTarget = false;
		}
		else
		{
			TargettingBehavior(Player, _targetNPC);
			HadTarget = true;
		}

		int framespersecond = 1;
		int startframe = 0;
		int endframe = Main.projFrames[Projectile.type];

		if (DoAutoFrameUpdate(ref framespersecond, ref startframe, ref endframe))
			UpdateFrame(framespersecond, startframe, endframe);

		bool CanReachTarget(NPC npc, bool initialTargetCheck)
		{
			bool success = npc.CanBeChasedBy(this) && CanSelectTarget(npc) && npc.Distance(Player.Center) <= DeaggroRange;

			if (npc.Distance(Player.Center) > maxdist && npc.Distance(Projectile.Center) > maxdist && !HadTarget && initialTargetCheck) //Only check when it's looking for a new valid target
				return false;

			return success;
		}
	}

	/// <summary> Checks whether this minion is allowed to aggro on <paramref name="target"/>. Involves simple collision and target checks by default. </summary>
	public virtual bool CanSelectTarget(NPC target) => target == _targetNPC || Collision.CanHitLine(Projectile.Center, 0, 0, target.Center, 0, 0);

	public virtual void IdleMovement(Player player) { }

	public override bool? CanCutTiles() => false;

	public override void SendExtraAI(BinaryWriter writer)
	{
		writer.Write(HadTarget);
		writer.Write(_targetNPC is null ? -1 : _targetNPC.whoAmI);
		writer.Write(CanRetarget);
	}

	public override void ReceiveExtraAI(BinaryReader reader)
	{
		HadTarget = reader.ReadBoolean();
		int whoamI = reader.ReadInt32();
		_targetNPC = whoamI == -1 ? null : Main.npc[whoamI];
		CanRetarget = reader.ReadBoolean();
	}

	public override bool MinionContactDamage() => true;

	public virtual void TargettingBehavior(Player player, NPC target) { }

	public virtual bool DoAutoFrameUpdate(ref int framespersecond, ref int startframe, ref int endframe) => true;

	private void UpdateFrame(int framespersecond, int startframe, int endframe)
	{
		Projectile.frameCounter++;
		if (Projectile.frameCounter > 60 / framespersecond)
		{
			Projectile.frameCounter = 0;
			Projectile.frame++;

			if (Projectile.frame >= endframe)
				Projectile.frame = startframe;
		}
	}
}