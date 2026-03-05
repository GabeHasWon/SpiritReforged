using ILLogger;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using SpiritReforged.Common.Misc;

namespace SpiritReforged.Content.Ocean.Items.Reefhunter.CascadeArmor;

internal class ResistanceTextHandler : ILoadable
{
	private struct ResistanceText(int combatTextIndex, float damage, float resistance)
	{
		public int cIndex = combatTextIndex;
		public float damage = damage;
		public float resistanceValue = resistance;

		public readonly void Draw()
		{
			const int timeMax = 10;

			float resistanceDamage = resistanceValue;
			if (resistanceDamage == 0)
				return;

			var shield = TextureAssets.Extra[58].Value;

			var cText = Main.combatText[cIndex];
			int time = Math.Clamp(cText.lifeTime - 35, 0, timeMax);
			float span = 1f - (float)time / timeMax;

			string text = resistanceDamage.ToString("0");
			var font = FontAssets.CombatText[0].Value;
			var origin = font.MeasureString(text) / 2;

			var center = cText.position - Main.screenPosition + origin;
			var leftPosition = center - new Vector2(span * 18);

			if (Main.LocalPlayer.gravDir == -1)
			{
				float posY = Main.screenHeight - (cText.position.Y - Main.screenPosition.Y);
				leftPosition = new Vector2(cText.position.X - Main.screenPosition.X, posY) + origin - new Vector2(span * 18);
			}

			if (damage == 0) //Special case where the player resists ALL damage
			{
				var ray = AssetLoader.LoadedTextures["GodrayCircle"].Value;

				Main.spriteBatch.Draw(ray, center, null, Color.CornflowerBlue.Additive(), (float)Main.timeForVisualEffects / 80f, ray.Size() / 2, cText.scale * .075f * span, default, 0);
				Main.spriteBatch.Draw(shield, center, null, Color.Cyan.Additive(), cText.rotation, shield.Size() / 2, cText.scale * .8f * span, default, 0);
				Main.spriteBatch.Draw(shield, center, null, Color.White, cText.rotation, shield.Size() / 2, cText.scale * .75f * span, default, 0);

				cText.alpha = 0; //Turn the default text invisible
			}
			else
			{
				if (time < timeMax)
				{
					Main.spriteBatch.Draw(shield, leftPosition, null, Color.Cyan.Additive() * cText.alpha, cText.rotation, shield.Size() / 2, cText.scale * .8f * span, default, 0);
					Main.spriteBatch.Draw(shield, leftPosition, null, Color.White * cText.alpha, cText.rotation, shield.Size() / 2, cText.scale * .75f * span, default, 0);

					Utils.DrawBorderStringFourWay(Main.spriteBatch, font, text, leftPosition.X, leftPosition.Y, GetColor(1f) * span * cText.alpha, GetColor(.25f) * span * cText.alpha, origin, cText.scale);
				}
				else //Draw a combined value
				{
					cText.alpha = 0; //Turn the default text invisible
					Utils.DrawBorderStringFourWay(Main.spriteBatch, font, (damage + resistanceDamage).ToString("0"), leftPosition.X, leftPosition.Y, GetColor(.85f), GetColor(.25f), origin, cText.scale);
				}
			}

			Color GetColor(float dark)
			{
				float num3 = cText.scale / CombatText.TargetScale;

				var baseColor = Color.Cyan;
				return new Color((int)(baseColor.R * dark), (int)(baseColor.G * dark), (int)(baseColor.B * dark), baseColor.A) * num3;
			}
		}

		public readonly bool Active() => cIndex >= 0 && cIndex < Main.maxCombatText && Main.combatText[cIndex].active;
	}

	/// <summary> The last index of combat text spawned from the local player taking damage. </summary>
	private static int LastOpenIndex;
	private static readonly HashSet<ResistanceText> Texts = [];

	/// <summary> Apply a flat damage resistance visual modification to combat text of <paramref name="index"/>. </summary>
	/// <param name="damage"> The unrounded damage value taken. </param>
	/// <param name="resistance"> The flat damage resisted. </param>
	/// <param name="index"> The index of combat text to modify. Leave as -1 to modify the last instance of the player taking damage. </param>
	public static void ApplyText(float damage, float resistance, int index = -1) => Texts.Add(new((index == -1) ? LastOpenIndex : index, damage, resistance));

	public void Load(Mod mod)
	{
		IL_Player.Hurt_HurtInfo_bool += TrackCombatText;
		IL_Main.DoDraw += PostDrawCombatText;
		On_CombatText.UpdateCombatText += CheckActive;
	}

	private static void TrackCombatText(ILContext il)
	{
		ILCursor c = new(il);

		if (!c.TryGotoNext(MoveType.After, x => x.MatchCall<CombatText>("NewText")))
		{
			SpiritReforgedMod.Instance.LogIL("Track Combat Text", "Method 'CombatText.NewText' not found.");
			return;
		}

		c.EmitDelegate(SetText); //Only track player damage associated text
	}

	private static int SetText(int index) => LastOpenIndex = index;

	private static void PostDrawCombatText(ILContext il)
	{
		ILCursor c = new(il);

		c.GotoNext(x => x.MatchLdsfld<Main>("hideUI"));

		c.GotoNext(MoveType.After, x => x.MatchCall("ReLogic.Graphics.DynamicSpriteFontExtensionMethods", "DrawString")); //Reversed gravity
		c.Emit(OpCodes.Ldloc_S, (byte)35);
		c.EmitDelegate(DrawResistance);

		c.GotoNext(MoveType.After, x => x.MatchCall("ReLogic.Graphics.DynamicSpriteFontExtensionMethods", "DrawString")); //Normal gravity
		c.Emit(OpCodes.Ldloc_S, (byte)35);
		c.EmitDelegate(DrawResistance);
	}

	private static void DrawResistance(int index)
	{
		foreach (var text in Texts)
			if (text.cIndex == index)
				text.Draw();
	}

	private static void CheckActive(On_CombatText.orig_UpdateCombatText orig)
	{
		orig();

		HashSet<ResistanceText> removals = [];

		foreach (var text in Texts)
			if (!text.Active())
				removals.Add(text);

		foreach (var removal in removals)
			Texts.Remove(removal);
	}

	public void Unload() { }
}
