namespace Voxe;

public static class Stat
{
	public static ulong RenderedIndicesTotal { get; set; }
	public static ulong RenderedIndicesPerFrame { get; set; }

	public static void EndFrame()
	{
		RenderedIndicesPerFrame = 0;
	}

	public static void AddRenderedIndices(ulong renderedIndices)
	{
		RenderedIndicesTotal += renderedIndices;
		RenderedIndicesPerFrame += renderedIndices;
	}
}