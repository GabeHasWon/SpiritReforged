using SpiritReforged.Common;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Common.Visuals.Glowmasks;
using SpiritReforged.Content.SaltFlats.Tiles;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace SpiritReforged.Content.SaltFlats.Items;

[AutoloadEquip(EquipType.Head)]
[AutoloadGlowmask("255,255,255")]
public class MahakalaMaskBlue : ModItem
{
	public class MahakalaPlayer : ModPlayer
	{
		public float AuraStrength { get; private set; }

		private float AuraStrengthTarget => 1 - Math.Min(nearestEvil, lastHitByEvil) / 400f;

		public bool hasMask;
		public float nearestEvil = 0;
		public float lastHitByEvil = 0;

		public override void ResetEffects()
		{
			hasMask = false;
			lastHitByEvil += 4;
			nearestEvil = 400;

			foreach (NPC npc in Main.ActiveNPCs)
			{
				if (SpiritSets.IsCorrupt[npc.type] && npc.Distance(Player.Center) is { } dist and < 400)
				{
					nearestEvil = dist;
				}
			}

			AuraStrength = MathHelper.Lerp(AuraStrength, AuraStrengthTarget, 0.1f);
		}

		public override void ModifyHitByNPC(NPC npc, ref Player.HurtModifiers modifiers)
		{
			if (hasMask && SpiritSets.IsCorrupt[npc.type])
				modifiers.IncomingDamageMultiplier *= 0.8f;
		}

		public override void OnHitByNPC(NPC npc, Player.HurtInfo hurtInfo)
		{
			if (SpiritSets.IsCorrupt[npc.type])
				lastHitByEvil = 0;
		}

		public override void SetStaticDefaults() => TileLootSystem.RegisterLoot(
			static (loot) => loot.AddOneFromOptions(ModContent.ItemType<MahakalaMaskBlue>(), ModContent.ItemType<MahakalaMaskRed>(), 12), ModContent.TileType<StoneStupas>());
	}

	private class MahakalaGlowLayer : PlayerDrawLayer
	{
		private static readonly Asset<Texture2D> Glow = DrawHelpers.RequestLocal(typeof(MahakalaMaskBlue), "MahakalaGlow", false);

		public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.Head);

		protected override void Draw(ref PlayerDrawSet drawInfo)
		{
			MahakalaPlayer makahala = drawInfo.drawPlayer.GetModPlayer<MahakalaPlayer>();

			if (!makahala.hasMask || drawInfo.shadow != 0 || makahala.AuraStrength <= 0.05f)
				return;

			Vector2 pos = new Vector2((int)(drawInfo.Position.X - Main.screenPosition.X) + (drawInfo.drawPlayer.width - drawInfo.drawPlayer.bodyFrame.Width) / 2, 
				(int)(drawInfo.Position.Y - Main.screenPosition.Y) + drawInfo.drawPlayer.height - drawInfo.drawPlayer.bodyFrame.Height + 4) 
				+ drawInfo.drawPlayer.headPosition + drawInfo.rotationOrigin;

			int slot = ModContent.GetInstance<MahakalaMaskBlue>().Item.headSlot;
			Color color = (slot == drawInfo.drawPlayer.head ? Color.Pink : Color.White) * makahala.AuraStrength;
			Rectangle rect = new(0, 56 * (int)(Main.GameUpdateCount * 0.12f % 4), 40, 54);
			var drawData = new DrawData(Glow.Value, pos, rect, color, drawInfo.drawPlayer.headRotation, drawInfo.rotationOrigin, 1f, drawInfo.playerEffect, 0)
			{ shader = drawInfo.cHead };

			drawInfo.DrawDataCache.Add(drawData);
		}
	}

	public override void SetDefaults()
	{
		Item.Size = new(20);
		Item.defense = 2;
		Item.rare = ItemRarityID.Blue;
		Item.value = Item.sellPrice(silver: 50);
	}

	public override void UpdateEquip(Player player) => player.GetModPlayer<MahakalaPlayer>().hasMask = true;
	public override void AddRecipes() => CreateRecipe().AddRecipeGroup(RecipeGroupID.Wood, 10).AddRecipeGroup("Salt", 10).AddIngredient(ItemID.Sapphire).AddTile(TileID.Anvils).Register();
}

[AutoloadEquip(EquipType.Head)]
[AutoloadGlowmask("255,255,255")]
public class MahakalaMaskRed : MahakalaMaskBlue
{
	public override void AddRecipes() => CreateRecipe().AddRecipeGroup(RecipeGroupID.Wood, 10).AddRecipeGroup("Salt", 10).AddIngredient(ItemID.Ruby).AddTile(TileID.Anvils).Register();
}