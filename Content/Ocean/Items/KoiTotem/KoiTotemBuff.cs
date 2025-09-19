using MonoMod.Cil;
using SpiritReforged.Common.Visuals;

namespace SpiritReforged.Content.Ocean.Items.KoiTotem;

public class KoiTotemBuff : ModBuff
{
	public static float CursorOpacity { get; private set; }
	private static readonly Asset<Texture2D> CursorTexture = DrawHelpers.RequestLocal(typeof(KoiTotemBuff), "Cursor_Plus", false);

	#region detours
	public override void Load()
	{
		IL_Player.GetItem_FillIntoOccupiedSlot += RemovePickupSound;
		On_Player.ItemCheck_CheckFishingBobber_PickAndConsumeBait += CheckBait;
		On_Main.DrawInterface_40_InteractItemIcon += DrawPlusIcon;
	}

	private static void RemovePickupSound(ILContext il)
	{
		var c = new ILCursor(il);
		if (!c.TryGotoNext(MoveType.After, x => x.MatchLdcI4(7))) //Match the sound id
			return;

		//Remove the item pickup sound when replenishing bait to an existing stack
		c.EmitDelegate((int sound) => (CursorOpacity == 1) ? -1 : sound);
	}

	private static void DrawPlusIcon(On_Main.orig_DrawInterface_40_InteractItemIcon orig, Main self)
	{
		orig(self);

		if (CursorOpacity > 0 && Main.LocalPlayer.cursorItemIconID > ItemID.None)
		{
			var pos = Main.MouseScreen + new Vector2(6) + TextureAssets.Item[Main.LocalPlayer.cursorItemIconID].Size();
			Main.spriteBatch.Draw(CursorTexture.Value, pos, null, Color.White * CursorOpacity * 2, 0, Vector2.Zero, Main.cursorScale, SpriteEffects.None, 0f);
			CursorOpacity = MathHelper.Max(CursorOpacity - .025f, 0);
		}
	}

	private static void CheckBait(On_Player.orig_ItemCheck_CheckFishingBobber_PickAndConsumeBait orig, Player self, Projectile bobber, out bool pullTheBobber, out int baitTypeUsed)
	{
		orig(self, bobber, out pullTheBobber, out baitTypeUsed);

		if (pullTheBobber && self.HasBuff<KoiTotemBuff>() && Main.rand.NextBool(10))
		{
			CursorOpacity = 1;
			self.GetItem(self.whoAmI, new Item(baitTypeUsed), new GetItemSettings(false, true));
		}
	}
	#endregion

	public override void SetStaticDefaults()
	{
		Main.pvpBuff[Type] = true;
		Main.buffNoTimeDisplay[Type] = true;
	}

	public override void Update(Player player, ref int buffIndex) => player.fishingSkill += 5;
}