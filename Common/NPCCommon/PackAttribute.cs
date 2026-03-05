using SpiritReforged.Common.WorldGeneration;
using Terraria.DataStructures;

namespace SpiritReforged.Common.NPCCommon;

/// <summary> Facilitates spawning NPCs in packs. </summary>
[AttributeUsage(AttributeTargets.Class)]
public class SpawnPackAttribute : Attribute
{
	public int MinSize { get; private set; }
	public int MaxSize { get; private set; }

	public SpawnPackAttribute(int size) => (MinSize, MaxSize) = (size, size);
	public SpawnPackAttribute(int minSize, int maxSize) => (MinSize, MaxSize) = (minSize, maxSize);
}

internal class PackGlobalNPC : GlobalNPC
{
	private static SpawnPackAttribute Tag(int type)
	{
		if (NPCLoader.GetNPC(type) is ModNPC mNPC)
			return (SpawnPackAttribute)Attribute.GetCustomAttribute(mNPC.GetType(), typeof(SpawnPackAttribute));

		return null;
	}

	public override void OnSpawn(NPC npc, IEntitySource source)
	{
		const int spawnRange = 20; //Tile distance

		if (Main.netMode == NetmodeID.MultiplayerClient || source is EntitySource_Parent/*|| source is not EntitySource_SpawnNPC*/ || Tag(npc.type) is not SpawnPackAttribute atr)
			return;

		int packSize = Main.rand.Next(atr.MinSize, atr.MaxSize + 1) - 1; //-1 because we already spawned one NPC
		for (int i = 0; i < packSize; i++)
		{
			for (int a = 0; a < 20; a++) //20 intermediate attempts
			{
				var randomPos = npc.Center.ToTileCoordinates() + new Point((int)(spawnRange * Main.rand.NextFloat(-1f, 1f)), 0);
				int originalY = randomPos.Y;
				WorldMethods.FindGround(randomPos.X, ref randomPos.Y);

				//Don't spawn on screen, with an elevation difference greater than 5 tiles, or on invalid tiles
				if (!WorldGen.PlayerLOS(randomPos.X, randomPos.Y) && Math.Abs(originalY - randomPos.Y) <= 5 && NPC.IsValidSpawningGroundTile(randomPos.X, randomPos.Y))
				{
					NPC.NewNPCDirect(new EntitySource_Parent(npc), randomPos.ToWorldCoordinates() - new Vector2(0, npc.height / 2), npc.type);
					break;
				}
			}
		}
	}
}
