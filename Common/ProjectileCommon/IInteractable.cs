namespace SpiritReforged.Common.ProjectileCommon;

public interface IInteractable
{
	public sealed class IteractableHook : ILoadable
	{
		public void Load(Mod mod) => On_Projectile.IsInteractible += OverrideInteractable;

		private static bool OverrideInteractable(On_Projectile.orig_IsInteractible orig, Projectile self)
		{
			bool value = orig(self) || self.ModProjectile is IInteractable;
			return value;
		}

		public void Unload() { }
	}
}