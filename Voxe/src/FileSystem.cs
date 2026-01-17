using System;
using System.IO;

namespace Voxe;

public static class FileSystem
{
	private static string? _dataPath;

	public static void Initialize(string? dataPath)
	{
		if (!string.IsNullOrEmpty(dataPath))
			_dataPath = dataPath;
		else
		{
			string baseDir = AppDomain.CurrentDomain.BaseDirectory;
			baseDir = Path.TrimEndingDirectorySeparator(baseDir);

			DirectoryInfo? parent = Directory.GetParent(baseDir);
			string parentDir = parent?.FullName ?? baseDir;
			_dataPath = Path.Combine(parentDir, "data");
		}

		Console.WriteLine($"FileSystem: data path is '{_dataPath}'");
	}

	public static string GetAbsolutePath(string relativePath)
	{
		if (_dataPath == null)
			throw new InvalidOperationException("FileSystem is not initialized");
		return Path.GetFullPath(relativePath, _dataPath);
	}

	public static string ReadTextFile(string relativePath)
	{
		string absolutePath = GetAbsolutePath(relativePath);
		return File.ReadAllText(absolutePath);
	}

	public static byte[] ReadFile(string relativePath)
	{
		string absolutePath = GetAbsolutePath(relativePath);
		return File.ReadAllBytes(absolutePath);
	}

	public static Stream ReadFileStream(string relativePath)
	{
		string absolutePath = GetAbsolutePath(relativePath);
		FileStream stream = File.OpenRead(absolutePath);
		return stream;
	}
}