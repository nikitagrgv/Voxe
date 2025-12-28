using OpenTK.Mathematics;

namespace Voxe;

public static class VoxelUtils
{
	public static Vector3i ToBlockPosition(Vector3 position)
	{
		// Dont care about y < 0 flooring. But consider case eg -0.1 mustn't be rounded to 0
		int x = (int)float.Floor(position.X);
		int y = (int)(position.Y + 1) - 1;
		int z = (int)float.Floor(position.Z);
		return new Vector3i(x, y, z);
	}

	public static ChunkIndex GetChunkIndex(float x, float z)
	{
		int blockX = (int)float.Floor(x);
		int blockZ = (int)float.Floor(z);
		int chunkX = FloorToWidth(blockX);
		int chunkZ = FloorToWidth(blockZ);
		return new ChunkIndex(chunkX, chunkZ);
	}

	public static ChunkIndex GetChunkIndex(int x, int z)
	{
		int chunkX = FloorToWidth(x);
		int chunkZ = FloorToWidth(z);
		return new ChunkIndex(chunkX, chunkZ);
	}

	private static int FloorToWidth(int value)
	{
		int res = value / Chunk.ChunkWidth;
		res -= (value < 0 ? 1 : 0) & (value % Chunk.ChunkWidth != 0 ? 1 : 0);
		return res;
	}
}