using System.Drawing;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Voxe.Render;

namespace Voxe;

public class MainWindow : GameWindow
{
	private const float CameraBaseMoveSpeed = 4f;
	private const float CameraRunMultiplier = 2f;
	private const float CameraRotateSpeed = 0.2f;

	private float _currentCameraMoveSpeedMultiplier = 1f;

	// TODO: Shit, don't use double for it
	private double _totalTime;

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

	public MainWindow() : base(GetGameWindowSettings(), GetNativeWindowSettings())
	{
	}

	protected override void OnLoad()
	{
		base.OnLoad();
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

		Chunk chunk = new();
		for (int y = 0; y < Chunk.ChunkHeight; y++)
		{
			if (y % 2 == 1)
			{
				continue;
			}

			Block block = y > 5 ? new Block(BasicBlock.Snow) : new Block(BasicBlock.Grass);
			for (int z = 0; z < Chunk.ChunkWidth; z++)
			{
				if (z % 2 == 1)
				{
					continue;
				}

				for (int x = 0; x < Chunk.ChunkWidth; x++)
				{
					if (x % 2 == 1)
					{
						continue;
					}

					chunk.SetBlock(x, y, z, block);
				}
			}
		}

		ChunkMeshGenerator generator = new();
		ChunkMeshGenerator.Result result = generator.GenerateMesh(chunk);
		ChunkMesh chunkMesh = new();
		chunkMesh.SetData(result.Vertices, result.Indices);

		double lastUpdateMeanFpsTime = 0f;
		double lastUpdateMinMaxFpsTime = 0f;
		int numFramesForMeanFps = 0;
		double lastMeanFps = 0f;
		double lastMinDt = 0f;
		double lastMaxDt = float.PositiveInfinity;
		double minDt = float.PositiveInfinity;
		double maxDt = 0;
		const float meanFpsUpdateTime = 0.5f;
		const float minMaxFpsUpdateTime = 2f;
		RenderFrame += (args) =>
		{
			if (_totalTime - lastUpdateMeanFpsTime > meanFpsUpdateTime)
			{
				lastMeanFps = (_totalTime - lastUpdateMeanFpsTime) / int.Max(numFramesForMeanFps, 1);
				lastUpdateMeanFpsTime = _totalTime;
				numFramesForMeanFps = 0;
			}

			if (_totalTime - lastUpdateMinMaxFpsTime > minMaxFpsUpdateTime)
			{
				lastUpdateMinMaxFpsTime = _totalTime;
				lastMinDt = minDt;
				lastMaxDt = maxDt;
				minDt = float.PositiveInfinity;
				maxDt = 0;
			}

			minDt = double.Min(minDt, UpdateTime);
			maxDt = double.Max(maxDt, UpdateTime);

			numFramesForMeanFps++;

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
			                          FPS: {1 / UpdateTime:F1}
			                          Mean FPS: {1 / lastMeanFps:F1}
			                          Min FPS: {1 / lastMaxDt:F1} ({lastMaxDt * 1000:F1}ms)
			                          Max FPS: {1 / lastMinDt:F1}
			                          --------------
			                          Pos: {_camera.Position.X:F1} {_camera.Position.Y:F1} {_camera.Position.Z:F1}
			                          Speed: {CameraBaseMoveSpeed * _currentCameraMoveSpeedMultiplier:F1}
			                          """,
				new Vector2(5, 5),
				20,
				Color.DarkRed);
			Visualizer.RenderAndClear(viewProj, ClientSize);
		};
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

	protected override void OnUpdateFrame(FrameEventArgs args)
	{
		double dt = args.Time;
		_totalTime += dt;

		base.OnUpdateFrame(args);

		if (KeyboardState.IsKeyDown(Keys.Escape))
		{
			Close();
			return;
		}

		UpdatePlayer((float)dt);
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
				_currentCameraMoveSpeedMultiplier *= mul;
			}
		}

		Matrix3 rot = _camera.GetRotation();
		Vector3 globalDelta = localDelta * rot;
		_camera.Position += globalDelta;
	}

	protected override void OnRenderFrame(FrameEventArgs args)
	{
		GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);

		base.OnRenderFrame(args);

		SwapBuffers();
	}

	private static GameWindowSettings GetGameWindowSettings()
	{
		GameWindowSettings settings = new();
		return settings;
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
}