using System.Diagnostics;

namespace Voxe;

public class World
{
	private readonly Dictionary<ChunkIndex, Chunk?> _chunksMap = new();

	public int NumLoadedChunks => _chunksMap.Count;
	public IEnumerable<Chunk> AllChunks => _chunksMap.Values;

	public ulong TotalGPUMemory
	{
		get
		{
			ulong total = 0;
			foreach (Chunk chunk in AllChunks)
			{
				if (chunk.Mesh != null)
					total += chunk.Mesh.BuffersMemoryUsage;
			}

			return total;
		}
	}

	public void InitChunk(ChunkIndex index, Chunk chunk)
	{
		Debug.Assert(!HasChunk(index));
		Debug.Assert(chunk.Index == index);
		_chunksMap.Add(index, chunk);
	}

	public Chunk? TryGetChunk(ChunkIndex index)
	{
		_chunksMap.TryGetValue(index, out Chunk? chunk);
		return chunk;
	}

	public bool HasChunk(ChunkIndex index)
	{
		return _chunksMap.ContainsKey(index);
	}

	public void GetChunks(ChunkIndex center, int radius, List<Chunk> chunks)
	{
		// TODO# Implement normally, use chunks flat map, sort
		chunks.Clear();
		int rad2 = radius * radius;
		for (int x = -radius; x < +radius; x++)
		{
			for (int z = -radius; z < +radius; z++)
			{
				if (x * x + z * z > rad2)
					continue;
				ChunkIndex chunkIndex = new(center.X + x, center.Z + z);
				if (!_chunksMap.TryGetValue(chunkIndex, out Chunk? chunk))
					continue;
				chunks.Add(chunk);
			}
		}
	}

	public void GetEmptyChunks(ChunkIndex center, int radius, List<ChunkIndex> chunks)
	{
		chunks.Clear();
		int rad2 = radius * radius;
		for (int x = -radius; x < +radius; x++)
		{
			for (int z = -radius; z < +radius; z++)
			{
				if (x * x + z * z > rad2)
					continue;
				ChunkIndex chunkIndex = new(center.X + x, center.Z + z);
				if (HasChunk(chunkIndex))
					continue;
				chunks.Add(chunkIndex);
			}
		}
	}

	public ChunkNeighbourhood GetNeighbourhood(ChunkIndex chunkIndex)
	{
		_chunksMap.TryGetValue(new ChunkIndex(chunkIndex.X + 1, chunkIndex.Z), out Chunk? px);
		_chunksMap.TryGetValue(new ChunkIndex(chunkIndex.X - 1, chunkIndex.Z), out Chunk? nx);
		_chunksMap.TryGetValue(new ChunkIndex(chunkIndex.X, chunkIndex.Z + 1), out Chunk? pz);
		_chunksMap.TryGetValue(new ChunkIndex(chunkIndex.X, chunkIndex.Z - 1), out Chunk? nz);

		Debug.Assert(px != null || nx != null || pz != null || nz != null);
		return new ChunkNeighbourhood(px!, nx!, pz!, nz!);
	}
}