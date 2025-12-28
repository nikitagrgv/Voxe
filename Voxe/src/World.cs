using System.Diagnostics;

namespace Voxe;

public class World
{
	private Dictionary<ChunkIndex, Chunk> _chunksMap = new();

	public void InitChunk(ChunkIndex index, Chunk chunk)
	{
		Debug.Assert(TryGetChunk(index) == null);
		_chunksMap.Add(index, chunk);
	}

	public Chunk? TryGetChunk(ChunkIndex index)
	{
		_chunksMap.TryGetValue(index, out Chunk? chunk);
		return chunk;
	}
}