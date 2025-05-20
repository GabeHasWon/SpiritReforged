using SpiritReforged.Common.ProjectileCommon;
using SpiritReforged.Common.Visuals.Glowmasks;
using SpiritReforged.Common.WorldGeneration;
using Terraria.Audio;
using Terraria.WorldBuilding;

namespace SpiritReforged.Content.Underground.Moss.MossFlasks;

public abstract class MossFlask : ModItem
{
	public override void SetDefaults()
	{
		Item.CloneDefaults(ItemID.HolyWater);
		Item.damage = 10;
		Item.rare = ItemRarityID.Blue;
		Item.value = Item.sellPrice(0, 0, 1, 0);

		if (Mod.TryFind(Name + "Projectile", out ModProjectile p))
			Item.shoot = p.Type;
	}
}

public abstract class MossFlaskProjectile : ModProjectile
{
	public readonly record struct MossConversion(ushort Stone, ushort Brick, ushort Plant = TileID.LongMoss)
	{
		public readonly ushort Stone = Stone;
		public readonly ushort Brick = Brick;
		public readonly ushort Plant = Plant;
	}

	private static ushort[] StoneTypes;
	private static ushort[] BrickTypes;
	private static ushort[] PlantTypes;

	private int _itemType = -1;
	public int ItemType
	{
		get
		{
			if (_itemType != -1)
				return _itemType;

			return _itemType = Mod.Find<ModItem>(Name.Replace("Projectile", string.Empty)).Type;
		}
	}

	public abstract MossConversion Conversion { get; }

	public override string Texture => base.Texture.Replace("Projectile", string.Empty);
	public override LocalizedText DisplayName => ItemLoader.GetItem(ItemType).DisplayName;

	public override void SetStaticDefaults()
	{
		if (StoneTypes == null && BrickTypes == null)
		{
			List<ushort> stone = [];
			List<ushort> brick = [];
			List<ushort> plant = [];

			stone.Add(TileID.Stone);
			brick.Add(TileID.GrayBrick);
			plant.Add(TileID.LongMoss);

			foreach (var flask in Mod.GetContent<MossFlaskProjectile>())
			{
				stone.Add(flask.Conversion.Stone);
				brick.Add(flask.Conversion.Brick);

				ushort plantValue = flask.Conversion.Plant;

				if (plantValue != 0)
					plant.Add(plantValue);
			} //Gather a list based on registered Types

			StoneTypes = [.. stone];
			BrickTypes = [.. brick];
			PlantTypes = [.. plant];
		}
	}

	public override void SetDefaults() => Projectile.CloneDefaults(ProjectileID.HolyWater);
	public override bool? CanCutTiles() => false;

	public override void OnKill(int timeLeft)
	{
		const int area = 5;

		if (Main.netMode != NetmodeID.MultiplayerClient)
		{
			var pt = Projectile.Center.ToTileCoordinates();
			ShapeData data = new();

			WorldUtils.Gen(pt, new Shapes.Circle(area), Actions.Chain(new Modifiers.OnlyTiles(StoneTypes), new Modifiers.SkipTiles(Conversion.Stone), new SolidIsTouchingAir(true), new ReplaceType(Conversion.Stone).Output(data)));
			WorldUtils.Gen(pt, new Shapes.Circle(area), Actions.Chain(new Modifiers.OnlyTiles(BrickTypes), new Modifiers.SkipTiles(Conversion.Brick), new SolidIsTouchingAir(true), new ReplaceType(Conversion.Brick).Output(data)));
			WorldUtils.Gen(pt, new Shapes.Circle(area), Actions.Chain(new Modifiers.OnlyTiles(PlantTypes), new Modifiers.SkipTiles(Conversion.Plant), new ReplaceType(Conversion.Plant).Output(data)));

			WorldUtils.Gen(pt, new ModShapes.All(data), Actions.Chain(new Actions.SetFrames(true), new Send()));
		}

		SoundEngine.PlaySound(SoundID.Shatter, Projectile.Center);
		CreateDust(DustID.KryptonMoss);

		for (int i = 0; i < 10; i++)
			Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Glass);
	}

	public virtual void CreateDust(int type)
	{
		for (int i = 0; i < 8; i++)
			Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, type).fadeIn = 1.2f;
	}

	public override bool PreDraw(ref Color lightColor)
	{
		Projectile.QuickDraw();

		if (GlowmaskItem.ItemIdToGlowmask.TryGetValue(ItemType, out var data))
		{
			var glow = data.Glowmask.Value;
			Main.EntitySpriteDraw(glow, Projectile.Center - Main.screenPosition, null, Projectile.GetAlpha(data.GetDrawColor(Projectile)), Projectile.rotation, glow.Size() / 2, Projectile.scale, default);
		}

		return false;
	}
}