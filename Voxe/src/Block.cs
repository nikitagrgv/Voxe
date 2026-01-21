namespace Voxe;

public struct Block
{
	public ushort TypeId;

	public Block() : this(BasicBlock.Air)
	{
	}

	public Block(ushort typeId)
	{
		TypeId = typeId;
	}

	public Block(BasicBlock basicBlock)
	{
		TypeId = (ushort)basicBlock;
	}

	public override string ToString()
	{
		return $"Id={TypeId}";
	}
}