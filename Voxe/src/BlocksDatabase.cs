using System.Text.Json;
using System.Text.Json.Serialization;

namespace Voxe;

public class BlocksDatabase
{
	public const int BlockTextureWidth = 16;

	public readonly struct Result
	{
		public Block[] Blocks { get; init; }
		public Image[] Images { get; init; }
	}

	public readonly struct Block
	{
		public ushort Id { get; init; }
		public string Name { get; init; }
		public bool IsTransparent { get; init; }
		public bool IsInvisible { get; init; }

		public int? TextureIndexPX { get; init; }
		public int? TextureIndexNX { get; init; }

		public int? TextureIndexPY { get; init; }
		public int? TextureIndexNY { get; init; }

		public int? TextureIndexPZ { get; init; }
		public int? TextureIndexNZ { get; init; }
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

	public Result Load(string databaseRelPath)
	{
		using Stream stream = FileSystem.ReadFileStream(databaseRelPath);
		ParsedRoot root = JsonSerializer.Deserialize<ParsedRoot>(stream);

		Dictionary<string, int> imageToIndex = new();
		List<Image> images = [];

		foreach (ParsedBlock block in root.Blocks)
		{
			TryAddImage(block.TexturePath);

			TryAddImage(block.TexturePathPX);
			TryAddImage(block.TexturePathNX);

			TryAddImage(block.TexturePathPY);
			TryAddImage(block.TexturePathNY);

			TryAddImage(block.TexturePathPZ);
			TryAddImage(block.TexturePathNZ);
		}

		Block[] blocks = new Block[root.Blocks.Length];
		for (int i = 0; i < root.Blocks.Length; i++)
		{
			ParsedBlock parsedBlock = root.Blocks[i];

			int? imageMain = parsedBlock.TexturePath != null ? imageToIndex[parsedBlock.TexturePath] : null;

			int? imagePX = TryGetImage(parsedBlock.TexturePathPX, imageMain);
			int? imageNX = TryGetImage(parsedBlock.TexturePathNX, imageMain);

			int? imagePY = TryGetImage(parsedBlock.TexturePathPY, imageMain);
			int? imageNY = TryGetImage(parsedBlock.TexturePathNY, imageMain);

			int? imagePZ = TryGetImage(parsedBlock.TexturePathPZ, imageMain);
			int? imageNZ = TryGetImage(parsedBlock.TexturePathNZ, imageMain);

			Block block = new()
			{
				Id = parsedBlock.Id,
				Name = parsedBlock.Name,

				IsTransparent = parsedBlock.IsTransparent ?? false,
				IsInvisible = parsedBlock.IsInvisible ?? false,

				TextureIndexPX = imagePX,
				TextureIndexNX = imageNX,

				TextureIndexPY = imagePY,
				TextureIndexNY = imageNY,

				TextureIndexPZ = imagePZ,
				TextureIndexNZ = imageNZ,
			};

			blocks[i] = block;
		}

		return new Result()
		{
			Blocks = blocks,
			Images = images.ToArray(),
		};

		////////////////////////////////
		void TryAddImage(string? path)
		{
			if (string.IsNullOrEmpty(path))
				return;

			if (imageToIndex.ContainsKey(path))
				return;

			Image image = new(path, Image.ImageFormat.Rgba, flipY: true);

			if (image.Width != BlockTextureWidth || image.Height != BlockTextureWidth)
				throw new Exception($"Invalid block texture size: {image.Width}x{image.Height}");

			int imageIndex = images.Count;
			images.Add(image);
			imageToIndex.Add(path, imageIndex);
		}

		int? TryGetImage(string? path, int? defaultImage)
		{
			switch (path)
			{
				case null:
					return defaultImage;
				case "":
					return null;
				default:
					imageToIndex.TryGetValue(path, out int image);
					return image;
			}
		}
	}
}