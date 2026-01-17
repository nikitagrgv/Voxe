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
		[JsonPropertyName("id")] public ushort Id { get; init; }
		[JsonPropertyName("name")] public string Name { get; init; }

		[JsonPropertyName("transparent")] public bool? IsTransparent { get; init; }
		[JsonPropertyName("invisible")] public bool? IsInvisible { get; init; }

		[JsonPropertyName("texture")] public string? TexturePath { get; init; }

		[JsonPropertyName("texture-px")] public string? TexturePathPX { get; init; }
		[JsonPropertyName("texture-nx")] public string? TexturePathNX { get; init; }

		[JsonPropertyName("texture-py")] public string? TexturePathPY { get; init; }
		[JsonPropertyName("texture-ny")] public string? TexturePathNY { get; init; }

		[JsonPropertyName("texture-pz")] public string? TexturePathPZ { get; init; }
		[JsonPropertyName("texture-nz")] public string? TexturePathNZ { get; init; }
	}

	private readonly struct ParsedRoot
	{
		[JsonPropertyName("blocks")] public ParsedBlock[] Blocks { get; init; }
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