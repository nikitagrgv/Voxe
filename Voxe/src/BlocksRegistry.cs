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
	private static int _atlasWidth;

	public static void Initialize(int atlasWidth)
	{
		Debug.Assert(_blocks.Count == 0, "Already initialized");

		BlocksDatabase database = new();
		for (int i = 0; i < 19; ++i)
		{
		   Console.WriteLine($"{i} - {Voxe.Math.Utils.RoundUpToPowerOfTwo(i)}");
		}
		// TODO# inject from params
		BlocksDatabase.Result databaseBlocks = database.Load("blocks.json");

		Image image = new(128, 128, Image.ImageFormat.Rgba, Color.FromArgb(255, 255, 0, 255));
		image.CopyFrom(databaseBlocks.Images[2], 4, 4, 12, 55, 16-4, 16-4);
		image.Save("spam/gen.png");

		AddBasicBlock(BasicBlock.Air, new UvIndexSet(), isInvisible: true);
		AddBasicBlock(BasicBlock.Grass,
			new UvIndexSet(positiveX: 1, negativeX: 1, positiveY: 0, negativeY: 2, positiveZ: 1, negativeZ: 1),
			isInvisible: false);
		AddBasicBlock(BasicBlock.Dirt,
			new UvIndexSet(positiveX: 2, negativeX: 2, positiveY: 2, negativeY: 2, positiveZ: 2, negativeZ: 2),
			isInvisible: false);
		AddBasicBlock(BasicBlock.Stone,
			new UvIndexSet(positiveX: 3, negativeX: 3, positiveY: 3, negativeY: 3, positiveZ: 3, negativeZ: 3),
			isInvisible: false);
		AddBasicBlock(BasicBlock.Snow,
			new UvIndexSet(positiveX: 4, negativeX: 4, positiveY: 4, negativeY: 4, positiveZ: 4, negativeZ: 4),
			isInvisible: false);

		_numBasicBlocks = _blocks.Count;

		Debug.Assert(Enum.GetValues<BasicBlock>().Cast<ushort>().Max() == _numBasicBlocks - 1,
			"All must be registered");

		SetAtlasWidth(atlasWidth);
	}

	public static void SetAtlasWidth(int width)
	{
		Debug.Assert(width > 0);
		_atlasWidth = width;
		RecalculateUv();
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
		Debug.Assert(_blocks.Count <= _atlasWidth * _atlasWidth);
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
		double blockSize = 1 / (double)_atlasWidth;

		int col = index % _atlasWidth;
		int row = index / _atlasWidth;
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
}