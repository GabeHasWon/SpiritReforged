using Terraria.Graphics.Shaders;

namespace SpiritReforged.Content.Desert.ScarabBoss.Items;

public class IridescentDye : ModItem
{
	public static int DyeID;

	public override void SetStaticDefaults()
	{
		Item.ResearchUnlockCount = 3;

		if (!Main.dedServ)
		{
			var shader = ModContent.Request<Effect>("SpiritReforged/Assets/Shaders/ScarabeusIridescentDye", AssetRequestMode.ImmediateLoad);
			GameShaders.Armor.BindShader(Type, new ArmorShaderData(shader, "IridescencePass"))
				.UseSaturation(0.15f)
				.UseOpacity(0.35f);
		}
	}

	public override void SetDefaults()
	{
		//Cache the dye ID, because the dye ID is automatically registered in BindShader, and we don't want it to get overriden in clonedefaults
		DyeID = Item.dye;
		Item.CloneDefaults(ItemID.AcidDye);
		Item.dye = DyeID;

	}
}