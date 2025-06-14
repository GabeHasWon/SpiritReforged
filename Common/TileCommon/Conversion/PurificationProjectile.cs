namespace SpiritReforged.Common.TileCommon.Conversion;

/// <summary> <see cref="ProjectileID.PurificationPowder"/> is excluded from <see cref="ConvertAdjacentSet"/>'s hook, so handle it here. </summary>
internal class PurificationProjectile : GlobalProjectile
{
	public override bool AppliesToEntity(Projectile entity, bool lateInstantiation) => entity.type is ProjectileID.PurificationPowder;

	public override bool PreAI(Projectile projectile)
	{
		ConvertAdjacentSet.SetType(BiomeConversionID.PurificationPowder);
		return true;
	}
	public override void PostAI(Projectile projectile) => ConvertAdjacentSet.SetType(-1);
}
