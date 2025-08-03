using ILLogger;
using MonoMod.Cil;

namespace SpiritReforged.Content.Desert.Silk;

internal sealed class ProjectileEdits : ILoadable
{
	public void Load(Mod mod) => IL_Projectile.AI_047_MagnetSphere += AllowAfterimage;

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

	public void Unload() { }
}