using System.Diagnostics;

namespace Voxe;

public class World
{
	private readonly Dictionary<ChunkIndex, Chunk> _chunksMap = new();

	public readonly ref struct ChunksList
	{
		public ChunksList(ReadOnlySpan<Chunk> chunks)
		{
			Chunks = chunks;
		}

		public ReadOnlySpan<Chunk> Chunks { get; }
	}

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