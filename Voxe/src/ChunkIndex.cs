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

	public ChunkIndex ShiftedX(int dx)
	{
		return new ChunkIndex(X + dx, Z);
	}

	public ChunkIndex ShiftedZ(int dz)
	{
		return new ChunkIndex(X, Z + dz);
	}

	public ChunkIndex Shifted(int dx, int dz)
	{
		return new ChunkIndex(X + dx, Z + dz);
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