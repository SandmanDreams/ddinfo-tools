using DevilDaggersInfo.Tools.Engine;
using DevilDaggersInfo.Tools.Ui.Main;
using DevilDaggersInfo.Tools.User.Settings;
using ImGuiGlfw;
using ImGuiNET;
using Silk.NET.GLFW;
using Silk.NET.OpenGL;
using System.Runtime.InteropServices;

namespace DevilDaggersInfo.Tools;

public sealed unsafe class Application
{
	private const double _maxMainDelta = 0.25;
	private const double _mainLoopLength = 1 / 300.0;

	private readonly Glfw _glfw;
	private readonly GL _gl;
	private readonly WindowHandle* _window;
	private readonly ImGuiController _imGuiController;
	private readonly GlfwInput _glfwInput;
	private readonly UiRenderer _uiRenderer;
	private readonly Shortcuts _shortcuts;
	private readonly MainWindow _mainWindow;

	private readonly IntPtr _iconPtr;

	private double _currentTime;
	private double _frameTime;

	private int _currentSecond;
	private int _renders;

	public Application(
		Glfw glfw,
		GL gl,
		WindowHandle* window,
		ImGuiController imGuiController,
		GlfwInput glfwInput,
		ResourceManager resourceManager,
		GameInstallationValidator gameInstallationValidator,
		UiRenderer uiRenderer,
		Shortcuts shortcuts,
		MainWindow mainWindow)
	{
		_glfw = glfw;
		_gl = gl;
		_window = window;
		_imGuiController = imGuiController;
		_glfwInput = glfwInput;
		_uiRenderer = uiRenderer;
		_shortcuts = shortcuts;
		_mainWindow = mainWindow;

		_currentTime = glfw.GetTime();

		gameInstallationValidator.ValidateInstallation();

		int iconWidth = resourceManager.InternalResources.ApplicationIconTexture.Width;
		int iconHeight = resourceManager.InternalResources.ApplicationIconTexture.Height;

		_iconPtr = Marshal.AllocHGlobal(iconWidth * iconHeight * 4);
		Marshal.Copy(resourceManager.InternalResources.ApplicationIconTexture.Pixels, 0, _iconPtr, iconWidth * iconHeight * 4);
		Image image = new()
		{
			Width = iconWidth,
			Height = iconHeight,
			Pixels = (byte*)_iconPtr,
		};
		_glfw.SetWindowIcon(_window, 1, &image);

		Root.Application = this;
	}

	public int Fps { get; private set; }
	public float FrameTime => (float)_frameTime;
	public float TotalTime { get; private set; }

	public PerSecondCounter RenderCounter { get; } = new();
	public float LastRenderDelta { get; private set; }

	public void Run()
	{
		while (!_glfw.WindowShouldClose(_window))
		{
			double expectedNextFrame = _glfw.GetTime() + _mainLoopLength;
			Main();

			while (_glfw.GetTime() < expectedNextFrame)
				Thread.Yield();
		}

		_imGuiController.Destroy();
		_gl.Dispose();
		_glfw.Terminate();

		Marshal.FreeHGlobal(_iconPtr);
	}

	private void Main()
	{
		double mainStartTime = _glfw.GetTime();
		if (_currentSecond != (int)mainStartTime)
		{
			Fps = _renders;
			_renders = 0;
			_currentSecond = (int)mainStartTime;
		}

		_frameTime = mainStartTime - _currentTime;
		if (_frameTime > _maxMainDelta)
			_frameTime = _maxMainDelta;

		TotalTime += FrameTime;

		_currentTime = mainStartTime;

		_glfw.PollEvents();

		Render();
		_renders++;

		_glfw.SwapBuffers(_window);
	}

	private void Render()
	{
		float deltaF = (float)_frameTime;

		RenderCounter.Increment();
		LastRenderDelta = deltaF;

		_imGuiController.Update(deltaF);

		ImGui.DockSpaceOverViewport(0, null, ImGuiDockNodeFlags.PassthruCentralNode);

		_gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

		_uiRenderer.Render(deltaF);

		ImGuiIOPtr io = ImGui.GetIO();

		_shortcuts.Handle(io, _glfwInput);

		if (io.WantSaveIniSettings)
			UserSettings.SaveImGuiIni(io);

		_imGuiController.Render();

		_glfwInput.EndFrame();

		if (_mainWindow.ShouldClose)
		{
			_glfw.SetWindowShouldClose(_window, true);
		}
	}
}
