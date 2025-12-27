namespace Voxe;

public struct BlockType
{
	private bool _isInvisible;
	private ushort _id;

	public ushort Id => _id;
	public bool IsInvisible => _isInvisible;

	public BlockType(
		ushort id,
		bool isInvisible)
	{
		_isInvisible = isInvisible;
		_id = id;
	}
}