namespace Voxe;

public struct ChunkIndex(
	int x,
	int z) : IEquatable<ChunkIndex>
{
	public int X { get; set; } = x;
	public int Z { get; set; } = z;

	public bool Equals(ChunkIndex other)
	{
		return X == other.X && Z == other.Z;
	}

	public override bool Equals(object? obj)
	{
		return obj is ChunkIndex other && Equals(other);
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(X, Z);
	}

	public static bool operator ==(ChunkIndex left, ChunkIndex right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(ChunkIndex left, ChunkIndex right)
	{
		return !(left == right);
	}
}