using System;
using System.Diagnostics;
using System.Numerics;
using ImGuiNET;
using SharpDX.Direct3D11;
using T3.Core;
using T3.Core.Operator;

namespace T3.Gui.OutputUi
{
    public class Texture2dOutputUi : OutputUi<Texture2D>
    {
        private ShaderResourceView _srv;

        public override void DrawValue(ISlot slot)
        {
            if (slot is Slot<Texture2D> typedSlot)
            {
                Invalidate(slot);
                _evaluationContext.Reset();
                var texture = typedSlot.GetValue(_evaluationContext);
                if (_srv == null || _srv.Resource != texture)
                {
                    _srv?.Dispose();
                    _srv = new ShaderResourceView(ResourceManager.Instance()._device, texture);
                }
                ImGui.Image((IntPtr)_srv, new Vector2(256.0f, 256.0f));
            }
            else
            {
                Debug.Assert(false);
            }
        }
    }
}