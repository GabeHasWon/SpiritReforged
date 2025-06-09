using System.Reflection;

namespace SpiritReforged.Common.WorldGeneration;

/// <summary>Resets this static field to its original value when <see cref="WorldGen.clearWorld()"/> is called.<br/>
/// For <see cref="IEnumerable"/>s, this will call any Clear method (such as <see cref="HashSet{T}.Clear"/>) instead of nulling the value.</summary>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
internal class WorldBoundAttribute : Attribute
{
	/// <summary> When true, prevents alternative reset behaviour from taking place. </summary>
	public bool Manual;
}

internal class WorldBoundSystem : ModSystem
{
	private readonly record struct FieldData(object Obj, MethodInfo Alt = null)
	{
		public readonly object Default = Obj;
		public readonly MethodInfo Alternative = Alt;
	}
	private static readonly Dictionary<FieldInfo, FieldData> Defaults = [];

	public override void Load()
	{
		foreach (var type in Mod.Code.GetTypes())
		{
			foreach (var field in type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
			{
				if (field.GetCustomAttribute<WorldBoundAttribute>() is WorldBoundAttribute attr)
					Defaults.Add(field, new(field.GetValue(null), attr.Manual ? null : GetOptionalInfo(field)));
			}
		}

		static MethodInfo GetOptionalInfo(FieldInfo info) => info.FieldType.GetMethod("Clear", BindingFlags.Public | BindingFlags.Instance);
	}

	public override void ClearWorld()
	{
		foreach (var info in Defaults.Keys)
		{
			var data = Defaults[info];

			if (data.Alternative is null)
				info.SetValue(null, data.Default);
			else
				data.Alternative.Invoke(info.GetValue(data), null);
		}
	}
}
