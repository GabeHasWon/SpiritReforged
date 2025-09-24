using SpiritReforged.Common.ItemCommon.Abstract;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.NPCCommon;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Common.Visuals.Glowmasks;
using System.Reflection;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using Terraria.UI;

namespace SpiritReforged.Content.Aether.Items;

[AutoloadGlowmask("255,255,255")]
public class ScryingLens : InfoItem
{
	private class ScryingInfoElement(int npcType) : UIElement
	{
		public readonly int npcType = npcType;

		protected override void DrawSelf(SpriteBatch spriteBatch)
		{
			base.DrawSelf(spriteBatch);
			ScryingItem.DrawShinyIcon(GetDimensions().Center(), 0.75f);

			if (ContainsPoint(Main.MouseScreen))
				Main.instance.MouseText(Lang.GetNPCName(npcType).Value);
		}
	}

	internal class ScryingItem : GlobalItem
	{
		public const string LineName = "Shimmersight";

		/// <summary> Returns the appropriate shimmered item type resulting from <paramref name="item"/>.<para/>
		/// If <paramref name="item"/> releases an NPC, the shimmered NPC's caught item type is used instead. </summary>
		public static int GetTransformId(Item item)
		{
			int type = item.type;

			if (ItemID.Sets.ShimmerCountsAsItem[type] is int countsAs && countsAs != -1)
				type = countsAs;

			if (ItemID.Sets.ShimmerTransformToItem[type] is int shimmerType && shimmerType != -1)
				type = shimmerType;

			if (item.makeNPC != ItemID.None && NPCID.Sets.ShimmerTransformToNPC[item.makeNPC] is int npcType && npcType != -1)
			{
				if (ContentSamples.NpcsByNetId[npcType].catchItem is int catchItem && catchItem != 0)
					type = catchItem;
				else
					type = ItemID.DontHurtCrittersBook;
			}

			return (type == item.type) ? -1 : type;
		}

		public static Color GetShimmerGradient()
		{
			float factor = MathF.Sin(Main.GameUpdateCount * 0.02f) * 0.5f + 0.5f;
			var color = Color.Lerp(Color.Lerp(Color.White, new Color(150, 214, 245), factor), Color.Lerp(new Color(150, 214, 245), new Color(240, 146, 251), factor), factor);

			return color;
		}

		public static void DrawShinyIcon(Vector2 position, float scale)
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

			Main.spriteBatch.Draw(icon, position, null, Color.White, 0, icon.Size() / 2, scale, default, 0);
		}

		public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
		{
			if (!GetDisplay<ScryingLens>().Hidden && GetTransformId(item) is int shimmerType && shimmerType != -1)
			{
				const string whitespace = "   "; //Ensures appropriate text box padding after the item icon is inserted
				string text = Language.GetTextValue("Mods.SpiritReforged.Items.ScryingLens.ShimmerLine", shimmerType, Lang.GetItemName(shimmerType));

				tooltips.Add(new TooltipLine(Mod, LineName, text + whitespace) { OverrideColor = GetShimmerGradient() });
			}
		}

		public override bool PreDrawTooltipLine(Item item, DrawableTooltipLine line, ref int yOffset)
		{
			if (line.Mod == Mod.Name && line.Name == LineName)
			{
				// Draw icon
				var icon = TextureAssets.Item[ItemType].Value;
				var position = new Vector2(line.X, line.Y) + new Vector2(10, 10 + (float)Math.Sin(Main.timeForVisualEffects / 80f) * 2f);
				float scale = Math.Max(icon.Width, icon.Height) / 40f;

				DrawShinyIcon(position, scale);
				Utils.DrawBorderString(Main.spriteBatch, line.Text, new Vector2(line.X + 16, line.Y), GetShimmerGradient().Additive(50));

				return false;
			}

			return true;
		}
	}

	public static int ItemType { get; private set; }

	public override void Load()
	{
		base.Load();

		On_Main.DrawNPC_SlimeItem += ModifyScryingLensSlimeItem;
		On_NamePlateInfoElement.ProvideUIElement += ProvideShimmerInfo;
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

	private static UIElement ProvideShimmerInfo(On_NamePlateInfoElement.orig_ProvideUIElement orig, NamePlateInfoElement self, BestiaryUICollectionInfo info)
	{
		var value = orig(self, info);
		if (typeof(NamePlateInfoElement).GetField("_npcNetId", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(self) is int netID)
		{
			if (NPCID.Sets.ShimmerTransformToNPC[ContentSamples.NpcsByNetId[netID].type] is int shimmerType && shimmerType != -1 && !GetDisplay<ScryingLens>().Hidden)
				value.Append(new ScryingInfoElement(shimmerType)
				{
					VAlign = 1f,
					Left = new(2, 0),
					Width = new(20, 0),
					Height = new(20, 0)
				});
		}

		return value;
	}

	public override void SetStaticDefaults()
	{
		ItemType = Type;

		SlimeItemDatabase.AddLoot(new SlimeItemDatabase.ConditionalItem(SlimeItemDatabase.MatchId(NPCID.BlackSlime, NPCID.YellowSlime, NPCID.RedSlime), 0.01f, Type));
		NPCLootDatabase.AddLoot(new NPCLootDatabase.ConditionalLoot(NPCLootDatabase.MatchId(NPCID.ShimmerSlime), ItemDropRule.Common(Type, 60)));
	}
}