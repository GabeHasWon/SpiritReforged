using SpiritReforged.Common.WorldGeneration;
using Terraria.ModLoader.IO;

namespace SpiritReforged.Content.Forest.ButterflyStaff;

[Obsolete("Regions are handled by EtherealGrove instead")] //Keep temporarily for legacy data loading
internal class ButterflySystem : ModSystem
{
	//public static HashSet<Rectangle> ButterflyZones = [];

	//public override void ClearWorld() => ButterflyZones.Clear();

	public override void SaveWorldData(TagCompound tag)
	{
		//if (ButterflyZones.Count != 0)
		//	tag[nameof(ButterflyZones)] = ButterflyZones.ToList();
	}

	public override void LoadWorldData(TagCompound tag)
	{
		var list = tag.GetList<Rectangle>("ButterflyZones");
		/*ButterflyZones = [.. list];

		if (Main.netMode != NetmodeID.MultiplayerClient)
		{
			foreach (var rect in ButterflyZones)
				Populate(rect);
		}*/

		foreach (var rect in list) //When legacy data is loaded, register a microbiome using the shiny new system instead
			Microbiome.Create<ButterflyShrineBiome>(rect.Center().ToPoint16(), false);
	}

	/*/// <summary> Spawns butterfly NPCs within <paramref name="rect"/>. </summary>
	private static void Populate(Rectangle rect)
	{
		const int tries = 20;
		int randomCount = Main.rand.Next(3, 6);

		for (int i = 0; i < randomCount; i++)
		{
			var pos = Vector2.Zero;
			for (int t = 0; t < tries; t++)
			{
				pos = Main.rand.NextVector2FromRectangle(rect).ToWorldCoordinates();
				if (!Collision.SolidCollision(pos, 8, 8))
					break;
			}

			NPC.NewNPCDirect(new EntitySource_SpawnNPC(), pos, ModContent.NPCType<ButterflyCritter>()); //Withheld by PersistentNPCSystem
		}
	}

	public override void NetSend(BinaryWriter writer)
	{
		writer.Write((short)ButterflyZones.Count);

		foreach (var zone in ButterflyZones)
		{
			writer.Write((short)zone.Location.X);
			writer.Write((short)zone.Location.Y);
			writer.Write((short)zone.Width);
			writer.Write((short)zone.Height);
		}
	}

	public override void NetReceive(BinaryReader reader)
	{
		ButterflyZones.Clear();
		short count = reader.ReadInt16();

		for (int i = 0; i < count; ++i)
			ButterflyZones.Add(new(reader.ReadInt16(), reader.ReadInt16(), reader.ReadInt16(), reader.ReadInt16()));
	}*/
}