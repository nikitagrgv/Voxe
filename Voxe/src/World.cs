using System.Diagnostics;

namespace Voxe;

public class World
{
	private readonly Dictionary<ChunkIndex, Chunk> _chunksMap = new();

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

	public void GetChunks(ChunkIndex index, int radius, List<Chunk> chunks)
	{
		// TODO# Implement normally, use chunks flat map, sort
		chunks.Clear();
		chunks.AddRange(_chunksMap.Values);
	}
}