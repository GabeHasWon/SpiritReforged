using SpiritReforged.Common.Easing;
using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.TileCommon;
using Terraria.Audio;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Desert.Tiles;

public class NeedleTrap : ModTile, IAutoloadTileItem
{
	public override void SetStaticDefaults()
	{
		TileID.Sets.DrawsWalls[Type] = true;
		TileID.Sets.DontDrawTileSliced[Type] = true;
		TileID.Sets.IgnoresNearbyHalfbricksWhenDrawn[Type] = true;

		Main.tileSolid[Type] = true;
		Main.tileBlockLight[Type] = true;
		Main.tileFrameImportant[Type] = true;

		AddMapEntry(new Color(174, 74, 48), Language.GetText("MapObject.Trap"));

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

	private static void SpawnSpike(IEntitySource source, Point16 tileCoords) => Projectile.NewProjectile(source, tileCoords.ToWorldCoordinates(), Vector2.Zero, ModContent.ProjectileType<NeedleTrapProj>(), 30, 0);
}

public class NeedleTrapProj : ModProjectile
{
	public const int TimeLeftMax = 100;
	private Vector2 _origin;

	private float GetProgress(int substract = 0) => MathHelper.Clamp(1f - (float)(Projectile.timeLeft - substract) / (TimeLeftMax - substract), 0, 1);

	public override void SetDefaults()
	{
		Projectile.Size = new(12);
		Projectile.friendly = true;
		Projectile.hostile = true;
		Projectile.timeLeft = TimeLeftMax;
		Projectile.tileCollide = true;
		Projectile.ignoreWater = true;
		Projectile.hide = true;
	}

	public override void AI()
	{
		if (_origin == default) //Just spawned
		{
			_origin = Projectile.Center;
			SoundEngine.PlaySound(SoundID.Mech with { Volume = 0.5f, Pitch = -0.2f }, Projectile.Center);
		}

		float progress = GetProgress();
		float ease = (progress > 0.5f) ? EaseFunction.EaseCubicOut.Ease((1f - progress) * 2) : EaseFunction.EaseCubicIn.Ease(GetProgress(90));
		int distance = (progress < 0.15f) ? 16 : 14;

		Projectile.Center = _origin - new Vector2(0, ease * distance);
	}

	public override bool PreDraw(ref Color lightColor)
	{
		var texture = TextureAssets.Projectile[Type].Value;
		Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition + new Vector2(0, Projectile.gfxOffY), null, Projectile.GetAlpha(lightColor), Projectile.rotation, texture.Size() / 2, Projectile.scale, default);

		return false;
	}

	public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI) => behindNPCsAndTiles.Add(index);
}