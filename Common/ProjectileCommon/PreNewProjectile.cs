using ILLogger;
using MonoMod.Cil;
using Terraria.DataStructures;

namespace SpiritReforged.Common.ProjectileCommon;

internal class PreNewProjectile : ILoadable
{
	public delegate void PreSpawnDelegate(Projectile projectile);
	private static PreSpawnDelegate PreSpawnAction;

	/// <summary> Includes a delegate to modify additional projectile data (like scale or GlobalProjectile fields) before the projectile is synced automatically. </summary>
	/// <returns> The newly created projectile instance. </returns>
	public static Projectile New(IEntitySource spawnSource, Vector2 position, Vector2 velocity, int type, int damage = 0, float knockback = 0, int owner = -1, float ai0 = 0, float ai1 = 0, float ai2 = 0, PreSpawnDelegate preSpawnAction = null)
	{
		PreSpawnAction = preSpawnAction;
		var projectile = Projectile.NewProjectileDirect(spawnSource, position, velocity, type, damage, knockback, owner, ai0, ai1, ai2); //PreSyncProjectile is called
		PreSpawnAction = null;

		return projectile;
	}

	public void Load(Mod mod) => IL_Projectile.NewProjectile_IEntitySource_float_float_float_float_int_int_float_int_float_float_float += ModifyNewProjectile;
	
	private void ModifyNewProjectile(ILContext il)
	{
		ILCursor c = new(il);

		if (!c.TryGotoNext(x => x.MatchCall("Terraria.ModLoader.ProjectileLoader", "OnSpawn")))
		{
			SpiritReforgedMod.Instance.LogIL("Modify New Projectile", "Member 'ProjectileLoader.OnSpawn' not found.");
			return;
		}

		c.Index -= 2;

		c.EmitLdloc1();
		c.EmitDelegate(PreSyncProjectile);
	}

	private void PreSyncProjectile(Projectile projectile)
	{
		if (projectile.owner == Main.myPlayer)
			PreSpawnAction?.Invoke(projectile);
	}

	public void Unload() { }
}
