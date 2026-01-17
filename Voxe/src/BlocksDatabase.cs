using System.Text.Json;
using System.Text.Json.Serialization;

namespace Voxe;

public class BlocksDatabase
{
	public readonly struct Block
	{
		public ushort Id { get; }
		public string Name { get; }
		public bool IsTransparent { get; }
		public bool IsInvisible { get; }

		public Image? ImagePX { get; }
		public Image? ImageNX { get; }

		public Image? ImagePY { get; }
		public Image? ImageNY { get; }

		public Image? ImagePZ { get; }
		public Image? ImageNZ { get; }
	}

	private readonly struct ParsedBlock
	{
		[JsonPropertyName("id")] public ushort Id { get; }
		[JsonPropertyName("name")] public string Name { get; }

		[JsonPropertyName("transparent")] public bool? IsTransparent { get; }
		[JsonPropertyName("invisible")] public bool? IsInvisible { get; }

		[JsonPropertyName("texture")] public string? TexturePath { get; }

		[JsonPropertyName("texture-px")] public string? TexturePathPX { get; }
		[JsonPropertyName("texture-nx")] public string? TexturePathNX { get; }

		[JsonPropertyName("texture-py")] public string? TexturePathPY { get; }
		[JsonPropertyName("texture-ny")] public string? TexturePathNY { get; }

		[JsonPropertyName("texture-pz")] public string? TexturePathPZ { get; }
		[JsonPropertyName("texture-nz")] public string? TexturePathNZ { get; }
	}

	private readonly struct ParsedRoot
	{
		[JsonPropertyName("blocks")] public ParsedBlock[] Blocks { get; }
	}

	public Block[] Parse(string databaseRelPath)
	{
		using Stream stream = FileSystem.ReadFileStream(databaseRelPath);
		ParsedRoot root = JsonSerializer.Deserialize<ParsedRoot>(stream);

		foreach (ParsedBlock block in root.Blocks)
		{
			
		}

		return [];
	}
}