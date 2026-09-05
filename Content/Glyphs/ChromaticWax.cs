using Humanizer;
using ReLogic.Graphics;
using SpiritReforged.Common.ConfigurationCommon;
using SpiritReforged.Common.Easing;
using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.ModCompat;
using SpiritReforged.Common.ModCompat.Classic;
using SpiritReforged.Common.ModCompat.LocalizationTools;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.PrimitiveRendering;
using SpiritReforged.Common.PrimitiveRendering.PrimitiveShape;
using SpiritReforged.Common.ProjectileCommon;
using SpiritReforged.Common.ProjectileCommon.Abstract;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Common.Visuals.Glowmasks;
using SpiritReforged.Content.Particles;
using System.IO;
using System.Linq;
using System.Reflection;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.ItemDropRules;
using Terraria.ModLoader.IO;
using static SpiritReforged.Content.Glyphs.CelestialStamp;

namespace SpiritReforged.Content.Glyphs;

[FromClassic("Glyph")]
public class ChromaticWax : ModItem
{
	/// <summary> A pulsing rainbow color used for visual effects. </summary>
	public static Color SpecialColor => Main.hslToRgb((float)Main.timeForVisualEffects / 300f % 1f, 1, 0.8f);

	/// <summary> The item origin used for visual effects. </summary>
	private Vector2 Center => Item.Center - Vector2.UnitY * EaseFunction.EaseSine.Ease((float)Main.timeForVisualEffects / 90f) * 3;

	public static readonly Asset<Texture2D> WorldTexture = DrawHelpers.RequestLocal<ChromaticWax>("ChromaticWax_World", false);

	public override void SetStaticDefaults()
	{
		Item.ResearchUnlockCount = 5;

		ItemLootDatabase.AddItemRule(ItemID.GoldenCrate, ItemDropRule.Common(Type, 6, 2, 3));
		ItemLootDatabase.AddItemRule(ItemID.GoldenCrateHard, ItemDropRule.Common(Type, 6, 2, 3));
	}

	public override void SetDefaults()
	{
		Item.width = 28;
		Item.height = 28;
		Item.value = Item.sellPrice(silver: 10);
		Item.rare = ItemRarityID.Blue;
		Item.maxStack = Item.CommonMaxStack;
	}

	public override void Update(ref float gravity, ref float maxFallSpeed)
	{
		if (Main.rand.NextBool(10))
			ParticleHandler.SpawnParticle(new EmberParticle(Center + Main.rand.NextVector2Circular(10, 10), Vector2.UnitY * -Main.rand.NextFloat(0.1f, 1f), SpecialColor, 1, 30, 2));
	}

	public override void ModifyTooltips(List<TooltipLine> tooltips)
	{
		if (tooltips.FindIndex(static x => x.Name == "ItemName") is int index && index != -1)
			tooltips[index].OverrideColor = SpecialColor;
	}

	public override void PostDrawTooltipLine(DrawableTooltipLine line)
	{
		if (line.Name == "ItemName")
			DrawNameGlow(line.Text, line.Font, new(line.X, line.Y));
	}

	public static void DrawNameGlow(string text, DynamicSpriteFont font, Point origin)
	{
		Vector2 lineSize = font.MeasureString(text);
		Rectangle source = new(origin.X, origin.Y, (int)lineSize.X, (int)lineSize.Y);
		Color color = SpecialColor.Additive();

		Texture2D bloom = AssetLoader.LoadedTextures["Bloom"].Value;
		Main.EntitySpriteDraw(bloom, source.Center(), null, color * 0.25f, 0, bloom.Size() / 2, new Vector2(1f / bloom.Width * source.Width * 1.5f, 1f / bloom.Height * source.Height), default);

		DrawStar(new(source.X, source.Y), color * 0.15f, 22);
		DrawStar(new(source.X + source.Width * 0.5f, source.Y + source.Height * 0.7f), color * 0.3f, 30);
		DrawStar(new(source.Right, source.Y + source.Height * 0.3f), color * 0.1f, 25);

		static void DrawStar(Vector2 position, Color color, float duration)
		{
			double time = Main.timeForVisualEffects;
			float fullDuration = duration * 5;
			float opacity = (float)EaseFunction.EaseSine.Ease((float)time / fullDuration);

			position += Vector2.UnitX * (float)(time / fullDuration % 1) * 10;

			Texture2D texture = AssetLoader.LoadedTextures["Star"].Value;
			Main.spriteBatch.Draw(texture, position, null, color * opacity, (float)Main.timeForVisualEffects * 0.01f, texture.Size() / 2, 0.1f, 0, 0);
			Main.spriteBatch.Draw(texture, position, null, color.Additive() * opacity * 0.5f, (float)Main.timeForVisualEffects * 0.01f, texture.Size() / 2, 0.08f, 0, 0);
		}
	}

	public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
	{
		Texture2D texture = WorldTexture.Value;
		float itemScale = scale;
		float itemRotation = rotation;

		DrawHelpers.DrawOutline(default, default, default, default, (offset) =>
			spriteBatch.Draw(texture, Center - Main.screenPosition + offset, null, Item.GetAlpha(SpecialColor.Additive()), itemRotation, texture.Size() / 2, itemScale, 0, 0));

		Texture2D star = AssetLoader.LoadedTextures["StarChromatic"].Value;
		spriteBatch.Draw(star, Center - Main.screenPosition, null, Item.GetAlpha(SpecialColor.Additive()) * 0.8f, 0, star.Size () / 2, itemScale * (0.05f + 0.005f * (float)EaseFunction.EaseSine.Ease((float)Main.timeForVisualEffects / 65f)), 0, 0);

		spriteBatch.Draw(texture, Center - Main.screenPosition, null, Item.GetAlpha(lightColor), itemRotation, texture.Size() / 2, itemScale, 0, 0);
		return false;
	}
}

#region common & globals
public class GlyphGlobalNPC : GlobalNPC
{
	public override void OnKill(NPC npc)
	{
		if (npc.boss && Main.BestiaryTracker.Kills.GetKillCount(npc) == 1)
			DropGlyphs(npc, npc.GetSource_Death()); //Drop unlisted glyphs on first boss death
	}

	public override void ModifyNPCLoot(NPC npc, NPCLoot npcLoot)
	{
		if (npc.type is NPCID.Tim or NPCID.RuneWizard or NPCID.GoblinSummoner)
		{
			npcLoot.Add(ItemDropRule.ByCondition(new Conditions.IsExpert(), ModContent.ItemType<ChromaticWax>(), 1, 1, 2));
			npcLoot.Add(ItemDropRule.ByCondition(new Conditions.NotExpert(), ModContent.ItemType<ChromaticWax>()));
		}

		if (npc.type is NPCID.DarkCaster or NPCID.GoblinSorcerer)
		{
			npcLoot.Add(ItemDropRule.NormalvsExpert(ModContent.ItemType<ChromaticWax>(), 50, 40));
		}

		// Crossmod Wax Drops
		if (CrossMod.Thorium.Enabled)
		{
			if (CrossMod.Thorium.TryFind("BlueHag", out ModNPC blueHag) && CrossMod.Thorium.TryFind("GreenHag", out ModNPC greenHag) && CrossMod.Thorium.TryFind("CyanHag", out ModNPC cyanHag) && CrossMod.Thorium.TryFind("RedHag", out ModNPC redHag))
			{
				int[] hags = { blueHag.Type, greenHag.Type, redHag.Type, cyanHag.Type };

				if (hags.Contains(npc.type))
					npcLoot.Add(ItemDropRule.NormalvsExpert(ModContent.ItemType<ChromaticWax>(), 50, 40));
			}

			if (CrossMod.Thorium.TryFind("Illusionist", out ModNPC illusionist) && npc.type == illusionist.Type)
			{
				npcLoot.Add(ItemDropRule.ByCondition(new Conditions.IsExpert(), ModContent.ItemType<ChromaticWax>(), 1, 1, 2));
				npcLoot.Add(ItemDropRule.ByCondition(new Conditions.NotExpert(), ModContent.ItemType<ChromaticWax>()));
			}
		}

		if (CrossMod.Redemption.Enabled)
		{
			if (CrossMod.Redemption.TryFind("MoonflareCaster", out ModNPC moonflareCaster) && npc.type == moonflareCaster.Type)
				npcLoot.Add(ItemDropRule.NormalvsExpert(ModContent.ItemType<ChromaticWax>(), 50, 40));
		}
	}

	public static int DropGlyphs(NPC npc, IEntitySource source)
	{
		int stack = (int)Math.Max(npc.value / Item.gold, 3);

		if (CrossMod.Fables.Enabled && CrossMod.Fables.TryFind("ScourgeVsScarab", out ModNPC modNPC) && npc.type == modNPC.Type)
			stack = 10; //Override chromatic wax amount

		return Item.NewItem(source, npc.Hitbox, new Item(ModContent.ItemType<ChromaticWax>(), stack + Main.rand.Next(0, 3)));
	}
}

public class GlyphGlobalProjectile : GlobalProjectile
{
	public override bool InstancePerEntity => true;

	public GlyphItem.GlyphType glyph;

	public static bool TryGetGlyphFromContext(IEntitySource source, out GlyphItem.GlyphType glyphType)
	{
		if (source is IEntitySource_WithStatsFromItem { Item: Item item } && item.GetGlyph().ItemType > 0)
		{
			glyphType = item.GetGlyph(); //Transfer the associated item glyph to this projectile
			return true;
		}
		else if (source is EntitySource_Parent { Entity: Entity entity } && entity is Projectile parent && parent.GetGlyph().ItemType > 0)
		{
			glyphType = parent.GetGlyph(); //Transfer the parent projectile glyph to this projectile
			return true;
		}
		else
		{
			glyphType = default;
			return false;
		}
	}

	public override void OnSpawn(Projectile projectile, IEntitySource source)
	{
		if (TryGetGlyphFromContext(source, out GlyphItem.GlyphType glyphType))
		{
			glyph = glyphType; 
			projectile.netUpdate = true;
		}
	}

	public override void AI(Projectile projectile)
	{
		if (Main.dedServ || !ModContent.GetInstance<ReforgedClientConfig>().GlyphProjectileVisualEffects)
			return;

		if (projectile.GetGlyph() is GlyphItem.GlyphType glyph && glyph.ItemType > 0)
		{
			Player owner = Main.player[projectile.owner];
			if (owner.heldProj == projectile.whoAmI && projectile.ModProjectile is not BaseClubProj)
				return;

			int counts = owner.ownedProjectileCounts[projectile.type] - 1;

			if (Main.rand.NextFloat() > Math.Min(counts / 10f, 0.9f)) //Reduce odds with inversely with projectile counts of the same type
				(ItemLoader.GetItem(glyph.ItemType) as GlyphItem).UpdateGlyphProjectile(projectile);
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

		/// <summary> Prevents item consumption for the local client only. </summary>
		private static bool StopItemConsumption;

		private const int AnimationTimeMax = 20;
		private int _animationTime;

		public GlyphType Glyph { get; private set; }

		public void StartAnimation() => _animationTime = AnimationTimeMax;

		public bool HasGlyph(out GlyphItem glyphItem)
		{
			glyphItem = default;

			if (ItemLoader.GetItem(Glyph.ItemType) is GlyphItem _glyphItem)
				glyphItem = _glyphItem;

			return glyphItem != default;
		}

		/// <summary> Applies the provided glyph effect to <paramref name="item"/>. </summary>
		/// <param name="item"></param>
		/// <param name="type"></param>
		/// <param name="context"></param>
		/// <returns> Whether <paramref name="type"/> was successfully applied. </returns>
		public bool SetGlyph(Item item, GlyphType type, IApplicationContext context)
		{
			if (ItemLoader.GetItem(type.ItemType) is GlyphItem g && type == default) // removing glyph
				g.OnRemoveGlyph(item, context);

			if (type.ItemType == ItemID.None)
			{
				Glyph = type; //Remove glyph
				return true;
			}
			else if (ItemLoader.GetItem(type.ItemType) is GlyphItem glyphItem && glyphItem.CanApplyGlyph(item))
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

		public override bool AllowPrefix(Item item, int pre) => !HasGlyph(out _) || ModContent.GetInstance<CelestialStampToggle>().Active();  //No glyph effect is present

		public override bool CanReforge(Item item)
		{
			if (HasGlyph(out _) && !ModContent.GetInstance<CelestialStampToggle>().Active())
				SetGlyph(item, default, new ApplyContext()); //Remove the glyph

			return true;
		}

		public override bool CanRightClick(Item item) => Main.mouseItem.ModItem is GlyphItem glyphItem && glyphItem.CanApplyGlyph(item);

		public override void RightClick(Item item, Player player)
		{
			if (Main.mouseItem.ModItem is GlyphItem glyphItem && glyphItem.CanApplyGlyph(item))
			{
				item.SetGlyph(new(glyphItem.Type), new ApplyContext(player));

				if (--Main.mouseItem.stack <= 0)
					Main.mouseItem.TurnToAir(); //Consume the glyph on hand

				if (item.TryGetGlobalItem(out GlyphGlobalItem glyphGlobalItem))
					glyphGlobalItem.StartAnimation();

				StopItemConsumption = true;
			}
		}

		public override bool Shoot(Item item, Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
		{
			if (ModContent.GetInstance<ReforgedClientConfig>().GlyphItemVisualEffects && !item.channel && HasGlyph(out var glyphItem))
				glyphItem.GlyphShootEffects(item, player, source, position, velocity, type, damage, knockback);

			return base.Shoot(item, player, source, position, velocity, type, damage, knockback);
		}

		public override void ModifyWeaponCrit(Item item, Player player, ref float crit)
		{
			if (HasGlyph(out var glyphItem))
				glyphItem.ModifyGlyphedItemCrit(player, ref crit);
		}

		public override void ModifyWeaponDamage(Item item, Player player, ref StatModifier damage)
		{
			if (HasGlyph(out var glyphItem))
				glyphItem.ModifyGlyphedItemDamage(player, ref damage);
		}

		public override void ModifyWeaponKnockback(Item item, Player player, ref StatModifier knockback)
		{
			if (HasGlyph(out var glyphItem))
				glyphItem.ModifyGlyphedItemKnockback(player, ref knockback);
		}

		public override bool ConsumeItem(Item item, Player player)
		{
			bool value = StopItemConsumption;
			StopItemConsumption = false;
			return !value;
		}

		public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
		{
			if (HasGlyph(out var glyphItem))
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
			if (HasGlyph(out var glyphItem) && ModContent.GetInstance<ReforgedClientConfig>().GlyphItemVisualEffects)
				glyphItem.UpdateInWorld(item, ref gravity, ref maxFallSpeed);
		}

		public override bool PreDrawInWorld(Item item, SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
		{
			if (HasGlyph(out var glyphItem))
			{
				if (ModContent.GetInstance<ReforgedClientConfig>().GlyphItemVisualEffects)
				{
					glyphItem.DrawInWorld(item, spriteBatch, item.GetDrawParams(lightColor, rotation));
					return false;
				}
			}

			return true;
		}

		public override void PostDrawInInventory(Item item, SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
		{
			const int slotDimensions = 52;

			if (HasGlyph(out var glyphItem)) //Draw glyph inventory icons
			{
				Texture2D texture = glyphItem.IconTexture.Value;
				float iconScale = Main.inventoryScale;
				Vector2 iconPosition = position + (new Vector2(slotDimensions / 2, slotDimensions / 2) - texture.Size() / 2 - new Vector2(4)) * iconScale;

				DrawHelpers.DrawOutline(spriteBatch, texture, iconPosition, Color.White, (offset) =>
					spriteBatch.Draw(texture, iconPosition + offset, null, Color.White.Additive() * ((1f + (float)Math.Sin(Main.timeForVisualEffects / 30f)) * 0.1f), 0, texture.Size() / 2, iconScale, 0, 0));

				float progress = (float)_animationTime / AnimationTimeMax;
				spriteBatch.Draw(texture, iconPosition, null, Color.White * (1f - progress), 0, texture.Size() / 2, iconScale, 0, 0);

				if (_animationTime > 0)
				{
					Texture2D splashTexture = TextureAssets.Item[Glyph.ItemType].Value;
					float splashScale = (Math.Max((progress - 0.7f) * 4, 0) + 1) * Main.UIScale;

					DrawHelpers.DrawOutline(default, default, default, default, (offset) =>
						spriteBatch.Draw(splashTexture, position + offset, null, glyphItem.settings.Color.Additive(), 0, splashTexture.Size() / 2, EaseFunction.EaseQuinticOut.Ease(progress), 0, 0));

					Effect blurEffect = AssetLoader.LoadedShaders["BlurLine"].Value;
					SquarePrimitive coloredLine = new()
					{
						Position = position,
						Height = 20 * progress,
						Length = 200,
						Color = glyphItem.settings.Color.Additive()
					};

					PrimitiveRenderer.DrawPrimitiveShape(new SquarePrimitive()
					{
						Position = position,
						Height = 30 * progress,
						Length = 240,
						Color = Color.Black * 0.5f
					}, blurEffect, useUiMatrix: true);

					PrimitiveRenderer.DrawPrimitiveShape(coloredLine, blurEffect, useUiMatrix: true);
					PrimitiveRenderer.DrawPrimitiveShape(coloredLine, blurEffect, useUiMatrix: true);

					spriteBatch.Draw(splashTexture, position, null, Color.White * EaseFunction.EaseCubicOut.Ease(progress), 0, splashTexture.Size() / 2, splashScale, 0, 0);
				}

				if (_animationTime > 0)
					_animationTime--; //Update the application animation
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

	public static LocalizedText Gain => Language.GetText(glyphLocalization + "Gain");
	public static LocalizedText Target => Language.GetText(glyphLocalization + "Target");
	public static LocalizedText Enchant => Language.GetText(glyphLocalization + "RightClick");
	public LocalizedText Effect => this.GetLocalization("Effect");

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

	public virtual bool CanApplyGlyph(Item item) => item.damage > 0 && item.maxStack == 1 && item.TryGetGlobalItem(out GlyphGlobalItem glyphItem) && glyphItem.Glyph.ItemType != Type && !item.accessory;

	/// <summary> Called when this glyph effect is applied to <paramref name="item"/>. </summary>
	/// <param name="item"> The item being affected. </param>
	/// <param name="context"> The context in which this effect is being applied. Some examples include <see cref="ApplyContext"/>, <see cref="GenerateContext"/>, and <see cref="SyncContext"/>. </param>
	protected virtual void OnApplyGlyph(Item item, IApplicationContext context)
	{
		if (context is ApplyContext)
			SoundEngine.PlaySound(EnchantSound);

		if (!ModContent.GetInstance<CelestialStampToggle>().Active())
			item.prefix = 0; //Don't clear prefix if the celestial stamp is active

		item.ClearNameOverride();

		string itemName = item.Name;

		if (CrossMod.RussianLocalizable)
			itemName = itemName.ToLowerInvariant();

		item.SetNameOverride($"{GenderItemEffect(item, this.GetLocalizationKey(""))} " + itemName);

		if (item.rare < ItemRarityID.Purple)
			item.rare++;

		if (context is not SyncContext)
			item.Refresh(false); //Always prompts a netsync
	}

	protected virtual void OnRemoveGlyph(Item item, IApplicationContext context) { }

	private static string GenderItemEffect(Item item, string baseKey)
	{
		if (!CrossMod.RussianLocalizable)
			return Language.GetTextValue(baseKey + "Effect");

		string gender = RussianGendering.GetGender(item.type);

		return gender switch
		{
			"Feminine" => Language.GetTextValue(baseKey + "Gendered.Fem"),
			"Neuter" => Language.GetTextValue(baseKey + "Gendered.Neutral"),
			"Plural" => Language.GetTextValue(baseKey + "Gendered.Plural"),
			_ => Language.GetTextValue(baseKey + "Effect"),
		};
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

	public virtual void ModifyGlyphedItemCrit(Player player, ref float crit) { }
	public virtual void ModifyGlyphedItemDamage(Player player, ref StatModifier damage) { }
	public virtual void ModifyGlyphedItemKnockback(Player player, ref StatModifier knockBack) { }

	/// <summary> Used to modify drawing of glyph-affected items in the world. </summary>
	/// <param name="item"> The item being drawn. </param>
	/// <param name="spriteBatch"> The SpriteBatch being used. </param>
	public virtual void DrawInWorld(Item item, SpriteBatch spriteBatch, ItemMethods.ItemDrawParams parameters) => parameters.Draw();

	public virtual void UpdateInWorld(Item item, ref float gravity, ref float maxFallSpeed) { }

	public virtual void DrawHeldItem(ref PlayerDrawSet drawInfo, DrawData input) { }

	/// <summary> Visual effects that should happen when an item with a glyph shoots a projectile </summary>
	public virtual void GlyphShootEffects(Item item, Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) { }

	/// <summary> General update hook called for projectiles affected by glyphs, used only for particles and other visual effects </summary>
	public virtual void UpdateGlyphProjectile(Projectile projectile) { }
}
#endregion