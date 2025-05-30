using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Particle;
using SpiritReforged.Content.Particles;
using Terraria.Audio;

namespace SpiritReforged.Content.Forest.Misc.Remedy;

public class RemedyPotion : ModItem
{
	public static readonly SoundStyle Pop = new("SpiritReforged/Assets/SFX/Item/Bottle_Pop")
	{
		Pitch = 0.3f,
		PitchVariance = 0.1f
	};

	public override void SetStaticDefaults() => Item.ResearchUnlockCount = 20;

	public override void SetDefaults()
	{
		Item.width = 16;
		Item.height = 32;
		Item.rare = ItemRarityID.Blue;
		Item.maxStack = Item.CommonMaxStack;
		Item.useStyle = ItemUseStyleID.DrinkLiquid;
		Item.useTime = Item.useAnimation = 20;
		Item.consumable = true;
		Item.autoReuse = false;
		Item.buffType = ModContent.BuffType<RemedyPotionBuff>();
		Item.buffTime = 36000;
		Item.UseSound = SoundID.Item3;
		Item.value = Item.sellPrice(silver: 2);
	}

	public override bool? UseItem(Player player)
	{
		foreach (int type in RemedyPotionBuff.ImmuneTypes)
		{
			if (player.HasBuff(type))
			{
				DoHealVisuals(player);
				SoundEngine.PlaySound(Pop, player.Center);
				SoundEngine.PlaySound(SoundID.DD2_DarkMageCastHeal with { Pitch = .8f }, player.Center);
				break;
			}
		}

		return true;
	}

	private static void DoHealVisuals(Player player)
	{
		for (int i = 0; i < 3; i++)
		{
			var startColor = i == 2 ? Color.White : Color.Cyan;

			var ring = new TexturedPulseCircle(player.Bottom + Vector2.UnitY * i * -1.5f, startColor, Color.Green, 20, 80, 30, "supPerlin", new Vector2(1), EaseFunction.EaseCircularOut).WithSkew(.75f, -MathHelper.PiOver2);
			ring.Velocity = Vector2.UnitY * -1.2f;
			ParticleHandler.SpawnParticle(ring);
		}

		var rect = new Rectangle((int)player.BottomLeft.X, (int)player.BottomLeft.Y, player.width, 2);
		rect.Inflate(10, 0);

		for (int i = 0; i < 7; i++)
		{
			var pos = Main.rand.NextVector2FromRectangle(rect);
			var vel = Vector2.UnitY * -Main.rand.NextFloat(.5f, 3f);
			float scale = Main.rand.NextFloat(.25f, .5f);

			for (int l = 0; l < 2; l++)
			{
				var color = Color.Lerp(l == 0 ? Color.Cyan : Color.White, Color.Blue, Math.Abs((pos.X - player.Center.X) / 30f));
				if (l == 1)
					scale *= .75f;

				ParticleHandler.SpawnParticle(new GlowParticle(pos, vel, color, scale, (int)(scale * 120), 5, delegate (Particle p)
				{
					p.Velocity *= .95f;
				}).OverrideDrawLayer(ParticleLayer.AbovePlayer));
			}
		}
	}

	public override bool PreDrawTooltipLine(DrawableTooltipLine line, ref int yOffset)
	{
		const float iconScale = 1.3f;

		if (line.Mod != "Terraria" || line.Name != "Tooltip1")
			return true;

		int counter = 0;
		foreach (int buff in RemedyPotionBuff.ImmuneTypes)
		{
			var texture = TextureAssets.Buff[buff].Value;
			var origin = new Vector2(0, texture.Height / 2);

			Main.spriteBatch.Draw(texture, new Vector2(line.X + 24 * iconScale * counter, line.Y + 12), null, Color.White, 0, origin, Main.inventoryScale * iconScale, default, 0);
			counter++;
		}

		return false;
	}

	public override void AddRecipes() => CreateRecipe().AddIngredient(ItemID.BottledWater).AddIngredient(ItemID.Waterleaf)
		.AddIngredient(ItemID.Blinkroot).AddIngredient(ItemID.Stinger).AddTile(TileID.Bottles).Register();
}

public class RemedyPotionBuff : ModBuff
{
	public static readonly int[] ImmuneTypes = [BuffID.Poisoned, BuffID.Rabies, BuffID.Venom, BuffID.Weak, BuffID.Bleeding];

	public override void Update(Player player, ref int buffIndex)
	{
		foreach (int type in ImmuneTypes)
			player.buffImmune[type] = true;
	}
}