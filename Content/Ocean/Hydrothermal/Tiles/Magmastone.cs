using SpiritReforged.Common.Multiplayer;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.Visuals.Glowmasks;
using SpiritReforged.Content.Ocean.Items;
using System.IO;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Ocean.Hydrothermal.Tiles;

[AutoloadGlowmask("Method:Content.Ocean.Hydrothermal.Tiles.Magmastone Glow")]
public class Magmastone : ModTile, IAutoloadTileItem
{
	#region glow
	/// <summary> The rate in which <see cref="glowPoints"/> value decays. </summary>
	private const float GlowDecayRate = .1f;

	/// <summary> Magmastone glow origin points, handled by the local client. </summary>
	private static readonly Dictionary<Point16, float> glowPoints = [];

	private static readonly HashSet<Point16> wireGlowPoints = [];

	/// <summary> Adds a Magmastone glow origin point, handled by the local client. </summary>
	/// <param name="i"> The X tile coordinate. </param>
	/// <param name="j"> The Y tile coordinate. </param>
	/// <returns> Whether the point was successfully added. </returns>
	public static bool AddGlowPoint(int i, int j) => glowPoints.TryAdd(new Point16(i, j), HydrothermalVent.eruptDuration * GlowDecayRate + 1);

	/// <summary> Toggles a non-duration based glow point for the local client. Can be synced using <see cref="MessageType.MagmaGlowPoint"/> in multiplayer. </summary>
	/// <param name="i"> The X tile coordinate. </param>
	/// <param name="j"> The Y tile coordinate. </param>
	public static void ToggleWireGlowPoint(int i, int j)
	{
		if (Main.dedServ)
			return;

		var pt = new Point16(i, j);

		if (!wireGlowPoints.Remove(pt))
			wireGlowPoints.Add(pt);
	}

	public static Color Glow(object obj)
	{
		const int range = 10;

		var pos = (Point)obj;
		var defaultColor = Lighting.GetColor(pos) * 2;

		foreach (var pt in wireGlowPoints)
		{
			if (pos.X == pt.X && pos.Y == pt.Y)
				return Color.White;
		}

		foreach (var pt in glowPoints)
		{
			if (pt.Key.ToVector2().Distance(pos.ToVector2()) <= range)
				return Color.Lerp(defaultColor, Color.White, pt.Value);
		}

		return defaultColor;
	}

	public override void Load() => On_Main.UpdateParticleSystems += UpdateGlow;

	/// <summary> Checks whether glow points should be removed and handles duration. </summary>
	private static void UpdateGlow(On_Main.orig_UpdateParticleSystems orig, Main self)
	{
		orig(self);

		if (Main.dedServ)
			return;

		foreach (var pt in wireGlowPoints) //Validity check
		{
			if (Framing.GetTileSafely(pt).TileType != ModContent.TileType<Magmastone>())
			{
				wireGlowPoints.Remove(pt);
				break;
			}
		}

		foreach (var key in glowPoints.Keys) //Update glowPoint durations
		{
			if ((glowPoints[key] -= GlowDecayRate) <= 0)
			{
				glowPoints.Remove(key);
				break;
			}
		}
	}
	#endregion;

	public void AddItemRecipes(ModItem item)
	{
		item.CreateRecipe(25).AddIngredient(ItemID.StoneBlock, 10).AddIngredient(ModContent.ItemType<MineralSlag>(), 1).AddTile(TileID.WorkBenches).Register();
		Recipe.Create(ItemID.Lava3Echo, 4).AddIngredient(item.Type).AddTile(TileID.WorkBenches).AddCondition(Condition.InGraveyard).Register();
	}

	public override void SetStaticDefaults()
	{
		Main.tileSolid[Type] = true;
		Main.tileMergeDirt[Type] = true;
		Main.tileBlockLight[Type] = true;
		TileID.Sets.CanBeDugByShovel[Type] = true;

		AddMapEntry(new Color(200, 160, 80));
		this.Merge(TileID.Sand, TileID.HardenedSand, ModContent.TileType<Gravel>(), TileID.Stone);

		DustType = DustID.Asphalt;
		MineResist = .5f;

		Mod.Find<ModItem>(Name + "Item").Item.ResearchUnlockCount = 100;
	}

	public override void HitWire(int i, int j)
	{
		if (Main.dedServ)
			new MagmaGlowData(new Point16(i, j)).Send();
		else
			ToggleWireGlowPoint(i, j);
	}
}

internal class MagmaGlowData : PacketData
{
	private readonly Point16 _point;

	public MagmaGlowData() { }
	public MagmaGlowData(Point16 point) => _point = point;

	public override void OnReceive(BinaryReader reader, int whoAmI)
	{
		short i = reader.ReadInt16();
		short j = reader.ReadInt16();

		if (Main.netMode == NetmodeID.MultiplayerClient)
			Magmastone.ToggleWireGlowPoint(i, j);
	}

	public override void OnSend(ModPacket modPacket) => modPacket.WritePoint16(_point);
}