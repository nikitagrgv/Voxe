namespace Voxe;

public readonly struct ChunkNeighbourhood
{
	public Chunk PositiveX { get; init; }
	public Chunk NegativeX { get; init; }
	public Chunk PositiveZ { get; init; }
	public Chunk NegativeZ { get; init; }

	public ChunkNeighbourhood(Chunk positiveX, Chunk negativeX, Chunk positiveZ, Chunk negativeZ)
	{
		PositiveX = positiveX;
		NegativeX = negativeX;
		PositiveZ = positiveZ;
		NegativeZ = negativeZ;
	}
}