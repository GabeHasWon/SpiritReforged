using SpiritReforged.Common.Easing;
using SpiritReforged.Common.ItemCommon.MagazineSystem.UI;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Subclasses.Wrenches;
using SpiritReforged.Common.Visuals;
using Terraria.Audio;
using Terraria.ModLoader;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace SpiritReforged.Common.ItemCommon.MagazineSystem;

[Autoload(Side = ModSide.Client)]
public class MagazinePlayer : ModPlayer
{
	/// <summary>
	/// Defaults to 0
	/// </summary>
	public int additionalMagazineSize;

	/// <summary>
	/// Defaults to 1f.
	/// </summary>
	public float magazineSizeMultiplier;

	/// <summary>
	/// Additional reload time in ticks. Higher values increase reload time.
	/// </summary>
	public int additionalReloadTime;

	/// <summary>
	/// Defaults to 1f. Higher values increase reload time.
	/// </summary>
	public float reloadTimeMultiplier;

	/// <summary>
	/// Calculates the players true magazine size based on bonuses. Cannot be lower than 1
	/// </summary>
	/// <param name="baseMagazineSize">The base magazine size of the weapon</param>
	/// <returns></returns>
	public int GetMagazineSize(int baseMagazineSize) => Math.Max(1, (int)((baseMagazineSize + additionalMagazineSize) * magazineSizeMultiplier));

	/// <summary>
	/// Calculates the players true magazine size of their held weapon based on bonuses. Cannot be lower than one
	/// Will throw exceptions if the player is not holding a magazine weapon. Check with <see cref="GetMagazineWeapon(Player)"/> before calling.
	/// </summary>
	/// <param name="player">The player to check the held weapon of</param>
	/// <returns></returns>
	public int GetMagazineSize(Player player) => GetMagazineSize(GetMagazineWeapon(player).GetMagazineData()._magazineSize);

	/// <summary>
	/// Calculates the players true reload time based on bonuses. Cannot be lower than 30 (ticks, half a second)
	/// </summary>
	/// <param name="baseReloadTime">The base reload time of the weapon</param>
	/// <returns></returns>
	public int GetReloadTime(int baseReloadTime) => Math.Max(30, (int)((baseReloadTime + additionalReloadTime) * reloadTimeMultiplier));

	/// <summary>
	/// Calculates the players true reload time of their held weapon based on bonuses. Cannot be lower than 30 (ticks, half a second)
	/// Will throw exceptions if the player is not holding a magazine weapon. Check with <see cref="GetMagazineWeapon(Player)"/> before calling.
	/// </summary>
	/// <param name="player">The player to check the held weapon of</param>
	/// <returns></returns>
	public int GetReloadTime(Player player) => GetReloadTime(GetMagazineWeapon(player).GetMagazineData()._reloadTime);

	/// <summary>
	/// Returns the <see cref="MagazineGlobalItem"/> of the player's held item.
	/// </summary>
	/// <param name="player">The player to check the held weapon of</param>
	/// <returns>null if the player is not holding a magazine weapon.</returns>
	public static MagazineGlobalItem GetMagazineWeapon(Player player) => player.HeldItem.TryGetGlobalItem<MagazineGlobalItem>(out var globalItem) && globalItem.Active ? globalItem : null;

	/// <summary>
	/// Safely attempts to get the <see cref="MagazineGlobalItem"/> of the player's held item.
	/// </summary>
	/// <param name="player">The player to check the held weapon of</param>
	/// <param name="magazineWeapon">The <see cref="MagazineGlobalItem"/> of the player's held item, if successful</param>
	/// <returns></returns>
	public static bool TryGetMagazineWeapon(Player player, out MagazineGlobalItem magazineWeapon)
	{
		magazineWeapon = GetMagazineWeapon(player);

		return magazineWeapon is not null;
	}

	public override void ResetEffects()
	{
		additionalMagazineSize = 0;
		magazineSizeMultiplier = 1;
		additionalReloadTime = 0;
		reloadTimeMultiplier = 1;
	}

	#region UI
	public override void PostUpdateEquips() => UpdateUI();
	void UpdateUI()
	{
		if (empoweredFlashTimer > 0)
			empoweredFlashTimer--;

		if (uiSlotMoveTime > 0)
			uiSlotMoveTime--;

		List<MagazineUIAmmo> shellsToRemove = [];

		foreach (MagazineUIAmmo shell in _ejectedAmmos)
		{
			shell.Update();
			if (!shell.Active)
				shellsToRemove.Add(shell);
		}

		foreach (MagazineUIAmmo shell in shellsToRemove)
			_ejectedAmmos.Remove(shell);

		if (UIActive && TryGetMagazineWeapon(Player, out var magazineWeapon))
			_count = magazineWeapon.AmmoRemaining(Player);
	}
	public override void Load()
	{
		CustomCursor.DrawCustomCursor += DrawAmmo;
		On_Main.DrawItems += DrawEjectedShells;
	}

	private void DrawEjectedShells(On_Main.orig_DrawItems orig, Main self)
	{
		orig(self);

		int ejectedCount = _ejectedAmmos.Count;

		if (ejectedCount > 0)
		{
			for (int x = 0; x < ejectedCount; x++)
			{
				var ejected = _ejectedAmmos[x];

				ejected.Draw(Main.spriteBatch);
			}
		}
	}

	public static List<MagazineUIAmmo> _ejectedAmmos = [];

	protected static int uiSlotMoveTime;
	protected static int maxMoveTime;

	static int _count;
	static int _oldCount;

	public static int empoweredCount;
	static int empoweredFlashTimer;
	const int maxEmpoweredFlashTimer = 30;

	static bool UIActive => !Main.gameMenu && !Main.LocalPlayer.mouseInterface;

	private static void DrawAmmo(bool thick)
	{
		if (UIActive && _count > 0 && TryGetMagazineWeapon(Main.LocalPlayer, out var magazineWeapon))
		{
			SpriteBatch sb = Main.spriteBatch;

			switch (magazineWeapon.UIType)
			{
				case MagazineUIType.Shell:
					ShellUI.DrawUI(sb, _count, uiSlotMoveTime, maxMoveTime, empoweredCount, empoweredFlashTimer, maxEmpoweredFlashTimer); break;
			}
		}
	}

	public static void Fire(Item item)
	{
		uiSlotMoveTime = item.useTime;
		maxMoveTime = item.useTime;

		if (TryGetMagazineWeapon(Main.LocalPlayer, out var magazineWeapon))
		{
			MagazineUIAmmo ammo = null;

			switch (magazineWeapon.UIType)
			{
				case MagazineUIType.Shell:
					ammo = ShellUI.OnEject(empoweredCount); break;
			}

			if (ammo is not null)
				_ejectedAmmos.Add(ammo);
		}

		if (empoweredCount > 0)
			empoweredCount--;
	}

	/// <summary>
	/// Visually empowers the next <paramref name="amount"/> shells in the ui. Does nothing mechanically.
	/// </summary>
	/// <param name="amount"></param>
	public static void EmpowerShot(int amount = 1)
	{
		empoweredCount += amount;
		empoweredFlashTimer = maxEmpoweredFlashTimer;
	}

	/// <summary>
	/// Reduces the amount of empowered shots by one. Used when reloading with a <see cref="MagazineReloadType.OneAtATime"/> weapon.
	/// </summary>
	/// <param name="amount"></param>
	public static void UnempowerShot(int amount = 1)
	{
		if (empoweredCount > 0)
			empoweredCount -= amount;
		if (empoweredCount < 0)
			empoweredCount = 0;
	}
	#endregion
}

public class MagazineUIAmmo
{
	public MagazineUIAmmo(Vector2 position, Vector2 velocity, int timeLeft, bool empowered = false)
	{
		offset = position;
		_velocity = velocity;
		_timeLeft = timeLeft;
		_maxTimeLeft = timeLeft;
		_empowered = empowered;

		_scale = 1f / Main.GameViewMatrix.Zoom.X; // scale our ejected shells to the current zoom at time of spawn, cause they're spawning from a scaled UI
	}

	public bool Active = true;
	public float Progress => _timeLeft / (float)_maxTimeLeft;

	protected Vector2 offset;
	protected Vector2 _velocity;

	protected bool _empowered;
	protected int _timeLeft;
	protected int _maxTimeLeft;
	protected float rotation;
	protected float _scale;

	public virtual void DoUpdate() { }
	public void Update()
	{
		DoUpdate();

		if (--_timeLeft <= 0)
			Active = false;
	}

	public virtual void Draw(SpriteBatch sb) { }
}

