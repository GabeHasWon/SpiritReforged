namespace SpiritReforged.Content.Forest.Misc.Bonsai;

public class SakuraBonsaiItem : ModItem
{
	public virtual int Style => 0;
	public override void SetDefaults() => Item.DefaultToPlaceableTile(ModContent.TileType<BonsaiTrees>(), Style);
}

public class WillowBonsaiItem : SakuraBonsaiItem
{
	public override int Style => 1;
}

public class PurityBonsaiItem : SakuraBonsaiItem
{
	public override int Style => 2;
}

public class RubyBonsaiItem : SakuraBonsaiItem
{
	public override int Style => 3;
}

public class DiamondBonsaiItem : SakuraBonsaiItem
{
	public override int Style => 4;
}

public class EmeraldBonsaiItem : SakuraBonsaiItem
{
	public override int Style => 5;
}

public class SapphireBonsaiItem : SakuraBonsaiItem
{
	public override int Style => 6;
}

public class TopazBonsaiItem : SakuraBonsaiItem
{
	public override int Style => 7;
}

public class AmethystBonsaiItem : SakuraBonsaiItem
{
	public override int Style => 8;
}