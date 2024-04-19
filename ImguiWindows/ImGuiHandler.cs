using System.Drawing;
using System.Numerics;
using System.Runtime.CompilerServices;
using ImGuiNET;

namespace SilkWindows;

internal class ImGuiHandler
{
    private readonly IImguiDrawer _drawer;
    private readonly string _windowTitle;
    private readonly IntPtr? _originalContext;
    private readonly object _contextLock;
    private readonly FontPack? _fontPack;
    private readonly string _mainWindowId;
    private readonly string _childWindowId;
    private ImFonts? _fontObj;
    private readonly IntPtr _context;
    
    public ImGuiHandler(IImguiImplementation impl, IImguiDrawer drawer, FontPack? fontPack, object? lockObj)
    {
        _windowTitle = impl.WindowOptions.Title;
        _mainWindowId = impl.MainWindowId;
        _childWindowId = impl.ChildWindowId;
        _drawer = drawer;
        _fontPack = fontPack;
        _contextLock = lockObj ?? new object();
        _imguiController = impl;
        
        lock (_contextLock)
        {
            var previousContext = ImGui.GetCurrentContext();
            _originalContext = previousContext == IntPtr.Zero ? null : previousContext;
            _context = impl.InitializeControllerContext();
            InitializeStyle();
        }
    }
    
    private unsafe void InitializeStyle()
    {
        if (_originalContext.HasValue)
        {
            var myContext = _context;
            
            // first we switch to the previous imgui context
            ImGui.SetCurrentContext(_originalContext.Value);
            
            // we copy the style from the previous context
            ImGuiStyle copiedStyle = default;
            Unsafe.Copy(destination: ref copiedStyle, source: ImGui.GetStyle().NativePtr);
            
            // we switch back to our own context
            ImGui.SetCurrentContext(myContext);
            
            // we apply the copied style to the current context
            Unsafe.Copy(ImGui.GetStyle().NativePtr, ref copiedStyle);
        }
        
        var colorVector = ImGui.GetStyle().Colors[(int)ImGuiCol.WindowBg];
        var colorVecByteValue = colorVector * 255;
        ClearColor = Color.FromArgb((int)colorVecByteValue.X, (int)colorVecByteValue.Y, (int)colorVecByteValue.Z);
        
        // do we need or want to do this?
        ImGui.GetIO().ConfigFlags |= ImGuiConfigFlags.ViewportsEnable;
        
        if (!_fontPack.HasValue)
        {
            _fontObj = new ImFonts([]);
            return;
        }
        
        var fontPack = _fontPack.Value;
        
        var io = ImGui.GetIO();
        var fontAtlasPtr = io.Fonts;
        var fonts = new ImFontPtr[4];
        fonts[0] = fontAtlasPtr.AddFontFromFileTTF(fontPack.Small.Path, fontPack.Small.PixelSize);
        fonts[1] = fontAtlasPtr.AddFontFromFileTTF(fontPack.Regular.Path, fontPack.Regular.PixelSize);
        fonts[2] = fontAtlasPtr.AddFontFromFileTTF(fontPack.Bold.Path, fontPack.Bold.PixelSize);
        fonts[3] = fontAtlasPtr.AddFontFromFileTTF(fontPack.Large.Path, fontPack.Large.PixelSize);
        
        if (!fontAtlasPtr.Build())
        {
            Console.WriteLine("Failed to build font atlas");
        }
        
        _fontObj = new ImFonts(fonts);
        _drawer.Init();
    }
    
    public void Draw(Vector2 windowSize, double deltaTime)
    {
        lock (_contextLock)
        {
            _imguiController.ClearFrame(ClearColor);
            var contextToRestore = ImGui.GetCurrentContext();
            if (contextToRestore == IntPtr.Zero || contextToRestore == _context)
            {
                contextToRestore = _originalContext ?? _context;
            }
            
            ImGui.SetCurrentContext(_context);
            _imguiController.StartUpdate((float)deltaTime);
            
            ImGui.SetNextWindowSize(windowSize);
            ImGui.SetNextWindowPos(new Vector2(0, 0));
            
            const ImGuiWindowFlags windowFlags = ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize |
                                                 ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.AlwaysAutoResize;
            
            ImGui.Begin(_mainWindowId, windowFlags);
            
            ImGui.BeginChild(_childWindowId, Vector2.Zero, false);
            _drawer.OnRender(_windowTitle, deltaTime, _fontObj!);
            ImGui.EndChild();
            
            ImGui.End();
            
            _imguiController.Render();
            _imguiController.Render();
            
            // restore
            ImGui.SetCurrentContext(contextToRestore);
        }
    }
    
    public void DisposeOfImguiContext()
    {
        lock (_contextLock)
        {
            _imguiController.Dispose();
        }
    }
    
    private readonly IImguiImplementation _imguiController;
    public Color ClearColor { get; protected set; } = Color.Black;
}