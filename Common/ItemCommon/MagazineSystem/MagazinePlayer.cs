using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Subclasses.Wrenches;
using SpiritReforged.Common.Visuals;
using Terraria.ModLoader;

namespace SpiritReforged.Common.ItemCommon.MagazineSystem;

[Autoload(Side = ModSide.Client)]
public class MagazinePlayer : ModPlayer
{
	public class UIShell
	{
		public UIShell(Vector2 position, Vector2 velocity, int timeLeft)
		{
			offset = position;
			_velocity = velocity;
			_timeLeft = timeLeft;
			_maxTimeLeft = timeLeft;
			_scale = 1f / Main.GameViewMatrix.Zoom.X; // scale our ejected shells to the current zoom at time of spawn, cause they're spawning from a scaled UI
		}

		public bool Active = true;
		public float Progress => _timeLeft / (float)_maxTimeLeft;

		Vector2 offset;
		Vector2 _velocity;
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
				sb.Draw(outlineTexture, offset - Main.screenPosition, null, Color.Lerp(Color.Orange, Main.MouseBorderColor, 1 - lerp) * lerp, rotation, outlineTexture.Size() / 2f, _scale, 0f, 0f);
			}

			sb.Draw(texture, offset - Main.screenPosition, null, Main.mouseColor * Progress, rotation, texture.Size() / 2f, _scale, 0f, 0f);
		}
	}

	public static bool HoldingMagazineWeapon => Main.LocalPlayer.HeldItem.TryGetGlobalItem<MagazineGlobalItem>(out var globalItem) && globalItem.Active;

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

	public override void ResetEffects()
	{
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
	}

	private static void DrawAmmo(bool thick)
	{
		if (!Main.LocalPlayer.mouseInterface && HoldingMagazineWeapon && Main.LocalPlayer.HeldItem.TryGetGlobalItem<MagazineGlobalItem>(out var globalItem) && globalItem.AmmoRemaining > 0)
		{
			SpriteBatch sb = Main.spriteBatch;

			int count = globalItem.AmmoRemaining;

			if (Main.LocalPlayer.TryGetModPlayer(out MagazinePlayer modPlayer))
			{
				const int offsetSize = 12;

				for (int i = 0; i < count; i++)
				{
					Texture2D texture = ModContent.Request<Texture2D>("SpiritReforged/Common/ItemCommon/MagazineSystem/MagazineUIShell").Value;
					Texture2D outlineTexture = ModContent.Request<Texture2D>("SpiritReforged/Common/ItemCommon/MagazineSystem/MagazineUIShell_Outline").Value;

					Vector2 position = Main.MouseScreen + new Vector2(32, globalItem.GetMagazineData()._magazineSize * offsetSize);

					float moveShellUp = MathHelper.Lerp((globalItem.GetCurrentMagazine().AmmoUsed - 1) * offsetSize, globalItem.GetCurrentMagazine().AmmoUsed * offsetSize, EaseBuilder.EaseCircularIn.Ease(1 - shellMoveTime / (float)maxMoveTime));

					Vector2 offset = new Vector2(0, -offsetSize * i - moveShellUp);

					sb.Draw(outlineTexture, position + offset, null, Main.MouseBorderColor, 0f, outlineTexture.Size() / 2f, 1f, 0f, 0f);
					sb.Draw(texture, position + offset, null, Main.mouseColor, 0f, texture.Size() / 2f, 1f, 0f, 0f);
				}
			}
		}
	}

	public static void EjectUIShell(Item item, int useTime)
	{
		shellMoveTime = useTime;
		maxMoveTime = useTime;

		Vector2 position = Main.MouseWorld + new Vector2(32, 0) / Main.GameViewMatrix.Zoom;

		_ejectedShells.Add(new UIShell(position, -Vector2.UnitY.RotatedByRandom(0.5f) * Main.rand.NextFloat(3f, 6f), 45));
	}
}
