using System.Diagnostics.Eventing.Reader;
using Terraria.DataStructures;

namespace SpiritReforged.Common.BuffCommon;

internal class AutoMinionPlayer : ModPlayer
{
	public override bool Shoot(Item item, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
	{
		if (AutoloadedMinionBuff.Registered.TryGetValue(type, out int buffType))
			Player.AddBuff(buffType, 180, false);

		return true;
	}
}

internal class AutoMinionProjectile : GlobalProjectile
{
	public override bool PreAI(Projectile projectile)
	{
		if (AutoloadedMinionBuff.Registered.TryGetValue(projectile.type, out int buffType)) //Is an autoloaded minion
		{
			var player = Main.player[projectile.owner];

			if (player.HasBuff(buffType))
				projectile.timeLeft = 2;
		}

		return true;
	}
}

internal sealed class AutoloadedMinionBuff(string fullName) : AutoloadedBuff(fullName)
{
	/// <summary> Projectile type to buff type. Populated after <see cref="ModBuff.SetStaticDefaults"/>. </summary>
	public static readonly Dictionary<int, int> Registered = [];

	public int MinionType { get; private set; }

	public override void SetStaticDefaults()
	{
		Main.buffNoSave[Type] = true;
		Main.buffNoTimeDisplay[Type] = true;

		if (Mod.TryFind(SourceName, out ModProjectile p))
			Registered.Add(MinionType = p.Type, Type);
	}

	public override void Update(Player player, ref int buffIndex)
	{
		player.buffTime[buffIndex] = 180;

		if (player.whoAmI == Main.myPlayer && player.ownedProjectileCounts[MinionType] == 0)
		{
			player.DelBuff(buffIndex);
			buffIndex--;
		}
	}
}

[AttributeUsage(AttributeTargets.Class)]
internal class AutoloadMinionBuff : AutoloadBuffAttribute
{
	public override void AddContent(Type type, Mod mod)
	{
		var buff = new AutoloadedMinionBuff(type.FullName);
		mod.AddContent(buff);

		BuffAutoloader.SourceToType.Add(type, buff.Type);
	}
}