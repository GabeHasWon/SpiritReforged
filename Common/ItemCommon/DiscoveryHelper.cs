using ILLogger;
using MonoMod.Cil;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Content.Particles;
using System.Linq;
using System.Reflection;
using Terraria.Audio;
using Terraria.ModLoader.IO;

namespace SpiritReforged.Common.ItemCommon;

internal class DiscoveryHelper : ModPlayer
{
	private const string nameKey = "discNames";

	private static int TrackedIndex = -1;

	/// <summary> Saved and loaded with the player. </summary>
	private static HashSet<string> CollectedNames = [];
	/// <summary> Populated with items in the mod. </summary>
	private static readonly Dictionary<int, SoundStyle> TypeToSound = [];

	/// <summary> Register this item type with a sound on first pickup. </summary>
	public static void RegisterPickup(int type, SoundStyle sound)
	{
		if (!Main.dedServ)
			TypeToSound.Add(type, sound);
	}

	public override void Load() => IL_Main.DrawItemTextPopups += OnDrawPopup;

	private static void OnDrawPopup(ILContext il)
	{
		ILCursor c = new(il);

		if (!c.TryGotoNext(x => x.MatchLdsfld<Main>("screenHeight")))
		{
			SpiritReforgedMod.Instance.LogIL("Draw Discovery Popup", "Member 'screenHeight' not found.");
			return;
		}

		if (!c.TryGotoPrev(MoveType.Before, x => x.MatchLdsfld<Main>("player")))
		{
			SpiritReforgedMod.Instance.LogIL("Draw Discovery Popup", "Member 'player' not found.");
			return;
		}

		c.EmitLdloc0();
		c.EmitDelegate(PreDrawPopup);
	}

	/// <summary> Before the <see cref="Main.popupText"/> of <paramref name="index"/> is drawn. </summary>
	private static void PreDrawPopup(int index) //Makes text fancy
	{
		if (index != TrackedIndex)
			return;

		var popup = Main.popupText[TrackedIndex];

		var pixelScale = FontAssets.MouseText.Value.MeasureString(popup.name);

		float progress = popup.lifeTime / (float)PopupText.activeTime;
		var scale = new Vector2(1f, pixelScale.X / ((progress - .5f) * 50f)) * popup.scale;
		var tex = TextureAssets.Projectile[644].Value;
		var origin = new Vector2(tex.Width / 2, tex.Height / 2);

		var position = popup.position + pixelScale / 2;
		var color = (popup.color * (progress - .5f) * .15f).Additive();

		Main.spriteBatch.Draw(tex, position - Main.screenPosition, null, color, popup.rotation + MathHelper.PiOver2, origin, scale, SpriteEffects.None, 0);
		Main.spriteBatch.Draw(tex, position - Main.screenPosition, null, color * 5f, popup.rotation + MathHelper.PiOver2, origin, scale * .5f, SpriteEffects.None, 0);

		if (progress < .9f && Main.rand.NextBool(15))
		{
			var vel = Vector2.UnitX * Main.rand.NextFloat(-1f, 1f) * 3;
			var center = position + Main.rand.NextVector2Unit() * Main.rand.NextFloat(pixelScale.Y / 3);
			float pScale = Main.rand.NextFloat(.25f, .75f) * progress;

			ParticleHandler.SpawnParticle(new GlowParticle(center, vel, popup.color, pScale, 50));
			ParticleHandler.SpawnParticle(new GlowParticle(center, vel, popup.color * 3, pScale * .5f, 50));
		}

		if (popup.lifeTime <= 1)
			TrackedIndex = -1; //Clear index
	}

	public override bool OnPickup(Item item)
	{
		if (!Main.dedServ && Player.whoAmI == Main.myPlayer && TypeToSound.TryGetValue(item.type, out var sound))
		{
			string name = item.ModItem?.Name;

			if (name != null && !CollectedNames.Contains(name))
			{
				SoundEngine.PlaySound(sound);

				object m = typeof(PopupText).InvokeMember("FindNextItemTextSlot", BindingFlags.Static | BindingFlags.InvokeMethod | BindingFlags.NonPublic, null, null, null);
				if (m is int nextPopup)
					TrackedIndex = nextPopup;

				CollectedNames.Add(name);
			}
		}

		return true;
	}

	public override void SaveData(TagCompound tag) => tag[nameKey] = CollectedNames.ToList();
	public override void LoadData(TagCompound tag) => CollectedNames = tag.GetList<string>(nameKey).ToHashSet();
}