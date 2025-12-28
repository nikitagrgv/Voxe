namespace Voxe;

public class World
{
	private Dictionary<ChunkIndex, Chunk> _chunksMap = new();

	public Chunk? TryGetChunk(ChunkIndex chunkIndex)
	{
		_chunksMap.TryGetValue(chunkIndex, out Chunk? chunk);
		return chunk;
	}
}