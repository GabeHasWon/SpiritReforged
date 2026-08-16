using ILLogger;
using MonoMod.Cil;
using SpiritReforged.Common.MathHelpers;
using SpiritReforged.Content.Ocean.Items.Reefhunter;
using SpiritReforged.Content.Ocean.Items.Reefhunter.Projectiles;
using System.Runtime.CompilerServices;

namespace SpiritReforged.Content.Desert.Silk;

internal sealed class ProjectileDuplicator : ModPlayer
{
	public delegate void ModifyShootStatsDelegate(Item item, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback);
	public delegate void ModifyOnSpawnDelegate(Projectile projectile);

	private static Dictionary<int, ModifyShootStatsDelegate> DelegateByItem = null;

	/// <summary> Whether a fired projectile is considered a duplicate. </summary>
	public static bool Duplicate { get; private set; }

	#region detours
	public void Load(Mod mod) => IL_Projectile.AI_047_MagnetSphere += AllowAfterimage;
	public void Unload() { }

	/// <summary> Allows two Magnet Sphere projectiles to exist simultaneously. </summary>
	private static void AllowAfterimage(ILContext il)
	{
		ILCursor c = new(il);
		if (!c.TryGotoNext(x => x.MatchCall<Projectile>("AI_047_MagnetSphere_TryAttacking")))
		{
			SpiritReforgedMod.Instance.LogIL("Magnet Sphere Afterimage", "Method 'AI_047_MagnetSphere_TryAttacking' not found.");
			return;
		}

		for (int i = 0; i < 2; i++)
		{
			if (!c.TryGotoPrev(x => x.MatchLdloc0()))
			{
				SpiritReforgedMod.Instance.LogIL("Magnet Sphere Afterimage", $"Instruction 'Ldloc 0' index {i} not found.");
				return;
			}
		}

		ILLabel label = c.MarkLabel();
		if (!c.TryGotoPrev(x => x.MatchLdfld<Entity>("whoAmI")))
		{
			SpiritReforgedMod.Instance.LogIL("Magnet Sphere Afterimage", "Member 'Entity.whoAmI' not found.");
			return;
		}

		c.Index += 2;

		c.EmitLdarg0();
		c.EmitDelegate(IsAfterimage);
		c.EmitBrtrue(label);

		static bool IsAfterimage(Projectile p) => p.TryGetGlobalProjectile(out AfterimageProjectile ap) && ap.Afterimage;
	}
	#endregion

	[UnsafeAccessor(UnsafeAccessorKind.Method, Name = "ItemCheck_Shoot")]
	private static extern void ItemCheck_Shoot(Player player, int i, Item sItem, int weaponDamage);

	public static void ShootFrom(Vector2 position, Player player, Item item = null, bool preserveDirection = false)
	{
		Duplicate = true;
		Vector2 oldPosition = player.position;
		int oldDirection = player.direction;

		player.position = position; //Briefly adjust the player position so that projectiles appear at the afterimage instead
		item ??= player.HeldItem;

		ItemCheck_Shoot(player, player.whoAmI, item, item.damage);

		if (preserveDirection)
			player.direction = oldDirection;

		player.position = oldPosition;
		Duplicate = false;
	}

	public override void ModifyShootStats(Item item, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
	{
		if (Duplicate)
			ChangeStats(item, ref position, ref velocity, ref type, ref damage, ref knockback);
	}

	/// <summary> Compensates for logic failures when firing specific duplicated projectiles. </summary>
	public static void ChangeStats(Item item, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
	{
		if (DelegateByItem == null)
			PopulateDelegates_Deferred();

		if (DelegateByItem.TryGetValue(item.type, out var action))
			action.Invoke(item, ref position, ref velocity, ref type, ref damage, ref knockback);
	}

	private static void PopulateDelegates_Deferred()
	{
		DelegateByItem = [];

		DelegateByItem.Add(ModContent.ItemType<UrchinStaff>(), static (Item item, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback) =>
		{
			type = ModContent.ProjectileType<UrchinBall>();
			position.Y -= 32;

			Vector2 shotTrajectory = Main.LocalPlayer.GetArcVel(Main.MouseWorld, 0.25f, 10); //LocalPlayer can be used as this is a locally invoked method only
			velocity = ArcVelocityHelper.GetArcVel(Main.LocalPlayer.MountedCenter, Main.MouseWorld, 0.25f, shotTrajectory.Length());
		});
	}
}