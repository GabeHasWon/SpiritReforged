using RubbleAutoloader;
using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.ProjectileCommon.Abstract;
using SpiritReforged.Content.Underground.Pottery;
using SpiritReforged.Content.Underground.Tiles;
using SpiritReforged.Content.Underground.WayfarerSet;
using Terraria.DataStructures;
using static SpiritReforged.Common.TileCommon.StyleDatabase;
using static SpiritReforged.Common.WorldGeneration.WorldMethods;

namespace SpiritReforged.Common.TileCommon.PresetTiles;

/// <summary> Helper for building pot tiles automatically registered in the Potstiary.<br/>
/// Automatically calls <see cref="LootTable.Resolve"/> if this tile implements <see cref="ILootTile"/>. </summary>
public abstract class PotTile : ModTile, IRecordTile, IAutoloadRubble
{
	public IAutoloadRubble.RubbleData Data => default; //Effectively creates no connection with the Rubblemaker item
	public abstract Dictionary<string, int[]> TileStyles { get; }
	public Dictionary<string, int[]> Styles
	{
		get
		{
			if (Autoloader.IsRubble(Type))
				return [];
			else
				return TileStyles;
		}
	}

	/// <inheritdoc cref="ModType.Load"/>
	public virtual void Load(Mod mod) { }
	public sealed override void Load()
	{
		if (Name.Contains("Rubble")) //Autoloader.IsRubble is unusuable before before loading is complete
			return;

		OnPopulateStyleGroups += AutoloadFromGroup;
		Load(Mod);
	}

	public virtual void AddRecord(int type, StyleGroup group) => RecordHandler.Records.Add(new TileRecord(group.name, type, group.styles));
	public virtual void AutoloadFromGroup()
	{
		foreach (var c in Groups[Type])
			Mod.AddContent(new AutoloadedPotItem(Name + "Rubble", c, AddItemRecipes));
	}

	/// <summary> Allows you to override the recipe of a pot item created in <see cref="AutoloadFromGroup"/>. </summary>
	public virtual void AddItemRecipes(ModItem modItem, StyleGroup group)
	{
		LocalizedText dicovered = AutoloadedPotItem.Discovered;
		var function = (modItem as AutoloadedPotItem).RecordedPot;

		modItem.CreateRecipe().AddRecipeGroup("ClayAndMud", 3).AddTile(ModContent.TileType<PotteryWheel>()).AddCondition(dicovered, function).Register();
	}

	/// <summary> <inheritdoc cref="ModBlockType.SetStaticDefaults"/><para/>
	/// Automatically sets common pot data by type. See <see cref="AddObjectData"/> and <see cref="AddMapData"/>
	/// </summary>
	public override void SetStaticDefaults()
	{
		Main.tileSolid[Type] = false;
		Main.tileBlockLight[Type] = false;
		Main.tileCut[Type] = !Autoloader.IsRubble(Type);
		Main.tileFrameImportant[Type] = true;
		Main.tileSpelunker[Type] = true;

		DustType = -1;
		
		if (!Autoloader.IsRubble(Type))
			WayfarerGlobalTile.PotTypes.Add(Type);

		AddObjectData();
		AddMapData();
	}

	/// <summary> Adds map data for the pot. Defaults to vanilla pot map entry and color.
	public virtual void AddMapData() => AddMapEntry(new Color(146, 76, 77), Language.GetText("MapObject.Pot"));

	/// <summary> Adds object data for this pot. By default, assumes <see cref="TileObjectData.Style2x2"/> with <see cref="TileObjectData.StyleWrapLimit"/> of 3. </summary>
	public virtual void AddObjectData()
	{
		const int row = 3;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
		TileObjectData.newTile.Origin = new(0, 1);
		TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop, TileObjectData.newTile.Width, 0);
		TileObjectData.newTile.StyleWrapLimit = row;
		TileObjectData.newTile.RandomStyleRange = row;
		TileObjectData.newTile.StyleHorizontal = true;
		TileObjectData.newTile.UsesCustomCanPlace = true;
		TileObjectData.newTile.DrawYOffset = 2;
		TileObjectData.addTile(Type);
	}

	public override void KillMultiTile(int i, int j, int frameX, int frameY)
	{
		if (Autoloader.IsRubble(Type) || Generating)
			return;

		if (Main.netMode != NetmodeID.MultiplayerClient)
			LootTable.Resolve(i, j, Type, frameX, frameY); //Resolves the loot table, if any

		if (!Main.dedServ)
			DeathEffects(i, j, frameX, frameY);
	}

	/// <summary> Called after <see cref="ModTile.KillMultiTile"/> on singleplayer/multiplayer clients. </summary>
	public virtual void DeathEffects(int i, int j, int frameX, int frameY) { }

	/// <summary> Calculates coin values similarly to how vanilla pots do. </summary>
	internal static float CalculateCoinValue()
	{
		float value = 200 + WorldGen.genRand.Next(-100, 101);
		value *= 1f + Main.rand.Next(-20, 21) * 0.01f;

		if (Main.hardMode)
			value *= 2;

		if (Main.rand.NextBool(4))
			value *= 1f + Main.rand.Next(5, 11) * 0.01f;

		if (Main.rand.NextBool(8))
			value *= 1f + Main.rand.Next(10, 21) * 0.01f;

		if (Main.rand.NextBool(12))
			value *= 1f + Main.rand.Next(20, 41) * 0.01f;

		if (Main.rand.NextBool(16))
			value *= 1f + Main.rand.Next(40, 81) * 0.01f;

		if (Main.rand.NextBool(20))
			value *= 1f + Main.rand.Next(50, 101) * 0.01f;

		if (Main.expertMode)
			value *= 2.5f;

		if (Main.expertMode && Main.rand.NextBool(2))
			value *= 1.25f;

		if (Main.expertMode && Main.rand.NextBool(3))
			value *= 1.5f;

		if (Main.expertMode && Main.rand.NextBool(4))
			value *= 1.75f;

		if (NPC.downedBoss1)
			value *= 1.1f;

		if (NPC.downedBoss2)
			value *= 1.1f;

		if (NPC.downedBoss3)
			value *= 1.1f;

		if (NPC.downedMechBoss1)
			value *= 1.1f;

		if (NPC.downedMechBoss2)
			value *= 1.1f;

		if (NPC.downedMechBoss3)
			value *= 1.1f;

		if (NPC.downedPlantBoss)
			value *= 1.1f;

		if (NPC.downedQueenBee)
			value *= 1.1f;

		if (NPC.downedGolemBoss)
			value *= 1.1f;

		if (NPC.downedPirates)
			value *= 1.1f;

		if (NPC.downedGoblins)
			value *= 1.1f;

		if (NPC.downedFrost)
			value *= 1.1f;

		return value;
	}

	/// <summary>
	/// Forcefully kills this tile instantly if the nearest player is using a fully charged club.<br/>
	/// This is meant to be used in <see cref="ICutAttempt.OnCutAttempt(int, int)"/>.
	/// </summary>
	/// <param name="i">X position of the tile.</param>
	/// <param name="j">Y position of the tile.</param>
	/// <returns>If the club killed this tile.</returns>
	internal static bool ClubQuickKill(int i, int j)
	{
		var p = Main.player[Player.FindClosest(new Vector2(i, j) * 16, 16, 16)];

		if (p.HeldItem.ModItem is ClubItem && p.heldProj != -1 && Main.projectile[p.heldProj].ModProjectile is BaseClubProj club && club.FullCharge)
		{
			WorldGen.KillTile(i, j);
			return true;
		}

		return false;
	}
}