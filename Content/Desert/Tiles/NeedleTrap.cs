using SpiritReforged.Common.Easing;
using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.TileCommon.TileMerging;
using Terraria.Audio;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Desert.Tiles;

public class NeedleTrap : ModTile, IAutoloadTileItem
{
	public static readonly SoundStyle Extend = new("SpiritReforged/Assets/SFX/Tile/SpikeTrapExtend") { MaxInstances = 3 };
	public static readonly SoundStyle Retract = new("SpiritReforged/Assets/SFX/Tile/SpikeTrapRetract") { MaxInstances = 3 };

	public override void SetStaticDefaults()
	{
		TileID.Sets.DrawsWalls[Type] = true;
		TileID.Sets.DontDrawTileSliced[Type] = true;
		TileID.Sets.IgnoresNearbyHalfbricksWhenDrawn[Type] = true;

		Main.tileSolid[Type] = true;
		Main.tileBlockLight[Type] = true;
		Main.tileFrameImportant[Type] = true;

		AddMapEntry(new Color(174, 74, 48), Language.GetText("MapObject.Trap"));
		this.Merge(ModContent.TileType<RedSandstoneBrick>(), ModContent.TileType<RedSandstoneBrickCracked>());

		DustType = DustID.DynastyShingle_Red;
		this.AutoItem().ResearchUnlockCount = 100;
	}

	public override void FloorVisuals(Player player)
	{
		if (player.whoAmI == Main.myPlayer)
		{
			var tileCoords = player.Bottom.ToTileCoordinates16();
			var tile = Framing.GetTileSafely(tileCoords);

			if (tile.HasUnactuatedTile && tile.TileType == Type && Wiring.CheckMech(tileCoords.X, tileCoords.Y, NeedleTrapProj.TimeLeftMax))
				SpawnSpike(new EntitySource_TileInteraction(player, tileCoords.X, tileCoords.Y), tileCoords);
		}
	}

	public override void HitWire(int i, int j) //Allow this trap to be triggered using wire in addition to being stepped on
	{
		if (Wiring.CheckMech(i, j, NeedleTrapProj.TimeLeftMax))
			SpawnSpike(Wiring.GetProjectileSource(i, j), new(i, j));
	}

	public override bool IsTileDangerous(int i, int j, Player player) => true;
	private static void SpawnSpike(IEntitySource source, Point16 tileCoords)
	{
		Vector2 position = tileCoords.ToWorldCoordinates(8, Framing.GetTileSafely(tileCoords).IsHalfBlock ? 16 : 8);
		Projectile.NewProjectile(source, position, Vector2.Zero, ModContent.ProjectileType<NeedleTrapProj>(), 30, 0);
	}

	public override void PostDraw(int i, int j, SpriteBatch spriteBatch) => TileMerger.DrawMerge(spriteBatch, i, j, ModContent.TileType<RedSandstoneBrick>(), ModContent.TileType<RedSandstoneBrickCracked>());
}

public class NeedleTrapProj : ModProjectile
{
	public const int TimeLeftMax = 100;
	private Vector2 _origin;

	private float GetProgress(int substract = 0) => MathHelper.Clamp(1f - (float)(Projectile.timeLeft - substract) / (TimeLeftMax - substract), 0, 1);

	public override void SetStaticDefaults() => ProjectileID.Sets.DontAttachHideToAlpha[Type] = true;

	public override void SetDefaults()
	{
		Projectile.Size = new(12);
		Projectile.friendly = true;
		Projectile.hostile = true;
		Projectile.timeLeft = TimeLeftMax;
		Projectile.tileCollide = true;
		Projectile.ignoreWater = true;
		Projectile.penetrate = -1;
		Projectile.hide = true;
	}

	public override void AI()
	{
		if (_origin == default) //Just spawned
		{
			_origin = Projectile.Center;
			SoundEngine.PlaySound(NeedleTrap.Extend, Projectile.Center);
		}

		float progress = GetProgress();
		float ease = (progress > 0.9f) ? (1f - (progress - 0.9f) / 0.1f) : EaseFunction.EaseCubicIn.Ease(GetProgress(90));
		int distance = (progress is < 0.15f or > 0.85f) ? 16 : 14;

		Projectile.Center = _origin - new Vector2(0, ease * distance);

		if (Projectile.timeLeft == 10)
			SoundEngine.PlaySound(NeedleTrap.Retract, Projectile.Center);
	}

	public override bool PreDraw(ref Color lightColor)
	{
		var texture = TextureAssets.Projectile[Type].Value;
		Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition + new Vector2(0, Projectile.gfxOffY), null, Projectile.GetAlpha(lightColor), Projectile.rotation, texture.Size() / 2, Projectile.scale, default);

		return false;
	}

	public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI) => behindNPCsAndTiles.Add(index);
}