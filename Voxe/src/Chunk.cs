using System.Diagnostics;

namespace Voxe;

// Blocks order in memory X->Z->Y
public class Chunk
{
	public const int ChunkWidth = 16;
	public const int ChunkHeight = 512;

	public const int ChunkWidth2 = ChunkWidth * ChunkWidth;
	public const int NumBlocks = ChunkWidth2 * ChunkHeight;

	private readonly Block[] _blocks;

	public ChunkMesh? Mesh { get; }

	public Chunk()
	{
		_blocks = new Block[NumBlocks];
		for (int i = 0; i < _blocks.Length; i++)
		{
			_blocks[i] = new Block();
		}
	}

	public void SetBlock(int x, int y, int z, Block block)
	{
		int index = GetBlockIndex(x, y, z);
		SetBlock(index, block);
	}

	public void SetBlock(int index, Block block)
	{
		Debug.Assert(index is >= 0 and < NumBlocks);
		_blocks[index] = block;
	}

	public Block GetBlock(int x, int y, int z)
	{
		int index = GetBlockIndex(x, y, z);
		return GetBlock(index);
	}

	public Block GetBlock(int index)
	{
		Debug.Assert(index is >= 0 and < NumBlocks);
		return _blocks[index];
	}

	public int GetBlockIndex(int x, int y, int z)
	{
		Debug.Assert(x is >= 0 and < ChunkWidth);
		Debug.Assert(z is >= 0 and < ChunkWidth);
		Debug.Assert(y is >= 0 and < ChunkHeight);
		int index = y * ChunkWidth2 + ChunkWidth * z + x;
		return index;
	}
}