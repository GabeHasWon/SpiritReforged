using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.PlayerCommon;
using SpiritReforged.Common.ProjectileCommon.Abstract;
using SpiritReforged.Common.Visuals.Glowmasks;
using Terraria;
using Terraria.GameContent.ItemDropRules;

namespace SpiritReforged.Content.Underground.Items.BigBombs;

[AutoloadGlowmask("255,255,255")]
public class BoomShroom : AccessoryItem
{
	public override void SetStaticDefaults()
	{
		CrateDatabase.AddCrateRule(ItemID.WoodenCrate, new CommonDrop(Type, 13));
		CrateDatabase.AddCrateRule(ItemID.WoodenCrateHard, new CommonDrop(Type, 13));
	}

	public override void SetDefaults()
	{
		Item.width = 28;
		Item.height = 20;
		Item.value = Item.sellPrice(0, 3, 0, 0);
		Item.rare = ItemRarityID.Blue;
		Item.accessory = true;
	}

	public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
	{
		Lighting.AddLight(Item.Center, new Vector3(.5f, .1f, .1f));
		return true;
	}
}

internal class BoomShroomPlayer : ModPlayer
{
	private static readonly Dictionary<int, int> SmallToLarge = [];

	public static int MakeLarge(int type)
	{
		if (SmallToLarge.TryGetValue(type, out int value))
			return value;

		return type;
	}

	public override void SetStaticDefaults()
	{
		foreach (var p in Mod.GetContent<BombProjectile>())
		{
			if (p is ILargeExplosive large)
				SmallToLarge.Add(large.OriginalType, p.Type);
		}
	}

	public override float UseSpeedMultiplier(Item item)
	{
		if (Player.HasAccessory<BoomShroom>() && SmallToLarge.ContainsKey(item.shoot))
			return 0.5f;

		return 1f;
	}

	public override void ModifyShootStats(Item item, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
	{
		if (Player.HasAccessory<BoomShroom>() && SmallToLarge.TryGetValue(type, out int t))
			type = t;
	}
}

/// <summary> Registers this projectile for use with <see cref="BoomShroom"/>. </summary>
internal interface ILargeExplosive
{
	/// <summary> The type of projectile that this will replace when <see cref="BoomShroom"/> is equipped. </summary>
	public int OriginalType { get; }
}