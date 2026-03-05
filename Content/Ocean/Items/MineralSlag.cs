using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.ModCompat.Classic;
using SpiritReforged.Common.Particle;
using SpiritReforged.Content.Particles;
using Terraria.Utilities;

namespace SpiritReforged.Content.Ocean.Items;

[FromClassic("DeepCascadeShard")]
public class MineralSlag : ModItem
{
	private struct ItemData(int itemType, int stack = 1) //Extractinator use
	{
		public int itemType = itemType;
		public int stack = stack;
	}

	public override void SetStaticDefaults()
	{
		ItemID.Sets.ExtractinatorMode[Type] = Type;
		VariantGlobalItem.AddVariants(Type, [new Point(20, 20), new Point(20, 20), new Point(20, 22), new Point(20, 22), new Point(20, 20)]);
		Item.ResearchUnlockCount = 25;
	}

	public override void SetDefaults()
	{
		Item.Size = new Vector2(20);
		Item.value = 100;
		Item.maxStack = Item.CommonMaxStack;
		Item.rare = ItemRarityID.Blue;
		Item.useTime = 10;
		Item.useAnimation = 15;
		Item.useStyle = ItemUseStyleID.Swing;
		Item.autoReuse = true;
		Item.consumable = true;
	}

	public override void ExtractinatorUse(int extractinatorBlockType, ref int resultType, ref int resultStack)
	{
		var choice = new WeightedRandom<ItemData>();
		choice.Add(new ItemData(ItemID.GoldCoin), .09f);
		choice.Add(new ItemData(ItemID.SilverCoin, Main.rand.Next(3) + 1), .7f);
		choice.Add(new ItemData(ItemID.CopperCoin, Main.rand.Next(40) + 1), 1.25f);

		choice.Add(new ItemData(ItemID.CopperOre, Main.rand.Next(3) + 1));
		choice.Add(new ItemData(ItemID.TinOre, Main.rand.Next(3) + 1));
		choice.Add(new ItemData(ItemID.IronOre, Main.rand.Next(3) + 1));
		choice.Add(new ItemData(ItemID.LeadOre, Main.rand.Next(3) + 1));
		choice.Add(new ItemData(ItemID.SilverOre, Main.rand.Next(3) + 1), .8f);
		choice.Add(new ItemData(ItemID.TungstenOre, Main.rand.Next(3) + 1), .8f);
		choice.Add(new ItemData(ItemID.GoldOre, Main.rand.Next(3) + 1), .5f);
		choice.Add(new ItemData(ItemID.PlatinumOre, Main.rand.Next(3) + 1), .5f);

		resultType = ((ItemData)choice).itemType;
		resultStack = ((ItemData)choice).stack;
	}
}

/// <summary> Non-functional variant of <see cref="MineralSlag"/> used for visual effects. </summary>
public class MineralSlagPickup : MineralSlag
{
	private const int timeLeftMax = 60 * 60 * 2;
	private int timeLeft = timeLeftMax;
	private bool collided = false;

	public override LocalizedText DisplayName => Language.GetText("Mods.SpiritReforged.Items.MineralSlag.DisplayName");
	public override LocalizedText Tooltip => Language.GetText("Mods.SpiritReforged.Items.MineralSlag.Tooltip");

	public override string Texture => base.Texture.Replace("Pickup", string.Empty);

	public override void PostDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, float rotation, float scale, int whoAmI)
	{
		if (timeLeft > timeLeftMax - 60 * 5) //Draw glow
		{
			float duration = 60f * 5;
			float intensity = (timeLeft - (timeLeftMax - duration)) / duration;

			var texture = TextureAssets.Item[Type].Value;
			var source = VariantGlobalItem.GetSource(Item);

			Main.EntitySpriteDraw(AssetLoader.LoadedTextures["Bloom"].Value, Item.Center - Main.screenPosition, null,
				(Color.Yellow with { A = 0 }) * .35f * intensity, rotation, AssetLoader.LoadedTextures["Bloom"].Size() / 2, Item.scale * .25f, SpriteEffects.None);

			for (int i = 0; i < 3; i++)
				Main.EntitySpriteDraw(texture, Item.Center - Main.screenPosition, source,
					Color.Orange with { A = 0 } * intensity, rotation, source.Size() / 2, Item.scale + .1f * i * intensity, SpriteEffects.None);
		}
	}

	public override void Update(ref float gravity, ref float maxFallSpeed)
	{
		if (--timeLeft <= 0)
			Item.active = false;

		if (!collided)
		{
			if (Item.velocity == Vector2.Zero)
			{
				Item.velocity = Vector2.UnitY * -1f;
				for (int i = 0; i < 10; i++)
				{
					var color = Color.Lerp(Color.Yellow, Color.Orange, Main.rand.NextFloat());
					float magnitude = Main.rand.NextFloat();

					ParticleHandler.SpawnParticle(new GlowParticle(Item.Bottom, Vector2.UnitY * -magnitude,
						color, (1f - magnitude) * .5f, Main.rand.Next(30, 120), 5, extraUpdateAction: delegate (Particle p)
						{
							p.Velocity = p.Velocity.RotatedBy(Main.rand.NextFloat(-.1f, .1f));
						}));
				}

				collided = true;
			}

			if (Main.rand.NextBool(3))
			{
				int type = Main.rand.NextBool() ? DustID.Torch : DustID.Ash;
				var dust = Dust.NewDustDirect(Item.position, Item.width, Item.height, type, 0, 0, 0, default, Main.rand.NextFloat(.5f, 2f));
				dust.noGravity = true;
				dust.velocity = -Item.velocity * Main.rand.NextFloat(.5f);

				if (type == DustID.Ash)
					dust.alpha = 150;
			}
		}
	}

	public override bool CanPickup(Player player) => true;

	public override bool OnPickup(Player player)
	{
		player.QuickSpawnItem(Item.GetSource_FromThis(), ModContent.ItemType<MineralSlag>(), Item.stack);
		return false;
	}
}
