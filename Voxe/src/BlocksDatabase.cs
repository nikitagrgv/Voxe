namespace Voxe;

public class BlocksDatabase
{
	public struct Block
	{
		public int Id { get; set; }
		public string Name { get; set; }
		public bool IsTransparent { get; set; }
		public bool IsInvisible { get; set; }

		public Image? ImagePX { get; set; }
		public Image? ImageNX { get; set; }

		public Image? ImagePY { get; set; }
		public Image? ImageNY { get; set; }

		public Image? ImagePZ { get; set; }
		public Image? ImageNZ { get; set; }
	}

	public Block[] Parse(string databaseRelPath)
	{
		string path = FileSystem.GetAbsolutePath(databaseRelPath);


		return [];
	}
}