using Humanizer;
using SpiritReforged.Common.Easing;
using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.ModCompat.Classic;
using SpiritReforged.Common.ProjectileCommon;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Common.Visuals.Glowmasks;
using System.IO;
using System.Linq;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ModLoader.IO;

namespace SpiritReforged.Content.Forest.Glyphs;

[FromClassic("Glyph")]
public class ChromaticWax : ModItem
{
	public override void SetStaticDefaults() => Item.ResearchUnlockCount = 5;

	public override void SetDefaults()
	{
		Item.width = 28;
		Item.height = 28;
		Item.value = 0;
		Item.rare = ItemRarityID.Quest;
		Item.maxStack = Item.CommonMaxStack;
	}
}

public class GlyphGlobalProjectile : GlobalProjectile
{
	public override bool InstancePerEntity => true;

	public GlyphItem.GlyphType glyph;

	public override void OnSpawn(Projectile projectile, IEntitySource source)
	{
		if (source is IEntitySource_WithStatsFromItem { Item: Item item } && item.GetGlyph() is GlyphItem.GlyphType itemGlyph && itemGlyph.ItemType > 0)
		{
			glyph = itemGlyph; //Transfer the associated item glyph to this projectile
			projectile.netUpdate = true;
		}
		else if (source is EntitySource_Parent { Entity: Entity entity } && entity is Projectile parent && parent.GetGlyph() is GlyphItem.GlyphType projGlyph && projGlyph.ItemType > 0)
		{
			glyph = projGlyph; //Transfer the parent projectile glyph to this projectile
			projectile.netUpdate = true;
		}
	}

	public override void SendExtraAI(Projectile projectile, BitWriter bitWriter, BinaryWriter binaryWriter) => binaryWriter.Write(glyph.ItemType);

	public override void ReceiveExtraAI(Projectile projectile, BitReader bitReader, BinaryReader binaryReader)
	{
		if (binaryReader.ReadInt32() is int itemType && ItemLoader.GetItem(itemType) is GlyphItem)
			glyph = new(itemType);
	}
}

[AutoloadGlowmask("255,255,255")]
public abstract class GlyphItem : ModItem
{
	public sealed class GlyphGlobalItem : GlobalItem
	{
		public override bool InstancePerEntity => true;

		private static float AnimationProgress;
		private static Item AnimationItem;
		/// <summary> Prevents item consumption for the local client only. </summary>
		private static bool StopItemConsumption;

		public GlyphType Glyph { get; private set; }

		public static void StartAnimation(Item item)
		{
			AnimationItem = item;
			AnimationProgress = 1;
		}

		/// <summary> Applies the provided glyph effect to <paramref name="item"/>. </summary>
		/// <param name="item"></param>
		/// <param name="type"></param>
		/// <returns> Whether <paramref name="type"/> was successfully applied. </returns>
		public bool SetGlyph(Item item, GlyphType type, IApplicationContext context)
		{
			if (ItemLoader.GetItem(type.ItemType) is GlyphItem glyphItem && glyphItem.CanApplyGlyph(item))
			{
				Glyph = type;
				glyphItem.OnApplyGlyph(item, context);

				return true;
			}

			return false;
		}

		public override void ApplyPrefix(Item item, int pre)
		{
			if (WorldGen.gen && WorldGen.genRand.NextBool(5)) //Randomly replace prefixes with Glyph effects on worldgen
			{
				GlyphItem[] array = Mod.GetContent<GlyphItem>().ToArray();
				GlyphItem glyphItem = array[WorldGen.genRand.Next(array.Length)];

				item.SetGlyph(new(glyphItem.Type), new GenerateContext());
			}
		}

		public override bool CanReforge(Item item) => Glyph == default; //No glyph effect is present

		public override bool CanRightClick(Item item) => Main.mouseItem.ModItem is GlyphItem glyphItem && glyphItem.CanApplyGlyph(item);

		public override void RightClick(Item item, Player player)
		{
			if (Main.mouseItem.ModItem is GlyphItem glyphItem && glyphItem.CanApplyGlyph(item))
			{
				item.SetGlyph(new(glyphItem.Type), new ApplyContext(player));

				if (--Main.mouseItem.stack <= 0)
					Main.mouseItem.TurnToAir(); //Consume the glyph on hand

				StartAnimation(item);
				StopItemConsumption = true;
			}
		}

		public override bool ConsumeItem(Item item, Player player)
		{
			bool value = StopItemConsumption;
			StopItemConsumption = false;
			return !value;
		}

		public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
		{
			if (Glyph != default && ItemLoader.GetItem(Glyph.ItemType) is GlyphItem glyphItem)
			{
				tooltips.AddRange(new List<TooltipLine>()
					{
						new(Mod, "GlyphEffect", glyphItem.Effect.Value) { OverrideColor = glyphItem.settings.Color },
						new(Mod, "GlyphTooltip", glyphItem.Tooltip.Value)
					}
				);
			}
		}

		public override void Update(Item item, ref float gravity, ref float maxFallSpeed)
		{
			if (Glyph != default && ItemLoader.GetItem(Glyph.ItemType) is GlyphItem g)
				g.UpdateGlyphItemInWorld(item);
		}

		public override bool PreDrawInWorld(Item item, SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
		{
			if (Glyph != default && ItemLoader.GetItem(Glyph.ItemType) is GlyphItem g)
			{
				Main.GetItemDrawFrame(item.type, out var texture, out Rectangle frame);

				Vector2 origin = frame.Size() / 2f;
				Vector2 position = item.Bottom - Main.screenPosition - new Vector2(0, origin.Y);

				g.PreDrawGlyphItem(item, texture, frame, spriteBatch, position, origin, rotation, scale);
			}

			return true;
		}

		public override void PostDrawInWorld(Item item, SpriteBatch spriteBatch, Color lightColor, Color alphaColor, float rotation, float scale, int whoAmI)
		{
			if (Glyph != default && ItemLoader.GetItem(Glyph.ItemType) is GlyphItem g)
			{
				Main.GetItemDrawFrame(item.type, out var texture, out Rectangle frame);

				Vector2 origin = frame.Size() / 2f;
				Vector2 position = item.Bottom - Main.screenPosition - new Vector2(0, origin.Y);

				g.PostDrawGlyphItem(item, texture, frame, spriteBatch, position, origin, rotation, scale);
			}
		}

		public override void PostDrawInInventory(Item item, SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
		{
			const int slotDimensions = 52;

			if (Glyph != default && ItemLoader.GetItem(Glyph.ItemType) is GlyphItem glyphItem) //Draw glyph inventory icons
			{
				Texture2D texture = glyphItem.IconTexture.Value;
				float iconScale = Main.inventoryScale;
				Vector2 iconPosition = position + (new Vector2(slotDimensions / 2, slotDimensions / 2) - texture.Size() / 2 - new Vector2(4)) * iconScale;

				DrawHelpers.DrawOutline(spriteBatch, texture, iconPosition, Color.White, (offset) =>
					spriteBatch.Draw(texture, iconPosition + offset, null, Color.White.Additive() * ((1f + (float)Math.Sin(Main.timeForVisualEffects / 30f)) * 0.1f), 0, texture.Size() / 2, iconScale, 0, 0));

				spriteBatch.Draw(texture, iconPosition, null, Color.White * (1f - AnimationProgress), 0, texture.Size() / 2, iconScale, 0, 0);

				if (AnimationItem == item && AnimationProgress > 0)
				{
					Texture2D splashTexture = TextureAssets.Item[Glyph.ItemType].Value;
					float splashScale = (Math.Max((AnimationProgress - 0.5f) * 2, 0) + 1) * Main.UIScale;

					spriteBatch.Draw(splashTexture, position, null, Color.White * EaseFunction.EaseCubicOut.Ease(AnimationProgress), 0, splashTexture.Size() / 2, splashScale, 0, 0);

					AnimationProgress -= 0.05f;
				}
			}
		}

		public override void NetSend(Item item, BinaryWriter writer) => writer.Write(Glyph.ItemType);

		public override void NetReceive(Item item, BinaryReader reader)
		{
			if (reader.ReadInt32() is int itemType && itemType != -1)
				item.SetGlyph(new(itemType), new SyncContext(255));
		}

		public override void SaveData(Item item, TagCompound tag)
		{
			if (Glyph.Name != null)
				tag[nameof(Glyph)] = Glyph.Name;
		}

		public override void LoadData(Item item, TagCompound tag)
		{
			if (tag.GetString(nameof(Glyph)) is string name && name != null && Mod.TryFind(name, out ModItem modItem))
				item.SetGlyph(new(modItem.Type), new LoadContext());
		}
	}

	#region application context
	public interface IApplicationContext;

	/// <summary> When an effect is applied as a result of being applied by a player. </summary>
	public readonly record struct ApplyContext(Player Player) : IApplicationContext;
	/// <summary> When an effect is applied as a result of being generated naturally. </summary>
	public readonly record struct GenerateContext : IApplicationContext;
	/// <summary> When an effect is applied as a result of network sync. </summary>
	public readonly record struct SyncContext(int ClientOrigin) : IApplicationContext;
	/// <summary> When an effect is applied as a result of data loading. </summary>
	public readonly record struct LoadContext : IApplicationContext;
	#endregion

	public readonly record struct GlyphType(int ItemType)
	{
		public string Name => ItemLoader.GetItem(ItemType)?.Name;
	}

	public readonly record struct GlyphSettings(Color Color);

	public LocalizedText Effect => this.GetLocalization("Effect");
	public static LocalizedText Gain => Language.GetText(glyphLocalization + "Gain");
	public static LocalizedText Target => Language.GetText(glyphLocalization + "Target");
	public static LocalizedText Enchant => Language.GetText(glyphLocalization + "RightClick");

	public Asset<Texture2D> IconTexture
	{
		get
		{
			if (IconByItemType.TryGetValue(Type, out Asset<Texture2D> texture))
			{
				return texture;
			}
			else
			{
				IconByItemType.Add(Type, ModContent.Request<Texture2D>(Texture + "_Icon"));
				return IconByItemType[Type];
			}
		}
	}

	private const string glyphLocalization = "Mods.SpiritReforged.Items.ChromaticWax.";
	private static readonly Dictionary<int, Asset<Texture2D>> IconByItemType = [];

	public static readonly SoundStyle EnchantSound = new("SpiritReforged/Assets/SFX/Item/GlyphAttach");
	public GlyphSettings settings;

	public override void SetStaticDefaults()
	{
		Item.ResearchUnlockCount = 5;
		Enchanter.SpecialShop.Add(Type, 3);
	}

	public virtual bool CanApplyGlyph(Item item) => item.damage >= 0 && item.TryGetGlobalItem(out GlyphGlobalItem glyphItem) && glyphItem.Glyph.ItemType != Type;

	/// <summary> Called when this glyph effect is applied to <paramref name="item"/>. </summary>
	/// <param name="item"> The item being affected. </param>
	/// <param name="context"> The context in which this effect is being applied. Some examples include <see cref="ApplyContext"/>, <see cref="GenerateContext"/>, and <see cref="SyncContext"/>. </param>
	protected virtual void OnApplyGlyph(Item item, IApplicationContext context)
	{
		if (context is ApplyContext)
			SoundEngine.PlaySound(EnchantSound);

		item.prefix = 0;
		item.ClearNameOverride();
		item.SetNameOverride($"{Effect} " + item.Name);

		if (item.rare < ItemRarityID.Purple)
			item.rare++;

		if (context is not SyncContext)
			item.Refresh(false); //Always prompts a netsync
	}

	public override void ModifyTooltips(List<TooltipLine> tooltips)
	{
		if (tooltips.FindIndex(static x => x.Name == "Tooltip0") is int index && index < 0)
			return;

		Color color = settings.Color * (Main.mouseTextColor / 255f);
		tooltips.Insert(index, new(Mod, "GlyphTooltip", Gain.Value.FormatWith(string.Format("{0:X2}{1:X2}{2:X2}", color.R, color.G, color.B), Effect))
		{
			OverrideColor = new Color(120, 190, 120)
		});

		tooltips.Insert(index, new(Mod, "GlyphHint", (Item.shopCustomPrice.HasValue ? Target : Enchant).Value)
		{
			OverrideColor = new Color(120, 190, 120)
		});
	}

	public virtual void PreDrawGlyphItem(Item item, Texture2D texture, Rectangle frame, SpriteBatch spriteBatch, Vector2 position, Vector2 origin, float rotation, float scale) { }

	public virtual void PostDrawGlyphItem(Item item, Texture2D texture, Rectangle frame, SpriteBatch spriteBatch, Vector2 position, Vector2 origin, float rotation, float scale) { }

	public virtual void UpdateGlyphItemInWorld(Item item) { }
}