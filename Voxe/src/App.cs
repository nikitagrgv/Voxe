using System;
using System.Runtime.InteropServices;
using CommandLine;
using OpenTK.Graphics.OpenGL;

namespace Voxe;

public class App
{
	private class Options
	{
		[Option('d', "data", Required = false, HelpText = "Path to data directory")]
		public string? DataPath { get; set; } = null;
	}

	private readonly Options _options;

	public App(string[] args)
	{
		Parser parser = new(s => s.IgnoreUnknownArguments = true);
		ParserResult<Options>? result = parser.ParseArguments<Options>(args);
		if (result?.Value == null)
		{
			throw new ArgumentException("Invalid arguments");
		}

		_options = result.Value;
	}

	public void Run()
	{
		Console.WriteLine("Init...");

		FileSystem.Initialize(_options.DataPath);

		{
			using MainWindow window = new();

			GL.DebugMessageCallback(
				(
					DebugSource source,
					DebugType type,
					uint id,
					DebugSeverity severity,
					int length,
					IntPtr message,
					IntPtr userParam) =>
				{
					if (severity != DebugSeverity.DebugSeverityNotification)
					{
						Console.WriteLine($"GL: {Marshal.PtrToStringAnsi(message)}");
					}
				}, IntPtr.Zero);
			GL.Enable(EnableCap.DebugOutput);
			GL.Enable(EnableCap.DebugOutputSynchronous);

			window.Run();
		}

		Console.WriteLine("Shutdown complete");
	}
}