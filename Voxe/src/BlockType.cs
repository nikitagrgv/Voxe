using System.Diagnostics;

namespace Voxe;

public struct BlockType
{
	private bool _isInvisible;
	private bool _isTransparent;
	private ushort _id;

	public ushort Id => _id;
	public bool IsInvisible => _isInvisible;
	public bool IsTransparent => _isTransparent;

	public BlockType(
		ushort id,
		bool isInvisible,
		bool isTransparent)
	{
		Debug.Assert(!isInvisible || isTransparent);
		_isInvisible = isInvisible;
		_isTransparent = isTransparent;
		_id = id;
	}
}