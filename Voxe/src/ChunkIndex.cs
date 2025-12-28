namespace Voxe;

public struct ChunkIndex(
	int x,
	int z)
{
	public int X { get; set; } = x;
	public int Z { get; set; } = z;
}