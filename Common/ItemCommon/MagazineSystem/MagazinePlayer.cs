using SpiritReforged.Common.Easing;
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

	public override void PostUpdateEquips() => UpdateUI();
	void UpdateUI()
	{
		if (empoweredShellFlashTimer > 0)
			empoweredShellFlashTimer--;

		if (shellMoveTime > 0)
			shellMoveTime--;

		List<UIShell> shellsToRemove = [];

		foreach (UIShell shell in _ejectedShells)
		{
			shell.Update();
			if (!shell.Active)
				shellsToRemove.Add(shell);
		}

		foreach (UIShell shell in shellsToRemove)
			_ejectedShells.Remove(shell);

		if (UIActive && TryGetMagazineWeapon(Player, out var magazineWeapon))
		{
			int magazineSize = GetMagazineSize(Player);
			var magazine = magazineWeapon.GetCurrentMagazine();

			if (magazineWeapon.Reloading)
			{
				float interpolant = 1 - (magazine.ReloadTimer - 1) / ((float)GetReloadTime(Player) - 1);

				_oldCount = _count;
				_count = (int)MathHelper.Lerp(0, magazineSize, interpolant);

				if (_count != _oldCount && _count > 0 && Main.myPlayer == Player.whoAmI)
				{
					if (_count == magazineSize)
					{
						SoundEngine.PlaySound(SoundID.MaxMana with { Volume = 2f });
						_oldCount = _count;
					}
					else
						SoundEngine.PlaySound(new SoundStyle("SpiritReforged/Assets/SFX/UI/Magazine/ShellLoad") with { Volume = 2f, Pitch = MathHelper.Lerp(-0.25f, 0.25f, interpolant) });
				}
			}
			else
				_count = magazineWeapon.AmmoRemaining(Player);
		}
	}

	#region UI
	public class UIShell
	{
		public UIShell(Vector2 position, Vector2 velocity, int timeLeft, bool empowered = false)
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

		Vector2 offset;
		Vector2 _velocity;

		bool _empowered;
		int _timeLeft;
		int _maxTimeLeft;
		float rotation;
		float _scale;

		public void Update()
		{
			_velocity.Y += 0.1f;
			_velocity *= 0.98f;

			offset += _velocity;

			rotation += _velocity.Length() * 0.05f;

			if (--_timeLeft <= 0)
				Active = false;
		}

		public void Draw(SpriteBatch sb)
		{
			Texture2D texture = ModContent.Request<Texture2D>("SpiritReforged/Common/ItemCommon/MagazineSystem/MagazineUIShell").Value;
			Texture2D outlineTexture = ModContent.Request<Texture2D>("SpiritReforged/Common/ItemCommon/MagazineSystem/MagazineUIShell_Outline").Value;

			if (Progress > 0.5f)
			{
				float lerp = (Progress - 0.5f) / 0.5f;
				sb.Draw(outlineTexture, offset - Main.screenPosition, null, Color.Lerp(_empowered ? Color.Yellow : Color.Orange, Main.MouseBorderColor, 1 - lerp) * lerp, rotation, outlineTexture.Size() / 2f, _scale, 0f, 0f);
			}

			sb.Draw(texture, offset - Main.screenPosition, null, Main.mouseColor * Progress, rotation, texture.Size() / 2f, _scale, 0f, 0f);
		}
	}

	public override void Load()
	{
		CustomCursor.DrawCustomCursor += DrawAmmo;
		On_Main.DrawItems += DrawEjectedShells;
	}

	private void DrawEjectedShells(On_Main.orig_DrawItems orig, Main self)
	{
		orig(self);

		int ejectedCount = _ejectedShells.Count;

		if (ejectedCount > 0)
		{
			for (int x = 0; x < ejectedCount; x++)
			{
				var ejected = _ejectedShells[x];

				ejected.Draw(Main.spriteBatch);
			}
		}
	}

	public static List<UIShell> _ejectedShells = [];

	protected static int shellMoveTime;
	protected static int maxMoveTime;

	static int _count;
	static int _oldCount;

	public static int empoweredShellCount;
	static int empoweredShellFlashTimer;
	const int maxEmpoweredShellFlashTimer = 30;

	static bool UIActive => !Main.gameMenu && !Main.LocalPlayer.mouseInterface;

	private static void DrawAmmo(bool thick)
	{
		if (UIActive && _count > 0)
		{
			SpriteBatch sb = Main.spriteBatch;

			if (Main.LocalPlayer.TryGetModPlayer(out MagazinePlayer modPlayer) && TryGetMagazineWeapon(Main.LocalPlayer, out var magazineWeapon))
			{
				Texture2D texture = ModContent.Request<Texture2D>("SpiritReforged/Common/ItemCommon/MagazineSystem/MagazineUIShell").Value;
				Texture2D outlineTexture = ModContent.Request<Texture2D>("SpiritReforged/Common/ItemCommon/MagazineSystem/MagazineUIShell_Outline").Value;
				Texture2D starTexture = AssetLoader.LoadedTextures["Star"].Value;
				Texture2D bloomTexture = AssetLoader.LoadedTextures["Bloom"].Value;

				int magazineSize = modPlayer.GetMagazineSize(Main.LocalPlayer);
				var magazine = magazineWeapon.GetCurrentMagazine();

				const int offsetSize = 12;
				
				for (int i = 0; i < _count; i++)
				{
					float fadeOut = (i + 1) / (float)_count;

					if (_count >= 5)
					{
						if (i < _count - 5) // anything more than five just gets turned invisible
							fadeOut = 0f;
						else
						{
							fadeOut = (i - (_count - 5)) / 5f;
						}
					}

					Vector2 position = Main.MouseScreen + new Vector2(32, magazineSize * offsetSize);

					float moveShellUp = MathHelper.Lerp((magazine.AmmoUsed - 1) * offsetSize, magazine.AmmoUsed * offsetSize, shellMoveTime > 0 ? EaseBuilder.EaseCircularOut.Ease(1 - shellMoveTime / (float)maxMoveTime) : 1f);

					if (magazineWeapon.Reloading)
						moveShellUp = (magazineWeapon.GetCurrentMagazine().AmmoUsed - _count) * offsetSize;

					Vector2 offset = new Vector2(0, -offsetSize * i - moveShellUp);

					if (i < empoweredShellCount)
					{
						float sine = (float)Math.Sin(Main.timeForVisualEffects / 20f);

						Vector2 shake = Vector2.Zero;
						float rotation = sine * 0.2f;
						float scale = 1f;
						if (sine > 0)
						{
							scale += sine * 0.1f;
							shake += Main.rand.NextVector2CircularEdge(sine, sine);
						}

						if (empoweredShellFlashTimer > 0)
						{
							float lerp = empoweredShellFlashTimer / (float)maxEmpoweredShellFlashTimer;

							Vector2 starScale = new Vector2(MathHelper.Lerp(0.1f, 0.3f, 1f - lerp), 0.1f);
							
							sb.Draw(starTexture, position + offset, null, Color.Yellow.Additive() * fadeOut * lerp, 0f, starTexture.Size() / 2f, starScale * 1.2f, 0f, 0f);
							sb.Draw(starTexture, position + offset, null, Color.LightYellow.Additive() * fadeOut * lerp, 0f, starTexture.Size() / 2f, starScale * 1f, 0f, 0f);
						}

						sb.Draw(outlineTexture, position + offset + shake, null, Color.Yellow * fadeOut, rotation, outlineTexture.Size() / 2f, scale, 0f, 0f);
						sb.Draw(texture, position + offset + shake, null, Main.mouseColor * fadeOut, rotation, texture.Size() / 2f, scale, 0f, 0f);

						sb.Draw(bloomTexture, position + offset, null, Color.Yellow.Additive() * fadeOut * 0.15f, 0f, bloomTexture.Size() / 2f, 0.15f, 0f, 0f);
						sb.Draw(bloomTexture, position + offset, null, Color.LightYellow.Additive() * fadeOut * 0.1f, 0f, bloomTexture.Size() / 2f, 0.1f, 0f, 0f);

						if (empoweredShellFlashTimer > 0)
						{
							float lerp = empoweredShellFlashTimer / (float)maxEmpoweredShellFlashTimer;

							sb.Draw(bloomTexture, position + offset, null, Color.Yellow.Additive() * fadeOut * lerp * 0.5f, 0f, bloomTexture.Size() / 2f, 0.15f, 0f, 0f);
							sb.Draw(bloomTexture, position + offset, null, Color.LightYellow.Additive() * fadeOut * lerp * 0.35f, 0f, bloomTexture.Size() / 2f, 0.1f, 0f, 0f);
						}

						continue;
					}

					sb.Draw(outlineTexture, position + offset, null, Main.MouseBorderColor * fadeOut, 0f, outlineTexture.Size() / 2f, 1f, 0f, 0f);
					sb.Draw(texture, position + offset, null, Main.mouseColor * fadeOut, 0f, texture.Size() / 2f, 1f, 0f, 0f);
				}
			}
		}
	}

	public static void EjectUIShell(Item item)
	{
		shellMoveTime = item.useTime;
		maxMoveTime = item.useTime;

		Vector2 position = Main.MouseWorld + new Vector2(32, 0) / Main.GameViewMatrix.Zoom;

		_ejectedShells.Add(new UIShell(position, -Vector2.UnitY.RotatedByRandom(0.5f) * Main.rand.NextFloat(3f, 6f), 45, empoweredShellCount > 0));

		if (empoweredShellCount > 0)
			empoweredShellCount--;
	}

	/// <summary>
	/// Visually empowers the next <paramref name="amount"/> shells in the ui. Does nothing mechanically.
	/// </summary>
	/// <param name="amount"></param>
	public static void EmpowerUIShell(int amount = 1)
	{
		empoweredShellCount += amount;
		empoweredShellFlashTimer = maxEmpoweredShellFlashTimer;
	}
	#endregion
}
