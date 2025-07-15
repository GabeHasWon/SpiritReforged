using SpiritReforged.Common.ItemCommon.Abstract;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.NPCCommon;
using SpiritReforged.Common.PlayerCommon;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Common.Visuals.Glowmasks;
using Terraria.GameContent.ItemDropRules;

namespace SpiritReforged.Content.Aether.Items;

[AutoloadGlowmask("255,255,255")]
public class ScryingLens : InfoItem
{
	public static int ItemType { get; private set; }

	public override void Load()
	{
		AutoloadInfoDisplay();
		On_Main.DrawNPC_SlimeItem += ModifyScryingLensSlimeItem;
	}

	private static void ModifyScryingLensSlimeItem(On_Main.orig_DrawNPC_SlimeItem orig, NPC npc, int typeCache, Color npcColor, float addedRotation)
	{
		int itemType = (int)npc.ai[1];

		if (itemType == ModContent.ItemType<ScryingLens>())
		{
			Lighting.AddLight(npc.Center, Color.White.ToVector3() * 0.2f);
			Main.GetItemDrawFrame(itemType, out var itemTexture, out var src);

			var sb = Main.spriteBatch;
			float scale = 1f;
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
			sb.Draw(itemTexture, position, src, npcColor, rotation, src.Size() / 2f, scale, SpriteEffects.None, 0f); // Draw underlying item

			var bloom = AssetLoader.LoadedTextures["Bloom"].Value; // Then draw a soft glow and the actual glowmask
			sb.Draw(bloom, position, null, Color.Pink.Additive(0) * 0.4f, rotation, bloom.Size() / 2f, scale * 0.3f, SpriteEffects.None, 0f);
			sb.Draw(GlowmaskItem.ItemIdToGlowmask[ItemType].Glowmask.Value, position, src, Color.Lerp(npcColor, Color.White, 1f) * 1f, rotation, src.Size() / 2f, scale, SpriteEffects.None, 0f);
			return;
		}

		orig(npc, typeCache, npcColor, addedRotation);
	}

	public override void SetStaticDefaults()
	{
		ItemType = Type;

		SlimeItemDatabase.AddLoot(new SlimeItemDatabase.ConditionalItem(SlimeItemDatabase.MatchId(NPCID.BlackSlime, NPCID.YellowSlime, NPCID.RedSlime), 0.01f, Type));
		NPCLootDatabase.AddLoot(new NPCLootDatabase.ConditionalLoot(NPCLootDatabase.MatchId(NPCID.ShimmerSlime), ItemDropRule.Common(Type, 60)));
	}
}

internal class ScryingItem : GlobalItem
{
	public const string LineName = "Shimmersight";

	public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
	{
		if (Main.LocalPlayer.HasInfoItem<ScryingLens>() && GetTransformId(item) is int transform && transform != -1)
		{
			string text = Language.GetTextValue("Mods.SpiritReforged.Items.ScryingLens.ShimmerLine", transform, Lang.GetItemName(transform));
			tooltips.Add(new TooltipLine(Mod, LineName, text) { OverrideColor = GetShimmerGradient() });
		}
	}

	private static int GetTransformId(Item item)
	{
		int id = (ItemID.Sets.ShimmerCountsAsItem[item.type] is int counts && counts == -1) ? item.type : counts;
		int transform = ItemID.Sets.ShimmerTransformToItem[id];

		return transform;
	}

	internal static Color GetShimmerGradient()
	{
		float factor = MathF.Sin(Main.GameUpdateCount * 0.02f) * 0.5f + 0.5f;
		var color = Color.Lerp(Color.Lerp(Color.White, new Color(150, 214, 245), factor), Color.Lerp(new Color(150, 214, 245), new Color(240, 146, 251), factor), factor);

		return color;
	}

	public override bool PreDrawTooltipLine(Item item, DrawableTooltipLine line, ref int yOffset)
	{
		if (line.Mod == Mod.Name && line.Name == LineName)
		{
			// Draw icon
			var icon = TextureAssets.Item[ScryingLens.ItemType].Value;
			var position = new Vector2(line.X, line.Y) + new Vector2(10, 10 + (float)Math.Sin(Main.timeForVisualEffects / 80f) * 2f);
			float scale = Math.Max(icon.Width, icon.Height) / 40f;

			DrawShinyBackground(position, scale);
			Main.spriteBatch.Draw(icon, position, null, Color.White, 0, icon.Size() / 2, scale, default, 0);

			// Draw text
			string text = line.Text.Replace("{0}", string.Empty);
			Utils.DrawBorderString(Main.spriteBatch, text, new Vector2(line.X + 16, line.Y), GetShimmerGradient().Additive(50));

			return false;
		}

		return true;
	}

	private static void DrawShinyBackground(Vector2 position, float scale)
	{
		var sb = Main.spriteBatch;

		var icon = TextureAssets.Item[ModContent.ItemType<ScryingLens>()].Value;
		var texture = AssetLoader.LoadedTextures["Extra_49"].Value;
		var color = Main.hslToRgb(new Vector3((float)Main.timeForVisualEffects / 200f % 1, 0.5f, 0.5f)).Additive();

		sb.Draw(texture, position, null, color * 0.5f, 0, texture.Size() / 2, new Vector2(1.5f, 1) * 0.3f, default, 0);
		sb.Draw(texture, position, null, color * 0.5f, 0, texture.Size() / 2, new Vector2(2.5f, 0.5f) * 0.2f, default, 0);
		sb.Draw(texture, position, null, color * 0.5f, 0, texture.Size() / 2, new Vector2(2.5f, 0.25f) * 0.2f, default, 0);

		var outlineColor = GetShimmerGradient().Additive() * 0.4f;
		DrawHelpers.DrawOutline(sb, icon, position, outlineColor, (offset) => sb.Draw(icon, position + (offset * 2).RotatedBy(Main.timeForVisualEffects / 25f), null, outlineColor, 0, icon.Size() / 2, scale, default, 0));
	}
}