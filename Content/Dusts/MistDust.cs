namespace SpiritReforged.Content.Dusts;

public class MistDust : ModDust
{
	public override void OnSpawn(Dust dust)
	{
		dust.noGravity = true;
		dust.noLight = false;
	}

	public override bool Update(Dust dust)
	{
		dust.position += dust.velocity;
		dust.velocity.Y *= 1.01f;
		dust.velocity *= 0.96f;
		dust.rotation = dust.velocity.X * 0.2f;
		dust.scale -= 0.02f;

		if (dust.customData is NPC parent)
			dust.position += parent.velocity * 0.55f;
		
		if (dust.scale < 0.2f)
			dust.active = false;
		
		return false;
	}
}
