using System.Text.Json;
using System.Text.Json.Serialization;

namespace Voxe;

public class BlocksDatabase
{
	public readonly struct Block
	{
		public ushort Id { get; init; }
		public string Name { get; init; }
		public bool IsTransparent { get; init; }
		public bool IsInvisible { get; init; }

		public Image? ImagePX { get; init; }
		public Image? ImageNX { get; init; }

		public Image? ImagePY { get; init; }
		public Image? ImageNY { get; init; }

		public Image? ImagePZ { get; init; }
		public Image? ImageNZ { get; init; }
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