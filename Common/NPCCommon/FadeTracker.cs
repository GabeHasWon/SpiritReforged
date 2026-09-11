namespace SpiritReforged.Common.NPCCommon;

internal class FadeTracker(int length)
{
	public struct FadeInstance
	{
		public Vector2 Position;
		public Vector2 Center;
		public Rectangle Frame;
		public Color DrawColor;
		public float Rotation;
		public float Scale;
		public SpriteEffects Effects;
	}
	
	public enum TrailDrawMode : byte
	{
		Direct,
		Fade,
		FadeExponential
	}

	public List<FadeInstance> Instances = new(length);

	public void Update(NPC npc, Color color, bool addEmpty, bool addNone)
	{
		if (addNone)
		{
			Instances.Insert(0, addEmpty ? new FadeInstance() { Scale = 0 } : new FadeInstance()
			{
				Position = npc.position,
				Center = npc.Center,
				DrawColor = color,
				Frame = npc.frame,
				Rotation = npc.rotation,
				Scale = npc.scale,
				Effects = npc.spriteDirection == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None
			});
		}

		if (Instances.Count >= length)
			Instances.RemoveAt(length - 1);
	}

	public void Draw(Texture2D texture, bool useCenter, Vector2 screenPos, Vector2 origin, TrailDrawMode mode, SpriteEffects flipEffects = SpriteEffects.None)
	{
		if (Instances.Count == 0)
			return;

		for (int i = Instances.Count - 1; i >= 0; --i)
		{
			FadeInstance instance = Instances[i];

			if (instance.Scale == 0f)
				continue;

			Vector2 position = useCenter ? instance.Center : instance.Position;
			Color color = Lighting.GetColor(position.ToTileCoordinates(), instance.DrawColor);
			instance.Effects ^= flipEffects;

			if (mode == TrailDrawMode.Fade)
				color *= 1 - i / (float)Instances.Count;
			else if (mode == TrailDrawMode.FadeExponential)
				color *= MathF.Pow(1 - i / (float)Instances.Count, 2);

			Main.EntitySpriteDraw(texture, position - screenPos, instance.Frame, color, instance.Rotation, origin, instance.Scale, instance.Effects);
		}
	}
}
