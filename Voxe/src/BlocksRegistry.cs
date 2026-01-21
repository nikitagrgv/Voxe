using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Transactions;
using Voxe.Math;

namespace Voxe;

public readonly struct UvSet
{
	public UvSet()
	{
	}

	public UvSet(
		Rect positiveX,
		Rect negativeX,
		Rect positiveY,
		Rect negativeY,
		Rect positiveZ,
		Rect negativeZ)
	{
		PositiveX = positiveX;
		NegativeX = negativeX;
		PositiveY = positiveY;
		NegativeY = negativeY;
		PositiveZ = positiveZ;
		NegativeZ = negativeZ;
	}

	public Rect PositiveX { get; }
	public Rect NegativeX { get; }
	public Rect PositiveY { get; }
	public Rect NegativeY { get; }
	public Rect PositiveZ { get; }
	public Rect NegativeZ { get; }
}

public readonly struct UvIndexSet(
	int positiveX,
	int negativeX,
	int positiveY,
	int negativeY,
	int positiveZ,
	int negativeZ)
{
	public UvIndexSet() : this(-1, -1, -1, -1, -1, -1)
	{
	}

	public int PositiveX { get; } = positiveX;
	public int NegativeX { get; } = negativeX;
	public int PositiveY { get; } = positiveY;
	public int NegativeY { get; } = negativeY;
	public int PositiveZ { get; } = positiveZ;
	public int NegativeZ { get; } = negativeZ;
}

public static class BlocksRegistry
{
	private readonly static List<BlockType> _blocks = new();
	private readonly static List<UvIndexSet> _uvIndexSets = new();
	private readonly static List<UvSet> _uvSets = new();

	private static int _numBasicBlocks;
	private static int _numImagesBySide;

	private static Image[] _atlasImages = [];

	public static void Initialize(string blocksDatabasePath)
	{
		Debug.Assert(_blocks.Count == 0, "Already initialized");

		BlocksDatabase database = new();

		BlocksDatabase.Result databaseBlocks = database.Load(blocksDatabasePath);
		GenerateMipMaps(databaseBlocks.Images, databaseBlocks.ImageWidth, out _atlasImages, out _numImagesBySide);

		for (int i = 0; i < _atlasImages.Length; i++)
		{
			_atlasImages[i].Save($"gen/Atlas-{i}.png");
		}

		foreach (BlocksDatabase.Block block in databaseBlocks.Blocks.OrderBy(v => v.Id))
		{
			// TODO: Allow skip ids
			Debug.Assert(_blocks.Count == block.Id, "Skipped ID");
			Debug.Assert(_blocks.Count == _uvIndexSets.Count, "Skipped ID");

			BlockType type = new(block.Id, isInvisible: block.IsInvisible);
			UvIndexSet uvset = new(
				positiveX: block.TextureIndexPX.GetValueOrDefault(-1),
				negativeX: block.TextureIndexNX.GetValueOrDefault(-1),
				positiveY: block.TextureIndexPY.GetValueOrDefault(-1),
				negativeY: block.TextureIndexNY.GetValueOrDefault(-1),
				positiveZ: block.TextureIndexPZ.GetValueOrDefault(-1),
				negativeZ: block.TextureIndexNZ.GetValueOrDefault(-1)
			);

			_blocks.Add(type);
			_uvIndexSets.Add(uvset);
		}

		_numBasicBlocks = _blocks.Count;

		Debug.Assert(Enum.GetValues<BasicBlock>().Cast<ushort>().Max() == _numBasicBlocks - 1,
			"All must be registered");

		RecalculateUv();
	}

	private static void GenerateMipMaps(Image[] blockImages, int blockImageWidth, out Image[] atlasImages,
		out int numImagesBySide)
	{
		// TODO: Shitty

		int totalNumBlockImages = blockImages.Length;
		numImagesBySide = GetNumImagesBySide(totalNumBlockImages, blockImageWidth);

		int curMipMapBlockWidth = blockImageWidth;
		List<Image> mipMaps = [];
		Image[] mipMapsBlocks = blockImages.ToArray();
		while (true)
		{
			int atlasWidth = numImagesBySide * curMipMapBlockWidth;
			Image atlas = new(atlasWidth, atlasWidth,
				Image.ImageFormat.Rgba,
				Color.FromArgb(255, 255, 0, 255));
			for (int i = 0; i < mipMapsBlocks.Length; ++i)
			{
				Image blockImage = mipMapsBlocks[i];
				Debug.Assert(blockImage.Width == curMipMapBlockWidth && blockImage.Height == curMipMapBlockWidth);
				int imageX = (i % numImagesBySide) * curMipMapBlockWidth;
				int imageY = (i / numImagesBySide) * curMipMapBlockWidth;
				atlas.CopyFrom(blockImage, 0, 0, imageX, imageY, curMipMapBlockWidth, curMipMapBlockWidth);
			}

			mipMaps.Add(atlas);
			if (curMipMapBlockWidth >= 2)
			{
				curMipMapBlockWidth /= 2;
				for (int i = 0; i < mipMapsBlocks.Length; i++)
				{
					Image cur = mipMapsBlocks[i];
					Image next = cur.GenerateNextMipLevel();
					mipMapsBlocks[i] = next;
				}
			}
			else
			{
				break;
			}
		}

		atlasImages = mipMaps.ToArray();
	}

	public static int NumMipMaps => _atlasImages.Length;

	public static Image GetAtlas(int mipLevel)
	{
		return _atlasImages[mipLevel];
	}

	public static UvSet GetBlockUvSet(int id)
	{
		return _uvSets[id];
	}

	public static BlockType GetBasicBlockType(BasicBlock basicBlock)
	{
		ushort id = (ushort)basicBlock;
		Debug.Assert(id < _numBasicBlocks);
		return _blocks[id];
	}

	public static BlockType GetBlockType(ushort id)
	{
		Debug.Assert(id < _numBasicBlocks);
		return _blocks[id];
	}

	private static void AddBasicBlock(BasicBlock basicBlock, UvIndexSet uvIndexSet, bool isInvisible)
	{
		ushort id = (ushort)basicBlock;
		BlockType type = new(id, isInvisible);

		Debug.Assert(_blocks.Count == id, "Must be in order");
		Debug.Assert(_blocks.Count == _uvIndexSets.Count, "Must be the same");

		_blocks.Add(type);
		_uvIndexSets.Add(uvIndexSet);
	}

	private static void RecalculateUv()
	{
		Debug.Assert(_blocks.Count <= _numImagesBySide * _numImagesBySide);
		Debug.Assert(_blocks.Count == _uvIndexSets.Count);

		_uvSets.Clear();
		_uvSets.EnsureCapacity(_blocks.Count);
		for (int id = 0; id < _blocks.Count; id++)
		{
			BlockType info = _blocks[id];
			UvSet uvSet;
			if (info.IsInvisible)
			{
				uvSet = new UvSet();
			}
			else
			{
				UvIndexSet uvIndexSet = _uvIndexSets[id];
				uvSet = new UvSet(
					positiveX: GetRect(uvIndexSet.PositiveX),
					negativeX: GetRect(uvIndexSet.NegativeX),
					positiveY: GetRect(uvIndexSet.PositiveY),
					negativeY: GetRect(uvIndexSet.NegativeY),
					positiveZ: GetRect(uvIndexSet.PositiveZ),
					negativeZ: GetRect(uvIndexSet.NegativeZ)
				);
			}

			_uvSets.Add(uvSet);
		}

		Debug.Assert(_uvSets.Count == _uvIndexSets.Count);
	}

	private static Rect GetRect(int index)
	{
		double blockSize = 1 / (double)_numImagesBySide;

		int col = index % _numImagesBySide;
		int row = index / _numImagesBySide;
		int colNext = col + 1;
		int rowNext = row + 1;

		double colD = col;
		double rowD = row;
		double colNextD = colNext;
		double rowNextD = rowNext;

		return new Rect(
			minX: (float)(colD * blockSize),
			minY: (float)(rowD * blockSize),
			maxX: (float)(colNextD * blockSize),
			maxY: (float)(rowNextD * blockSize)
		);
	}

	private static int GetNumImagesBySide(int totalNumImages, int imageWidth)
	{
		// TODO: Shitty but ok
		Debug.Assert(Voxe.Math.Utils.RoundUpToPowerOfTwo(imageWidth) == imageWidth);
		int sideBlocksSizePixels = imageWidth;
		while ((sideBlocksSizePixels / imageWidth) * (sideBlocksSizePixels / imageWidth) < totalNumImages)
			sideBlocksSizePixels *= 2;
		int numImagesBySide = sideBlocksSizePixels / imageWidth;
		return numImagesBySide;
	}
}