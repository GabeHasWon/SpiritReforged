namespace SpiritReforged.Common.ItemCommon;

internal class ItemEvents : GlobalItem
{
	public delegate void DefaultsDelegate(Item item);
	internal static readonly Dictionary<int, DefaultsDelegate> DefaultByType = [];

	/// <summary> Binds <paramref name="dele"/> to the provided <paramref name="itemType"/> and invokes it whenever <see cref="GlobalType{TEntity, TGlobal}.SetDefaults(TEntity)"/> is called. </summary>
	public static void CreateItemDefaults(int itemType, DefaultsDelegate dele) => DefaultByType.Add(itemType, dele);
	/// <inheritdoc cref="CreateItemDefaults(int, DefaultsDelegate)"/>
	public static void CreateItemDefaults(DefaultsDelegate dele, params int[] itemTypes)
	{
		foreach (int type in itemTypes)
			CreateItemDefaults(type, dele);
	}

	public override void SetDefaults(Item entity)
	{
		if (DefaultByType.TryGetValue(entity.type, out var dele))
		{
			dele.Invoke(entity);
		}
	}
}