using SpiritReforged.Common.Multiplayer;
using System.IO;
using Terraria.DataStructures;
using Terraria.ModLoader.IO;

namespace SpiritReforged.Common.WorldGeneration.Microbiomes;

public sealed class MicrobiomeSystem : ModSystem
{
	/// <summary> Data structure for individually-generated biome instances that are automatically loaded and synced in multiplayer.<br/>
	/// Intended for use with Micropasses. <para/>
	/// Contains <see cref="Position"/> by default and can be placed using method <code>Create(Point16 | Point)</code> </summary>
	public abstract class Microbiome : ILoadable
	{
		public virtual string Name => GetType().Name;
		public Point16 Position { get; private set; }

		/// <summary> Creates a microbiome of the provided type at <paramref name="point"/>. </summary>
		/// <param name="point"> The origin position of the microbiome. </param>
		/// <returns> The newly created microbiome instance. </returns>
		public static T Create<T>(Point16 point) where T : Microbiome
		{
			var instance = (T)_biomeByName[typeof(T).Name].Clone();
			instance.Position = point;

			Microbiomes.Add(instance);
			return instance;
		}

		/// <inheritdoc cref="Create{T}(Point16)"/>
		public static T Create<T>(Point point) where T : Microbiome => Create<T>(new Point16(point.X, point.Y));

		/// <summary> Can be used to save custom data related to this microbiome <b>instance</b>. <para/>
		/// <see cref="Position"/> is saved by default. If that's not necessary, override this method. </summary>
		public virtual void WorldSave(TagCompound tag)
		{
			if (Position != Point16.Zero)
				tag[nameof(Position)] = Position; //Don't write the zeroed value because that's a waste
		}

		public virtual void WorldLoad(TagCompound tag) => Position = tag.Get<Point16>(nameof(Position));

		public virtual void NetSend(BinaryWriter writer) => writer.WritePoint16(Position);
		public virtual void NetReceive(BinaryReader reader) => Position = reader.ReadPoint16();

		public void Load(Mod mod)
		{
			AddDefinition(this);
			Load();
		}

		/// <summary> Called once per type when <see cref="_biomeByName"/> is populated. </summary>
		public virtual void Load() { }
		public virtual void Unload() { }

		/// <summary> Simply casts the result of <see cref="object.MemberwiseClone"/>. </summary>
		public Microbiome Clone() => (Microbiome)MemberwiseClone();
	}

	/// <summary> Invoked directly after <see cref="Microbiomes"/> is populated, which can happen during world load or after microbiomes are synced in multiplayer. </summary>
	public static event Action PopulateMicrobiomes;

	/// <summary> Default microbiome definitions by name, added during load. Instances should not be used directly but instead cloned using <see cref="Microbiome.Clone"/>. </summary>
	private static readonly Dictionary<string, Microbiome> _biomeByName = [];
	/// <summary> All Microbiome instances that currently exist in the world. </summary>
	public static readonly List<Microbiome> Microbiomes = [];

	/// <summary> Registers this biome instance as a template. </summary>
	public static void AddDefinition(Microbiome biome) => _biomeByName.Add(biome.Name, biome);

	/*/// <summary> Gets a cloned instance from <see cref="BiomeByName"/>. <para/>
	/// Prefer <see cref="Microbiome.Create"/> as it automatically registers an instance to <see cref="Microbiomes"/>. </summary>
	public static T GetInstance<T>() where T : Microbiome => (T)BiomeByName[typeof(T).Name].Clone();*/

	public override void ClearWorld() => Microbiomes.Clear();

	public override void NetSend(BinaryWriter writer)
	{
		writer.Write((ushort)Microbiomes.Count);

		foreach (var b in Microbiomes)
		{
			writer.Write(b.Name);
			b.NetSend(writer);
		}
	}

	public override void NetReceive(BinaryReader reader)
	{
		Microbiomes.Clear();
		ushort count = reader.ReadUInt16();

		for (int i = 0; i < count; i++)
		{
			string name = reader.ReadString();

			Microbiome biome = _biomeByName[name].Clone();
			biome.NetReceive(reader);

			Microbiomes.Add(biome); //Repopulate Microbiomes based on data provided by the server
		}

		PopulateMicrobiomes?.Invoke();
	}

	public override void SaveWorldData(TagCompound tag)
	{
		List<TagCompound> list = [];
		TagCompound data = [];

		foreach (var b in Microbiomes)
		{
			b.WorldSave(data);

			if (data.Count != 0)
			{
				list.Add(new()
				{
					["name"] = b.Name,
					["data"] = data
				});

				data = [];
			}
		}

		if (list.Count != 0)
			tag["microbiomes"] = list;
	}

	public override void LoadWorldData(TagCompound tag)
	{
		var list = tag.GetList<TagCompound>("microbiomes");

		foreach (var item in list)
		{
			string name = item.GetString("name");
			TagCompound data = item.GetCompound("data");

			if (_biomeByName.TryGetValue(name, out Microbiome b))
			{
				Microbiome inst = b.Clone();
				inst.WorldLoad(data);
				Microbiomes.Add(inst);
			}
			else
			{
				SpiritReforgedMod.Instance.Logger.Info($"Microbiome '{name}' was not present in the dictionary.");
			}
		}

		PopulateMicrobiomes?.Invoke();
	}
}