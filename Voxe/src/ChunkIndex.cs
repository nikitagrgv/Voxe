namespace Voxe;

public record struct ChunkIndex
{
	public int X { get; }
	public int Z { get; }

	public ChunkIndex(int x, int z)
	{
		X = x;
		Z = z;
	}

	public bool Equals(ChunkIndex other)
	{
		return X == other.X && Z == other.Z;
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(X, Z);
	}
}