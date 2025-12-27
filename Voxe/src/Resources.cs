using System.Reflection;

namespace Voxe;

internal static class Resources
{
	public static Stream GetResourceStream(string filename)
	{
		Stream? stream = Assembly
			.GetExecutingAssembly()
			.GetManifestResourceStream($"Voxe.res.{filename}");
		return stream ?? throw new Exception($"File not found: {filename}");
	}

	public static string ReadToString(string filename)
	{
		using Stream stream = GetResourceStream(filename);
		using StreamReader reader = new(stream);
		return reader.ReadToEnd();
	}
}