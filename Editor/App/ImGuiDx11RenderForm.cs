using System;
using System.Windows.Forms;
using ImGuiNET;
using SharpDX.Windows;
using T3.Core.IO;
using T3.Core.Logging;
using T3.SystemUi;

// ReSharper disable InconsistentNaming
// ReSharper disable IdentifierTypo
namespace T3.Editor.App
{
    /// <summary>
    /// A RenderForm class that maps windows RenderForm events and to ImGui 
    /// </summary>
    public class ImGuiDx11RenderForm : RenderForm
    {
        internal static IWindowsFormsMessageHandler[] InputMethods = Array.Empty<IWindowsFormsMessageHandler>();
        public ImGuiDx11RenderForm(string title)
            : base(title)
        {
            MouseMove += (o, e) => ImGui.GetIO().MousePos = new System.Numerics.Vector2(e.X, e.Y);
        }

        #region WM Message Ids
        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_LBUTTONUP = 0x0202;
        private const int WM_LBUTTONDBLCLK = 0x0203;
        private const int WM_RBUTTONDOWN = 0x0204;
        private const int WM_RBUTTONUP = 0x0205;
        private const int WM_RBUTTONDBLCLK = 0x0206;
        private const int WM_MBUTTONDOWN = 0x0207;
        private const int WM_MBUTTONUP = 0x0208;
        private const int WM_MBUTTONDBLCLK = 0x0209;

        private const int WM_MOUSEWHEEL = 0x020A;
        private const int WM_MOUSEHWHEEL = 0x020E;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_KEYUP = 0x0101;
        private const int WM_SYSKEYUP = 0x0105;
        private const int WM_CHAR = 0x0102;
        private const int WM_SETCURSOR = 0x0020;

        private const int WM_SETFOCUS = 0x0007;
        private const int WM_ACTIVATEAPP = 0x001C;
        #endregion

        #region VK constants
        private const int VK_SHIFT = 0x10;
        private const int VK_CONTROL = 0x11;
        private const int VK_ALT = 0x12;
        #endregion

        
        protected override void WndProc(ref System.Windows.Forms.Message m)
        {
            try
            {


                var filterAltKeyToPreventFocusLoss = (m.Msg == WM_SYSKEYDOWN || m.Msg == WM_SYSKEYUP) && (int)m.WParam == VK_ALT;
                if (!filterAltKeyToPreventFocusLoss)
                    base.WndProc(ref m);

                foreach (var inputMethod in InputMethods)
                    inputMethod.ProcessMessage(m);

                var isViewer = this == ProgramWindows.Viewer?.Form;

                ImGuiIOPtr io = ImGui.GetIO();

                switch (m.Msg)
                {

                    case WM_LBUTTONDOWN:
                    case WM_LBUTTONDBLCLK:
                    case WM_RBUTTONDOWN:
                    case WM_RBUTTONDBLCLK:
                    case WM_MBUTTONDOWN:
                    case WM_MBUTTONDBLCLK:
                    {
                        if (isViewer)
                            return;

                        int button = 0;
                        if (m.Msg == WM_LBUTTONDOWN || m.Msg == WM_LBUTTONDBLCLK) button = 0;
                        if (m.Msg == WM_RBUTTONDOWN || m.Msg == WM_RBUTTONDBLCLK) button = 1;
                        if (m.Msg == WM_MBUTTONDOWN || m.Msg == WM_MBUTTONDBLCLK) button = 2;
                        // TODO
                        //if (!ImGui.IsAnyMouseDown() && ::GetCapture() == NULL)
                        //    ::SetCapture(hwnd);
                        io.MouseDown[button] = true;
                        return;
                    }
                    case WM_LBUTTONUP:
                    case WM_RBUTTONUP:
                    case WM_MBUTTONUP:
                    {
                        int button = 0;
                        if (m.Msg == WM_LBUTTONUP) button = 0;
                        if (m.Msg == WM_RBUTTONUP) button = 1;
                        if (m.Msg == WM_MBUTTONUP) button = 2;
                        io.MouseDown[button] = false;
                        // TODO
                        //if (!ImGui::IsAnyMouseDown() && ::GetCapture() == hwnd)
                        //    ::ReleaseCapture();
                        return;
                    }
                    case WM_MOUSEWHEEL:
                        io.MouseWheel += (short)(((uint)(long)m.WParam >> 16) & 0xffff) / 120.0f; // TODO (float)WHEEL_DELTA;
                        return;
                    case WM_MOUSEHWHEEL:
                        io.MouseWheelH += (short)(((uint)(long)m.WParam >> 16) & 0xffff) / 120.0f; // TODO (float)WHEEL_DELTA;
                        return;
                    case WM_KEYDOWN:
                    case WM_SYSKEYDOWN:
                        switch ((int)m.WParam)
                        {
                            case VK_SHIFT:
                                io.KeyShift = true;
                                io.KeysDown[(int)m.WParam] = true;
                                io.KeysDown[(int)Key.ShiftKey] = true;
                                break;
                            case VK_CONTROL:
                                io.KeyCtrl = true;
                                io.KeysDown[(int)Key.CtrlKey] = true;
                                break;
                            case VK_ALT:
                                io.KeyAlt = true;
                                io.KeysDown[(int)Key.Alt] = true;
                                KeyHandler.SetKeyDown(Key.Alt);
                                break;
                            default:
                            {
                                if ((int)m.WParam < 256)
                                    io.KeysDown[(int)m.WParam] = true;
                                break;
                            }
                        }

                        return;
                    case WM_KEYUP:
                    case WM_SYSKEYUP:
                        switch ((int)m.WParam)
                        {
                            case VK_SHIFT:
                                io.KeyShift = false;
                                io.KeysDown[(int)Key.ShiftKey] = false;
                                break;
                            case VK_CONTROL:
                                io.KeyCtrl = false;
                                io.KeysDown[(int)Key.CtrlKey] = false;
                                break;
                            case VK_ALT:
                                io.KeyAlt = false;
                                io.KeysDown[(int)Key.Alt] = false;
                                KeyHandler.SetKeyUp(Key.Alt);
                                break;
                            default:
                            {
                                if ((int)m.WParam < 256)
                                    io.KeysDown[(int)m.WParam] = false;
                                break;
                            }
                        }

                        return;
                    case WM_CHAR:
                        // You can also use ToAscii()+GetKeyboardState() to retrieve characters.
                        if ((int)m.WParam > 0 && (int)m.WParam < 0x10000)
                            io.AddInputCharacter((ushort)m.WParam);
                        return;
                    case WM_SETCURSOR:
                        if ((((int)m.LParam & 0xFFFF) == 1) && UpdateMouseCursor())
                            m.Result = (IntPtr)1;
                        return;
                    case WM_SETFOCUS:
                        for (int i = 0; i < io.KeysDown.Count; i++)
                            io.KeysDown[i] = false;
                        io.KeyShift = false;
                        io.KeyCtrl = false;
                        io.KeyAlt = false;
                        break;

                    case WM_ACTIVATEAPP:
                        if (m.WParam.ToInt64() == 0) /* Being deactivated */
                        {
                            io.KeysDown[(int)Key.Alt] = false;
                            KeyHandler.SetKeyUp(Key.Alt);
                        }

                        break;
                }
            }
            catch (NullReferenceException)
            {
                Log.Warning("Detected invalid event message that would trigger null-reference exception");
            }
        }

        private bool UpdateMouseCursor()
        {
            ImGuiIOPtr io = ImGui.GetIO();
            if (((uint)io.ConfigFlags & (uint)ImGuiConfigFlags.NoMouseCursorChange) > 0)
                return false;

            ImGuiMouseCursor imgui_cursor = ImGui.GetMouseCursor();
            if (imgui_cursor == ImGuiMouseCursor.None || io.MouseDrawCursor)
            {
                // Hide OS mouse cursor if imgui is drawing it or if it wants no cursor
                Cursor.Current = null;
                return true;
            }

            Cursor newCursor = null;

            // Show OS mouse cursor
            switch (imgui_cursor)
            {
                case ImGuiMouseCursor.Arrow:
                    newCursor = Cursors.Arrow;
                    break;
                case ImGuiMouseCursor.TextInput:
                    newCursor = Cursors.IBeam;
                    break;
                case ImGuiMouseCursor.ResizeAll:
                    newCursor = Cursors.SizeAll;
                    break;
                case ImGuiMouseCursor.ResizeEW:
                    newCursor = Cursors.SizeWE;
                    break;
                case ImGuiMouseCursor.ResizeNS:
                    newCursor = Cursors.SizeNS;
                    break;
                case ImGuiMouseCursor.ResizeNESW:
                    newCursor = Cursors.SizeNESW;
                    break;
                case ImGuiMouseCursor.ResizeNWSE:
                    newCursor = Cursors.SizeNWSE;
                    break;
                case ImGuiMouseCursor.Hand:
                    newCursor = Cursors.Hand;
                    break;
                default:
                    newCursor = Cursors.Arrow;
                    break;
            }

            if (Cursor.Current != newCursor)
            {
                Cursor = newCursor;
            }

            return true;
        }
    }
}