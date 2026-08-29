using SpiritReforged.Common.Misc;
using System.Linq;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Glyphs.Sanguine;

public partial class SanguineGlyph
{
	/// <param name="decayTimer">How long the buff stack lasts, in ticks</param>
	/// <param name="damageBonus">How much bonus damage should be added, 0.05: 5% | Bonus is added to 1f</param>
	internal class SanguineStack(int decayTimer, float damageBonus)
	{
		public int timer = decayTimer;
		public float damageBonus = damageBonus;
	}

	public sealed class SanguineStackingBuff : ModBuff
	{
		public override void SetStaticDefaults() => Main.buffNoSave[Type] = true;

		public override void Update(Player player, ref int buffIndex)
		{
			if (player.GetModPlayer<SanguinePlayer>().storedHealth >= 1)
			{
				player.buffTime[buffIndex] = 2;
			}
			else
			{
				player.DelBuff(buffIndex);
				buffIndex--;
			}
		}

		public override void ModifyBuffText(ref string buffName, ref string tip, ref int rare)
		{
			float storedHP = Main.LocalPlayer.GetModPlayer<SanguinePlayer>().storedHealth;

			int count = (int)storedHP;

			buffName = Language.GetTextValue("Mods.SpiritReforged.Buffs.SanguineStackingBuff.DisplayName", count);

			float damage = storedHP * SanguinePlayer.HEALTH_DAMAGE_RATE;

			tip = Language.GetTextValue("Mods.SpiritReforged.Buffs.SanguineStackingBuff.Description", Math.Round(damage * 100, 2));

			rare = ItemRarityID.Red;
		}

		public override void PostDraw(SpriteBatch spriteBatch, int buffIndex, BuffDrawParams drawParams)
		{
			var mp = Main.LocalPlayer.GetModPlayer<SanguinePlayer>();

			int count = (int)mp.storedHealth;

			float lerp = mp.lifestealCooldown / 20f;

			var drawColor = Color.Lerp(Color.White, Color.Red.Additive(), lerp);

			float scale = MathHelper.Lerp(1f, 1.2f, lerp);

			string text = count.ToString();

			Utils.DrawBorderString(spriteBatch, text, drawParams.Position + new Vector2(25, 20), drawColor, scale);
		}
	}
}
