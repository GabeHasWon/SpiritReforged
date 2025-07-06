using SpiritReforged.Common.Misc;
using SpiritReforged.Common.NPCCommon;
using SpiritReforged.Common.Visuals;
using Terraria.GameContent.ItemDropRules;

namespace SpiritReforged.Content.Aether.Items;

internal class ScryingLens : ModItem
{
	private static readonly Asset<Texture2D> Glow = ModContent.Request<Texture2D>(DrawHelpers.RequestLocal(typeof(ScryingLens), "ScryingLens_Glow"));

	public override void Load() => On_Main.DrawNPC_SlimeItem += ModifyScryingLensSlimeItem;

	private static void ModifyScryingLensSlimeItem(On_Main.orig_DrawNPC_SlimeItem orig, NPC npc, int typeCache, Color npcColor, float addedRotation)
	{
		int itemType = (int)npc.ai[1];

		if (itemType == ModContent.ItemType<ScryingLens>())
		{
			Lighting.AddLight(npc.Center, Color.White.ToVector3() * 0.2f);

			float scale = 1f;
			Main.GetItemDrawFrame(itemType, out var itemTexture, out var src);
			float frameWidth = src.Width;
			float frameHeight = src.Height;
			bool isBallooned = (int)npc.ai[0] == -999;
			float scaleX = 22f * npc.scale;
			float scaleY = 18f * npc.scale;

			if (isBallooned)
			{
				scaleX = 14f * npc.scale;
				scaleY = 14f * npc.scale;
			}

			if (frameWidth > scaleX)
			{
				scale *= scaleX / frameWidth;
				frameWidth *= scale;
				frameHeight *= scale;
			}

			if (frameHeight > scaleY)
			{
				scale *= scaleY / frameHeight;
				frameWidth *= scale;
			}

			int npcFrameY = npc.frame.Y / (TextureAssets.Npc[typeCache].Height() / Main.npcFrameCount[typeCache]);
			float yOffset = 1 - npcFrameY;
			float xOffset = npcFrameY * 2 - 1;
			float rotation = 0.2f - 0.3f * npcFrameY;

			if (isBallooned)
			{
				rotation = 0f;
				yOffset -= 6f;
				xOffset -= frameWidth * addedRotation;
			}

			npcColor = npc.GetShimmerColor(npcColor);
			var position = new Vector2(npc.Center.X + xOffset, npc.Center.Y + npc.gfxOffY + yOffset) - Main.screenPosition;
			Main.spriteBatch.Draw(itemTexture, position, src, npcColor, rotation, src.Size() / 2f, scale, SpriteEffects.None, 0f); // Draw underlying item

			var bloom = AssetLoader.LoadedTextures["Bloom"].Value; // Then draw a soft glow and the actual glowmask
			Main.spriteBatch.Draw(bloom, position, null, Color.Pink.Additive(0) * 0.4f, rotation, bloom.Size() / 2f, scale * 0.3f, SpriteEffects.None, 0f);
			Main.spriteBatch.Draw(Glow.Value, position, src, Color.Lerp(npcColor, Color.White, 1f) * 1f, rotation, src.Size() / 2f, scale, SpriteEffects.None, 0f);
			return;
		}

		orig(npc, typeCache, npcColor, addedRotation);
	}

	public override void SetStaticDefaults()
	{
		SlimeItemDatabase.AddLoot(new SlimeItemDatabase.ConditionalLoot(SlimeItemDatabase.MatchId(NPCID.BlackSlime, NPCID.YellowSlime, NPCID.RedSlime), 0.01f, Type));
		NPCLootDatabase.AddLoot(new NPCLootDatabase.ConditionalLoot(NPCLootDatabase.MatchId(NPCID.ShimmerSlime), ItemDropRule.Common(Type, 60)));
	}

	public override void SetDefaults()
	{
		Item.CloneDefaults(ItemID.Radar);
		Item.Size = new(24, 32);
	}

	public override void UpdateInfoAccessory(Player player) => player.GetModPlayer<ScryingPlayer>().Enabled = true;
}

internal class ScryingPlayer : ModPlayer
{
	public bool Enabled = false;

	public override void ResetInfoAccessories() => Enabled = true;

	public override void RefreshInfoAccessoriesFromTeamPlayers(Player otherPlayer)
	{
		if (otherPlayer.GetModPlayer<ScryingPlayer>().Enabled)
		{
			Enabled = true;
		}
	}
}

internal class ScryingItem : GlobalItem
{
	public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
	{
		if (!Main.LocalPlayer.GetModPlayer<ScryingPlayer>().Enabled)
			return;

		Color color;
		string text;
		int transform = GetTransformId(item);

		if (transform != -1)
		{
			text = Language.GetTextValue("Mods.SpiritReforged.Items.ScryingLens.ShimmerLine", transform, Lang.GetItemName(transform));
			color = ShimmerGradient();
		}
		else
			return;

		tooltips.Add(new TooltipLine(Mod, "ScryingLens", text) { OverrideColor = color });
	}

	private static int GetTransformId(Item item)
	{
		int id = ItemID.Sets.ShimmerCountsAsItem[item.type];

		if (id == -1)
			id = item.type;

		int transform = ItemID.Sets.ShimmerTransformToItem[id];
		return transform;
	}

	internal static Color ShimmerGradient()
	{
		Color color;
		float factor = MathF.Sin(Main.GameUpdateCount * 0.02f) * 0.5f + 0.5f;
		color = Color.Lerp(Color.Lerp(Color.White, new Color(150, 214, 245), factor), Color.Lerp(new Color(150, 214, 245), new Color(240, 146, 251), factor), factor);
		return color;
	}

	public override bool PreDrawTooltipLine(Item item, DrawableTooltipLine line, ref int yOffset)
	{
		if (line.Mod == Mod.Name && line.Name == "ScryingLens")
		{
			// Draw icon
			var icon = TextureAssets.Item[ModContent.ItemType<ScryingLens>()].Value;
			var position = new Vector2(line.X, line.Y) + new Vector2(10, 10 + (float)Math.Sin(Main.timeForVisualEffects / 80f) * 2f);
			Main.spriteBatch.Draw(icon, position, null, Color.White, 0, icon.Size() / 2, Math.Max(icon.Width, icon.Height) / 38f, default, 0);

			// Draw text
			line.X += 16;
			string text = line.Text.Replace("{0}", string.Empty);
			Utils.DrawBorderString(Main.spriteBatch, text, new Vector2(line.X, line.Y), ShimmerGradient().Additive(50));
			return false;
		}

		return true;
	}
}

public class ScryingLensInfoDisplay : InfoDisplay
{
	public static LocalizedText ShowingShimmerText { get; private set; }

	public override string HoverTexture => Texture + "Hover";

	public override void SetStaticDefaults() => ShowingShimmerText = this.GetLocalization("ShowingShimmer");
	public override bool Active() => Main.LocalPlayer.GetModPlayer<ScryingPlayer>().Enabled;
	public override string DisplayValue(ref Color displayColor, ref Color displayShadowColor) => ShowingShimmerText.Value;
}