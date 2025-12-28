using System.Diagnostics;
using System.Drawing;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using LibNoise;
using LibNoise.Builder;
using LibNoise.Modifier;
using LibNoise.Primitive;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Voxe.Render;

namespace Voxe;

public class MainWindow : NativeWindow
{
	private const float CameraBaseMoveSpeed = 4f;
	private const float CameraRunMultiplier = 2f;
	private const float CameraRotateSpeed = 0.2f;

	public bool VSync
	{
		get;
		set
		{
			GLFW.SwapInterval(value ? 1 : 0);
			field = value;
		}
	}

	private float _currentCameraMoveSpeedMultiplier = 1f;

	private readonly Stopwatch _timer = new();

	private readonly FpsStat _fpsStat = new();

	// TODO# Just for convenience, remove
	private event Action? RenderFrame;
	private event Action? UpdateFrame;

	private struct Camera
	{
		public float Yaw
		{
			get;
			set => field = value.Normalize360();
		} = 0f;

		public float Pitch
		{
			get;
			set => field = value.Normalize360();
		} = 0f;

		public Vector3 Position { get; set; }

		public Matrix4 GetTransform()
		{
			float yaw = MathHelper.DegreesToRadians(Yaw);
			float pitch = MathHelper.DegreesToRadians(Pitch);
			Matrix4 transform = Matrix4.CreateRotationX(pitch) *
			                    Matrix4.CreateRotationY(yaw) *
			                    Matrix4.CreateTranslation(Position);
			return transform;
		}

		public Matrix3 GetRotation()
		{
			float yaw = MathHelper.DegreesToRadians(Yaw);
			float pitch = MathHelper.DegreesToRadians(Pitch);
			Matrix3 transform = Matrix3.CreateRotationX(pitch) *
			                    Matrix3.CreateRotationY(yaw);
			return transform;
		}

		public Camera()
		{
		}
	}

	private Camera _camera = new();

	private bool WindowShouldClose
	{
		get
		{
			unsafe
			{
				return GLFW.WindowShouldClose(WindowPtr);
			}
		}
	}

	public MainWindow() : base(GetNativeWindowSettings())
	{
	}

	public void Run()
	{
		Initialize();

		_timer.Start();
		Time.UpdateTime(currentTime: _timer.Elapsed, delta: TimeSpan.Zero);
		while (!WindowShouldClose)
		{
			TimeSpan currentTime = _timer.Elapsed;
			TimeSpan prevTime = Time.CurrentTime;
			TimeSpan delta = currentTime - prevTime;
			Time.UpdateTime(currentTime, delta);

			NewInputFrame();
			ProcessWindowEvents(IsEventDriven);

			Update();
			Render();
			Swap();
		}
	}

	private void Initialize()
	{
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			// From GameWindow, have no idea if I really need it
			SetThreadAffinityMask(GetCurrentThread(), new IntPtr(1));
		}

		Context?.MakeCurrent();

		VSync = true;

		GLState.Init();
		GL.ClearColor(Color4.Darkgray);

		Visualizer.Initialize();

		const int atlasSize = 8;
		BlocksRegistry.Initialize(atlasSize);

		string vertexShaderSource = FileSystem.ReadTextFile("mesh_vertex.glsl");
		string fragmentShaderSource = FileSystem.ReadTextFile("mesh_fragment.glsl");
		Shader shader = new(vertexShaderSource, fragmentShaderSource);

		Image atlasImage = new("atlas.png", Image.ImageFormat.Rgb, flipY: true);

		int atlasTexture = GL.GenTexture();
		GL.BindTexture(TextureTarget.Texture2d, atlasTexture);
		GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
		GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
		GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter,
			(int)TextureMinFilter.Nearest);
		GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter,
			(int)TextureMinFilter.Nearest);
		GL.TexImage2D(TextureTarget.Texture2d,
			level: 0,
			InternalFormat.Rgb,
			atlasImage.Width,
			atlasImage.Height,
			border: 0,
			atlasImage.Format.ToOpenGLFormat(),
			PixelType.UnsignedByte,
			atlasImage.RawData);
		GL.GenerateMipmap(TextureTarget.Texture2d);

		int mvpLocation = shader.GetUniformLocation("mvp");

		_camera.Position = new Vector3(0, 0, 8);

		SimplexPerlin perlin = new(seed: 1234, NoiseQuality.Best);
		NoiseMap heightMap = new();
		NoiseMapBuilderPlane heightMapBuilder = new()
		{
			SourceModule = perlin,
			NoiseMap = heightMap,
			Seamless = true,
		};
		heightMapBuilder.SetSize(Chunk.ChunkWidth, Chunk.ChunkWidth);
		heightMapBuilder.SetBounds(0, 1, 0, 1);
		heightMapBuilder.Build();

		ScaleBias blockScale = new(perlin, scale: 0.1f, bias: 0.5f);

		IModule final = blockScale;

		Chunk chunk = new();
		for (int y = 0; y < Chunk.ChunkHeight; y++)
		{
			for (int z = 0; z < Chunk.ChunkWidth; z++)
			{
				for (int x = 0; x < Chunk.ChunkWidth; x++)
				{
					float heightNormalized = heightMap.GetValue(x, z);
					float height = heightNormalized * Chunk.ChunkHeight;
					int h = (int)height;
					Block block;
					if (y > h)
						block = new Block(BasicBlock.Air);
					else if (y == h)
						block = new Block(BasicBlock.Grass);
					else
						block = new Block(BasicBlock.Dirt);

					chunk.SetBlock(x, y, z, block);
				}
			}
		}

		ChunkMeshGenerator meshGenerator = new();
		ChunkMeshGenerator.Result result = meshGenerator.GenerateMesh(chunk);
		ChunkMesh chunkMesh = new();
		chunkMesh.SetData(result.Vertices, result.Indices);

		RenderFrame += () =>
		{
			TimeSpan curTime = Time.CurrentTime;
			double dt = Time.DeltaTime;
			_fpsStat.Update(curTime, dt);

			GL.Enable(EnableCap.DepthTest);

			shader.Bind();

			GL.BindTexture(TextureTarget.Texture2d, atlasTexture);

			Matrix4 proj = CreateProjectionMatrix();
			Matrix4 model = Matrix4.Identity;
			Matrix4 view = _camera.GetTransform().Inverted();
			Matrix4 viewProj = view * proj;
			Matrix4 mvp = model * viewProj;

			shader.SetUniform(mvpLocation, mvp);

			GL.Enable(EnableCap.CullFace);
			GL.CullFace(TriangleFace.Back);
			GL.Enable(EnableCap.Blend);
			GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

			GL.BindTexture(TextureTarget.Texture2d, atlasTexture);
			chunkMesh.Render();

			Visualizer.AddWorldLine(Vector3.Zero, Vector3.UnitX, Color.Red, depthTest: false);
			Visualizer.AddWorldLine(Vector3.Zero, Vector3.UnitY, Color.Green, depthTest: false);
			Visualizer.AddWorldLine(Vector3.Zero, Vector3.UnitZ, Color.Blue, depthTest: false);
			Visualizer.AddScreenText($"""
			                          FPS{(VSync ? "(VSync):" : ":")} {1 / dt:F1}
			                          Mean FPS: {1 / _fpsStat.LastMeanFps:F1}
			                          Min FPS: {1 / _fpsStat.LastMaxDt:F1} ({_fpsStat.LastMaxDt * 1000:F1}ms)
			                          Max FPS: {1 / _fpsStat.LastMinDt:F1}
			                          --------------
			                          Pos: {_camera.Position.X:F1} {_camera.Position.Y:F1} {_camera.Position.Z:F1}
			                          Speed: {CameraBaseMoveSpeed * _currentCameraMoveSpeedMultiplier:F1}
			                          """,
				new Vector2(5, 5),
				20,
				Color.DarkRed);
			Visualizer.RenderAndClear(viewProj, ClientSize);
		};

		OnResize(new ResizeEventArgs(ClientSize));
	}

	private Matrix4 CreateProjectionMatrix()
	{
		float width = ClientSize.X;
		float height = ClientSize.Y;
		float fovY = MathHelper.DegreesToRadians(60f);
		float aspect = width / height;
		float near = 0.1f;
		float far = 10000f;
		Matrix4 proj = Matrix4.CreatePerspectiveFieldOfView(fovY, aspect, near, far);
		return proj;
	}

	protected override void OnResize(ResizeEventArgs e)
	{
		base.OnResize(e);
		GL.Viewport(0, 0, e.Width, e.Height);
	}

	private void Update()
	{
		UpdateFrame?.Invoke();

		if (KeyboardState.IsKeyDown(Keys.Escape))
		{
			Close();
			return;
		}

		if (KeyboardState.IsKeyPressed(Keys.F9))
		{
			VSync = !VSync;
		}

		UpdatePlayer((float)Time.DeltaTime);
	}

	private void UpdatePlayer(float dt)
	{
		bool isCameraRotateMode = MouseState.IsButtonDown(MouseButton.Right);
		bool isCameraRun = KeyboardState.IsKeyDown(Keys.LeftShift);
		CursorState cursorState = isCameraRotateMode ? CursorState.Grabbed : CursorState.Normal;
		if (CursorState != cursorState)
			CursorState = cursorState;

		Vector3 localDelta = new();

		float runMultiplier = isCameraRun ? CameraRunMultiplier : 1f;
		float cameraMoveSpeed = (CameraBaseMoveSpeed * _currentCameraMoveSpeedMultiplier) * runMultiplier;
		if (KeyboardState.IsKeyDown(Keys.S))
			localDelta.Z += cameraMoveSpeed * dt;
		if (KeyboardState.IsKeyDown(Keys.W))
			localDelta.Z -= cameraMoveSpeed * dt;
		if (KeyboardState.IsKeyDown(Keys.D))
			localDelta.X += cameraMoveSpeed * dt;
		if (KeyboardState.IsKeyDown(Keys.A))
			localDelta.X -= cameraMoveSpeed * dt;
		if (KeyboardState.IsKeyDown(Keys.E))
			localDelta.Y += cameraMoveSpeed * dt;
		if (KeyboardState.IsKeyDown(Keys.Q))
			localDelta.Y -= cameraMoveSpeed * dt;

		if (isCameraRotateMode)
		{
			Vector2 mouseDelta = MouseState.Delta;
			_camera.Yaw -= mouseDelta.X * CameraRotateSpeed;
			_camera.Pitch -= mouseDelta.Y * CameraRotateSpeed;

			if (MouseState.ScrollDelta.Y != 0)
			{
				const float step = 1.1f;
				float mul = MouseState.ScrollDelta.Y > 0 ? step : 1 / step;
				float totalMul = float.Pow(mul, float.Abs(MouseState.ScrollDelta.Y));
				_currentCameraMoveSpeedMultiplier *= totalMul;
			}
		}

		Matrix3 rot = _camera.GetRotation();
		Vector3 globalDelta = localDelta * rot;
		_camera.Position += globalDelta;
	}

	private void Render()
	{
		GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);

		RenderFrame?.Invoke();
	}

	private void Swap()
	{
		Context.SwapBuffers();
	}

	private static NativeWindowSettings GetNativeWindowSettings()
	{
		NativeWindowSettings settings = new()
		{
			API = ContextAPI.OpenGL,
			APIVersion = Version.Parse("4.6"),
			Profile = ContextProfile.Core,
			Vsync = VSyncMode.Off,
			ClientSize = new Vector2i(1280, 720),
			Title = "Voxe",
		};

		return settings;
	}

	[DllImport("kernel32", SetLastError = true)]
	private static extern IntPtr SetThreadAffinityMask(IntPtr hThread, IntPtr dwThreadAffinityMask);

	[DllImport("kernel32")]
	private static extern IntPtr GetCurrentThread();
}