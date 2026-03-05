using System.IO;
using System.Linq;
using Terraria.GameContent.Drawing;
using Terraria.ModLoader.IO;

namespace SpiritReforged.Common.SimpleEntity;

public class SimpleEntitySystem : ModSystem
{
	internal static Dictionary<Type, int> Types = [];
	internal static Asset<Texture2D>[] Textures;

	/// <summary> Instances that were created on load. </summary>
	private static SimpleEntity[] Templates;

	/// <summary> Entities that exist in the world. </summary>
	internal static SimpleEntity[] Entities;

	internal const int MaxEntities = 200;
	private static int NextIndex;

	/// <summary> Summons a new entity at the given position and syncs it if <paramref name="quiet"/> is false. </summary>
	public static SimpleEntity NewEntity(int type, Vector2 position, bool quiet = false)
	{
		var entity = Templates[type].Clone();
		entity.active = true;
		entity.whoAmI = NextIndex;
		entity.Center = position;

		Entities[NextIndex] = entity;

		for (int i = NextIndex; i < Entities.Length; i++)
		{
			if (Entities[i] is null)
			{
				NextIndex = i;
				break;
			}
		} //Move up the array to the next available slot

		if (Main.netMode != NetmodeID.SinglePlayer && !quiet)
			new SpawnSimpleEntityData((short)type, position).Send();

		return entity;
	}

	/// <inheritdoc cref="NewEntity(int, Vector2, bool)"/>
	public static SimpleEntity NewEntity(Type type, Vector2 position, bool quiet = false)
	{
		int t = Types[type];
		return NewEntity(t, position, quiet);
	}

	/// <inheritdoc cref="NewEntity(int, Vector2, bool)"/>
	public static SimpleEntity NewEntity<T>(Vector2 position, bool quiet = false) where T : SimpleEntity => NewEntity(typeof(T), position, quiet);

	public static void RemoveEntity(int whoAmI)
	{
		Entities[whoAmI] = null;
		NextIndex = whoAmI;
	}

	public override void Load()
	{
		Templates = new SimpleEntity[MaxEntities];
		Entities = new SimpleEntity[MaxEntities];
		Textures = new Asset<Texture2D>[MaxEntities];

		foreach (Type type in SpiritReforgedMod.Instance.Code.GetTypes())
		{
			if (type.IsSubclassOf(typeof(SimpleEntity)) && !type.IsAbstract)
			{
				int myType = Types.Count;
				Types[type] = myType;

				var instance = (SimpleEntity)Activator.CreateInstance(type);
				Textures[myType] = ModContent.Request<Texture2D>(instance.TexturePath);
				instance.Load();
				Templates[myType] = instance;
			}
		}

		Array.Resize(ref Textures, Types.Count);
		Array.Resize(ref Templates, Types.Count);

		On_TileDrawing.Update += UpdateEntities;
		On_Main.DoDraw_Tiles_NonSolid += DrawEntities;
	}

	private static void UpdateEntities(On_TileDrawing.orig_Update orig, TileDrawing self)
	{
		orig(self);

		for (int i = 0; i < MaxEntities; i++)
			Entities[i]?.Update();
	}

	private static void DrawEntities(On_Main.orig_DoDraw_Tiles_NonSolid orig, Main self)
	{
		orig(self);

		for (int i = 0; i < MaxEntities; i++)
			Entities[i]?.Draw(Main.spriteBatch);
	}

	public override void ClearWorld()
	{
		for (int i = 0; i < MaxEntities; i++)
			Entities[i] = null; //Unload all of our entities with the world

		NextIndex = 0;
	}

	private const string commonKey = "simpleEntities";
	public override void SaveWorldData(TagCompound tag)
	{
		static List<TagCompound> SaveData()
		{
			var list = new List<TagCompound>();

			for (int i = 0; i < MaxEntities; i++)
			{
				var entity = Entities[i];
				if (entity == null || !entity.saveMe)
					continue;

				TagCompound tag = [];

				tag["x"] = (int)entity.Center.X;
				tag["y"] = (int)entity.Center.Y;
				tag["name"] = entity.GetType().Name;

				list.Add(tag);
			}

			return list;
		}

		tag[commonKey] = SaveData();
	}

	public override void LoadWorldData(TagCompound tag)
	{
		var list = tag.GetList<TagCompound>(commonKey);
		if (list == default)
			return;

		for (int i = 0; i < list.Count; i++)
		{
			var tagInList = list[i];

			var position = new Vector2(tagInList.GetInt("x"), tagInList.GetInt("y"));
			string name = tagInList.GetString("name");
			NewEntity(Types.FirstOrDefault(x => x.Key.Name == name).Value, position);
		}
	}

	public override void NetSend(BinaryWriter writer)
	{
		var entities = Entities.Where(x => x is not null);
		writer.Write((byte)entities.Count());

		foreach (var entity in entities)
		{
			writer.Write((int)entity.Center.X);
			writer.Write((int)entity.Center.Y);
			writer.Write((byte)entity.Type);
		}
	}

	public override void NetReceive(BinaryReader reader)
	{
		ClearWorld();
		byte count = reader.ReadByte();

		for (int i = 0; i < count; i++)
		{
			int x = reader.ReadInt32();
			int y = reader.ReadInt32();
			byte type = reader.ReadByte();

			NewEntity(type, new Vector2(x, y), true);
		}
	}
}