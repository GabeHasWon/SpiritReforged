using SpiritReforged.Common.Multiplayer;
using System.IO;
using Terraria.DataStructures;
using Terraria.ModLoader.IO;

namespace SpiritReforged.Common.WorldGeneration;

/// <summary> Can be used to contain data for individually-generated biome instances. </summary>
public abstract class Microbiome : ILoadable
{
	public virtual string Name => GetType().Name;
	public Point16 Position { get; private set; }

	/// <summary> Creates a microbiome of the provided type and places it according to <paramref name="point"/> and <paramref name="place"/>. </summary>
	/// <param name="point"> The origin position of the microbiome. </param>
	/// <param name="place"> Whether this instance should be generated at <paramref name="point"/>. </param>
	/// <returns> The newly created instance. </returns>
	public static T Create<T>(Point16 point, bool place = true) where T : Microbiome
	{
		var inst = MicrobiomeSystem.GetInstance<T>();
		inst.Position = point;

		if (place)
			inst.OnPlace(point);

		MicrobiomeSystem.Microbiomes.Add(inst);
		return inst;
	}

	protected abstract void OnPlace(Point16 point);

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
		MicrobiomeSystem.AddDefinition(this);
		Load();
	}

	/// <summary> Called once per type when <see cref="MicrobiomeSystem.BiomeByName"/> is populated. </summary>
	public virtual void Load() { }
	public virtual void Unload() { }

	/// <summary> Simply casts the result of <see cref="object.MemberwiseClone"/>. </summary>
	public Microbiome Clone() => (Microbiome)MemberwiseClone();
}

public class MicrobiomeSystem : ModSystem
{
	public static event Action PostLoadMicrobiomes;

	/// <summary> Default microbiome definitions by name, added during load. Instances should not be used directly but instead cloned using <see cref="Microbiome.Clone"/>. </summary>
	private static readonly Dictionary<string, Microbiome> BiomeByName = [];
	internal static readonly List<Microbiome> Microbiomes = [];

	public static void AddDefinition(Microbiome biome) => BiomeByName.Add(biome.Name, biome);
	/// <summary> Gets a cloned instance from <see cref="BiomeByName"/>. <para/>
	/// Prefer <see cref="Microbiome.Create{T}(Point16)"/> as it automatically registers an instance to <see cref="Microbiomes"/>. </summary>
	public static T GetInstance<T>() where T : Microbiome => (T)BiomeByName[typeof(T).Name].Clone();

	public override void ClearWorld()
	{
		Microbiomes.Clear();
	}

	public override void NetSend(BinaryWriter writer)
	{
		foreach (var b in Microbiomes)
			b.NetSend(writer);
	}

	public override void NetReceive(BinaryReader reader)
	{
		foreach (var b in Microbiomes)
			b.NetReceive(reader);
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
		{
			tag["microbiomes"] = list;
		}
	}

	public override void LoadWorldData(TagCompound tag)
	{
		var list = tag.GetList<TagCompound>("microbiomes");

		foreach (var item in list)
		{
			string name = item.GetString("name");
			TagCompound data = item.GetCompound("data");

			if (BiomeByName.TryGetValue(name, out Microbiome b))
			{
				var inst = b.Clone();
				inst.WorldLoad(data);
				Microbiomes.Add(inst);
			}
			else
			{
				SpiritReforgedMod.Instance.Logger.Info($"Microbiome '{name}' was not present in the dictionary.");
			}
		}

		PostLoadMicrobiomes?.Invoke();
	}
}