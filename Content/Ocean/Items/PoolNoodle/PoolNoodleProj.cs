using SpiritReforged.Common.NPCCommon;
using SpiritReforged.Common.ProjectileCommon.Abstract;

namespace SpiritReforged.Content.Ocean.Items.PoolNoodle;

public class PoolNoodleProj : BaseWhipProj
{
	private int Style
	{
		get => (int)Projectile.ai[1];
		set => Projectile.ai[1] = value;
	}

	public override LocalizedText DisplayName => Language.GetText("Mods.SpiritReforged.Items.PoolNoodle.DisplayName");
	public override void StaticDefaults() => Main.projFrames[Type] = 7;

	public override void Defaults()
	{
		Projectile.WhipSettings.RangeMultiplier = .8f;
		Projectile.WhipSettings.Segments = 16;
	}

	public override void ModifyDraw(int segment, int numSegments, ref Rectangle frame)
	{
		Texture2D texture = TextureAssets.Projectile[Type].Value;
		frame.Width = texture.Width / 3;
		frame.X = 16 * Style;
	}

	public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
	{
		base.OnHitNPC(target, hit, damageDone);

		target.ApplySummonTag(3);
		target.AddBuff(ModContent.BuffType<PoolNoodleBubbleBuff>(), 600);
	}
}