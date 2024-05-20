using System.Runtime.InteropServices;
using System;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using T3.Core.DataTypes.Vector;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Resource;
using T3.Core.Utils;

namespace examples.lib.img.use
{
    [Guid("57c1fe66-d8bc-4ea5-ad25-6986d4c2bba4")]
    public class KeepPreviousFrame2 : Instance<KeepPreviousFrame2>
    {
        [Output(Guid = "7ba708e0-1cbb-411a-aa2c-bc78d248d761")]
        public readonly Slot<Texture2D> TextureA = new();

        [Output(Guid = "B8C943B7-A402-4AE1-A489-EEFF900889CD")]
        public readonly Slot<Texture2D> TextureB = new();

        private uint _textureResId;

        public KeepPreviousFrame2()
        {
            TextureA.UpdateAction = UpdateTexture;
            TextureB.UpdateAction = UpdateTexture;
        }

        private void UpdateTexture(EvaluationContext context)
        {
            var keep = Keep.GetValue(context);
            if (!ImageA.IsConnected || !keep)
            {
                return;
            }

            var texture = ImageA.GetValue(context);
            if (texture == null)
                return;

            var description = texture.Description;

            var formatChanged = _prevTextureA == null || _prevTextureA.IsDisposed ||
                                _prevTextureB == null || _prevTextureB.IsDisposed ||
                                description.Height != _prevTextureView.Height ||
                                description.Width != _prevTextureView.Width ||
                                description.Format != _prevTextureView.Format ||
                                description.MipLevels != _prevTextureView.MipLevels ||
                                description.OptionFlags != _prevTextureView.OptionFlags ||
                                description.SampleDescription.Count != _prevTextureView.SampleDescription.Count;

            try
            {
                if (formatChanged)
                {
                    Utilities.Dispose(ref _prevTextureA);
                    Utilities.Dispose(ref _prevTextureB);

                    _prevTextureA = new Texture2D(ResourceManager.Device, description);
                    _prevTextureB = new Texture2D(ResourceManager.Device, description);
                    _prevTextureView = description;
                }

                ResourceManager.Device.ImmediateContext.CopyResource(texture, _bufferToggle ? _prevTextureA : _prevTextureB);
            }
            catch (Exception e)
            {
                Log.Error($"Failed to create Texture2d: {e.Message}", this);
            }

            TextureA.Value = _bufferToggle ? _prevTextureA : _prevTextureB;
            TextureB.Value = _bufferToggle ? _prevTextureB : _prevTextureA;
            TextureA.DirtyFlag.Clear();
            TextureB.DirtyFlag.Clear();

            _bufferToggle = !_bufferToggle;
        }

        protected override void Dispose(bool isDisposing)
        {
            if (!isDisposing)
                return;

            Utilities.Dispose(ref _prevTextureA);
            Utilities.Dispose(ref _prevTextureB);
        }
        
        private bool _bufferToggle;
        private Texture2D _prevTextureA;
        private Texture2D _prevTextureB;
        private Texture2DDescription _prevTextureView;

        [Input(Guid = "B304EFD5-A8E4-4213-9C85-8F482C55C880")]
        public readonly InputSlot<Texture2D> ImageA = new();

        [Input(Guid = "9BAE6E82-3691-4F1A-82E0-77B3953B7019")]
        public readonly InputSlot<bool> Keep = new();
    }
}