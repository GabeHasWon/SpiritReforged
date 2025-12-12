using ILLogger;
using MonoMod.Cil;
using SpiritReforged.Content.Ocean.Items.Reefhunter;
using SpiritReforged.Content.Ocean.Items.Reefhunter.Projectiles;

namespace SpiritReforged.Content.Desert.Silk;

internal sealed class ProjectileEdits : ILoadable
{
	public delegate void ModifyShootStatsDelegate(Item item, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback);
	private static Dictionary<int, ModifyShootStatsDelegate> DelegateByItem = null;

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
		});
	}
}