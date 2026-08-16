using Terraria.DataStructures;

namespace SpiritReforged.Common.ItemCommon;

public class DrawGrid : DrawAnimation
{
	public readonly int Columns;
	public readonly int Rows;

	public DrawGrid(int columns, int rows, int frame = 0)
	{
		Frame = frame;
		FrameCounter = 0;
		TicksPerFrame = 1;
		Rows = rows;
		Columns = columns;
	}

	public override void Update() { }

	public override Rectangle GetFrame(Texture2D texture, int frameCounterOverride = -1)
	{
		int frame = (frameCounterOverride >= 0) ? Math.Clamp(frameCounterOverride, 0, Columns * Rows - 1) : Frame;
		return texture.Frame(Columns, Rows, frame % Columns, frame / Columns, (Columns == 1) ? 0 : -2, (Rows == 1) ? 0 : -2);
	}
}