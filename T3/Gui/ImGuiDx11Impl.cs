using ImGuiNET;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;
using Vector2 = System.Numerics.Vector2;

namespace T3
{
    public class ImGuiDx11Impl : IDisposable
    {
        private Device _device;
        private DeviceContext _deviceContext;
        private Buffer _vb;
        private Buffer _ib;
        private ShaderBytecode _vertexShaderBlob;
        private VertexShader _vertexShader;
        private InputLayout _inputLayout;
        private Buffer _vertexContantBuffer;
        private ShaderBytecode _pixelShaderBlob;
        private PixelShader _pixelShader;
        private SamplerState _fontSampler;
        private ShaderResourceView _fontTextureView;
        private RasterizerState _rasterizerState;
        private BlendState _blendState;
        private DepthStencilState _depthStencilState;
        private int _vertexBufferSize = 5000, _indexBufferSize = 1000;

        private bool _frameBegun;

        private IntPtr _fontAtlasID = (IntPtr)1;

        private int _windowWidth;
        private int _windowHeight;
        private Vector2 _scaleFactor = Vector2.One;

        public static ImFontPtr FontNormal { get; set; }
        public static ImFontPtr FontBold { get; set; }
        public static ImFontPtr FontSmall { get; set; }

        public ImGuiDx11Impl(Device device, int width, int height)
        {
            _device = device;
            _deviceContext = device.ImmediateContext;
            _windowWidth = width;
            _windowHeight = height;

            IntPtr context = ImGui.CreateContext();
            ImGui.SetCurrentContext(context);

            FontNormal = ImGui.GetIO().Fonts.AddFontFromFileTTF(@"Fonts/Roboto-Regular.ttf", 15f);
            FontBold = ImGui.GetIO().Fonts.AddFontFromFileTTF(@"Fonts/Roboto-Black.ttf", 15f);
            FontSmall = ImGui.GetIO().Fonts.AddFontFromFileTTF(@"Fonts/Roboto-Black.ttf", 12f);

            CreateDeviceObjects();
            SetKeyMappings();

            SetPerFrameImGuiData(1f / 60f);

            ImGui.NewFrame();
            _frameBegun = true;
        }


        public void WindowResized(int width, int height)
        {
            _windowWidth = width;
            _windowHeight = height;
        }

        public void DestroyDeviceObjects()
        {
            Dispose();
        }

        public void RenderImDrawData(ImDrawDataPtr draw_data)
        {
            if (_vb == null || _vertexBufferSize < draw_data.TotalVtxCount)
            {
                DisposeObj(ref _vb);
                _vertexBufferSize = draw_data.TotalVtxCount + 5000;
                _vb = new Buffer(_device,
                                 new BufferDescription()
                                 {
                                     SizeInBytes = _vertexBufferSize * Unsafe.SizeOf<ImDrawVert>(),
                                     Usage = ResourceUsage.Dynamic,
                                     BindFlags = BindFlags.VertexBuffer,
                                     CpuAccessFlags = CpuAccessFlags.Write
                                 });
            }

            if (_ib == null || _indexBufferSize < draw_data.TotalIdxCount)
            {
                DisposeObj(ref _ib);
                _indexBufferSize = draw_data.TotalIdxCount + 10000;
                _ib = new Buffer(_device,
                                 new BufferDescription()
                                 {
                                     SizeInBytes = _indexBufferSize * Unsafe.SizeOf<ushort>(),
                                     Usage = ResourceUsage.Dynamic,
                                     BindFlags = BindFlags.IndexBuffer,
                                     CpuAccessFlags = CpuAccessFlags.Write
                                 });
            }

            // Copy and convert all vertices into a single contiguous buffer
            SharpDX.DataStream vbStream, ibStream;
            _deviceContext.MapSubresource(_vb, MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None, out vbStream);
            _deviceContext.MapSubresource(_ib, MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None, out ibStream);
            for (int n = 0; n < draw_data.CmdListsCount; n++)
            {
                ImDrawListPtr cmd_list = draw_data.CmdListsRange[n];
                vbStream.WriteRange(cmd_list.VtxBuffer.Data, (uint)(cmd_list.VtxBuffer.Size * Unsafe.SizeOf<ImDrawVert>()));
                ibStream.WriteRange(cmd_list.IdxBuffer.Data, (uint)(cmd_list.IdxBuffer.Size * Unsafe.SizeOf<ushort>()));
            }

            vbStream.Dispose();
            ibStream.Dispose();
            _deviceContext.UnmapSubresource(_vb, 0);
            _deviceContext.UnmapSubresource(_ib, 0);

            // Setup orthographic projection matrix into our constant buffer
            // Our visible imgui space lies from draw_data->DisplayPos (top left) to draw_data->DisplayPos+data_data->DisplaySize (bottom right). 
            ImGuiIOPtr io = ImGui.GetIO();
            Matrix4x4 mvp = Matrix4x4.CreateOrthographicOffCenter(0.0f, io.DisplaySize.X, io.DisplaySize.Y, 0.0f, -1.0f, 1.0f);
            SharpDX.DataStream cbStream;
            _deviceContext.MapSubresource(_vertexContantBuffer, MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None, out cbStream);
            cbStream.Write(mvp);
            cbStream.Dispose();
            _deviceContext.UnmapSubresource(_vertexContantBuffer, 0);

            // Backup DX state that will be modified to restore it afterwards (unfortunately this is very ugly looking and verbose. Close your eyes!)
            var prevScissorRects = new SharpDX.Mathematics.Interop.RawRectangle[16];
            _deviceContext.Rasterizer.GetScissorRectangles(prevScissorRects);
            var prevViewports = _deviceContext.Rasterizer.GetViewports<SharpDX.Mathematics.Interop.RawViewportF>();
            var prevRasterizerState = _deviceContext.Rasterizer.State;
            var prevBlendState = _deviceContext.OutputMerger.BlendState;
            var prevBlendFactor = _deviceContext.OutputMerger.BlendFactor;
            var prevSampleMask = _deviceContext.OutputMerger.BlendSampleMask;
            var prevDepthStencilState = _deviceContext.OutputMerger.DepthStencilState;
            var prevStencilRef = _deviceContext.OutputMerger.DepthStencilReference;
            var prevPSShaderResource = _deviceContext.PixelShader.GetShaderResources(0, 1);
            var prevPSSampler = _deviceContext.PixelShader.GetSamplers(0, 1);
            var prevPS = _deviceContext.PixelShader.Get();
            var prevVS = _deviceContext.VertexShader.Get();
            var prevVSConstantBuffer = _deviceContext.VertexShader.GetConstantBuffers(0, 1);
            var prevPrimitiveTopology = _deviceContext.InputAssembler.PrimitiveTopology;
            _deviceContext.InputAssembler.GetIndexBuffer(out var prevIndexBuffer, out var prevIndexBufferFormat, out var prevIndexBufferOffset);
            Buffer[] prevVertexBuffer = new Buffer[1];
            int[] prevVertexBufferOffset = new int[1], prevVertexBufferStride = new int[1];
            _deviceContext.InputAssembler.GetVertexBuffers(0, 1, prevVertexBuffer, prevVertexBufferOffset, prevVertexBufferStride);
            var prevInputLayout = _deviceContext.InputAssembler.InputLayout;

            // Setup viewport
            _deviceContext.Rasterizer.SetViewport(0, 0, draw_data.DisplaySize.X, draw_data.DisplaySize.Y);

            // Bind shader and vertex buffers
            int stride = Unsafe.SizeOf<ImDrawVert>();
            int offset = 0;
            _deviceContext.InputAssembler.InputLayout = _inputLayout;
            _deviceContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_vb, stride, offset));
            _deviceContext.InputAssembler.SetIndexBuffer(_ib, Format.R16_UInt, 0);
            _deviceContext.InputAssembler.PrimitiveTopology = SharpDX.Direct3D.PrimitiveTopology.TriangleList;
            _deviceContext.VertexShader.SetShader(_vertexShader, null, 0);
            _deviceContext.VertexShader.SetConstantBuffer(0, _vertexContantBuffer);
            _deviceContext.PixelShader.SetShader(_pixelShader, null, 0);
            _deviceContext.PixelShader.SetSampler(0, _fontSampler);

            // Setup render state
            _deviceContext.OutputMerger.BlendState = _blendState;
            _deviceContext.OutputMerger.BlendFactor = new SharpDX.Mathematics.Interop.RawColor4(0.0f, 0.0f, 0.0f, 0.0f);
            _deviceContext.OutputMerger.DepthStencilState = _depthStencilState;
            _deviceContext.Rasterizer.State = _rasterizerState;

            // Render command lists
            int vtx_offset = 0;
            int idx_offset = 0;
            Vector2 pos = draw_data.DisplayPos;
            for (int n = 0; n < draw_data.CmdListsCount; n++)
            {
                ImDrawListPtr cmd_list = draw_data.CmdListsRange[n];
                for (int cmd_i = 0; cmd_i < cmd_list.CmdBuffer.Size; cmd_i++)
                {
                    ImDrawCmdPtr pcmd = cmd_list.CmdBuffer[cmd_i];
                    if (pcmd.UserCallback != IntPtr.Zero)
                    {
                        throw new NotImplementedException();
                    }
                    else
                    {
                        _deviceContext.Rasterizer.SetScissorRectangle((int)(pcmd.ClipRect.X - pos.X), (int)(pcmd.ClipRect.Y - pos.Y),
                                                                      (int)(pcmd.ClipRect.Z - pos.X), (int)(pcmd.ClipRect.W - pos.Y));
                        _deviceContext.PixelShader.SetShaderResource(0, (ShaderResourceView)pcmd.TextureId);
                        _deviceContext.DrawIndexed((int)pcmd.ElemCount, idx_offset, vtx_offset);
                    }

                    idx_offset += (int)pcmd.ElemCount;
                }

                vtx_offset += cmd_list.VtxBuffer.Size;
            }

            // Restore modified DX state
            _deviceContext.Rasterizer.SetScissorRectangles(prevScissorRects);
            _deviceContext.Rasterizer.SetViewports(prevViewports);
            _deviceContext.Rasterizer.State = prevRasterizerState;
            _deviceContext.OutputMerger.BlendState = prevBlendState;
            _deviceContext.OutputMerger.BlendFactor = prevBlendFactor;
            _deviceContext.OutputMerger.BlendSampleMask = prevSampleMask;
            _deviceContext.OutputMerger.DepthStencilState = prevDepthStencilState;
            _deviceContext.OutputMerger.DepthStencilReference = prevStencilRef;
            _deviceContext.PixelShader.SetShaderResources(0, prevPSShaderResource);
            _deviceContext.PixelShader.SetSamplers(0, prevPSSampler);
            _deviceContext.PixelShader.Set(prevPS);
            _deviceContext.VertexShader.Set(prevVS);
            _deviceContext.VertexShader.SetConstantBuffers(0, prevVSConstantBuffer);
            _deviceContext.InputAssembler.PrimitiveTopology = prevPrimitiveTopology;
            _deviceContext.InputAssembler.SetIndexBuffer(prevIndexBuffer, prevIndexBufferFormat, prevIndexBufferOffset);
            _deviceContext.InputAssembler.SetVertexBuffers(0, prevVertexBuffer, prevVertexBufferOffset, prevVertexBufferStride);
            _deviceContext.InputAssembler.InputLayout = prevInputLayout;
        }

        public unsafe void CreateFontsTexture()
        {
            // Build texture atlas
            ImGuiIOPtr io = ImGui.GetIO();
            io.Fonts.GetTexDataAsRGBA32(out var pixels, out var width, out var height, out _);
            io.Fonts.SetTexID(_fontAtlasID);

            // Upload texture to graphics system
            var textureDesc = new Texture2DDescription()
                              {
                                  Width = width,
                                  Height = height,
                                  MipLevels = 1,
                                  ArraySize = 1,
                                  Format = Format.R8G8B8A8_UNorm,
                                  SampleDescription = new SampleDescription() { Count = 1, Quality = 0 },
                                  Usage = ResourceUsage.Default,
                                  BindFlags = BindFlags.ShaderResource,
                                  CpuAccessFlags = CpuAccessFlags.None
                              };
            SharpDX.DataBox box = new SharpDX.DataBox((IntPtr)pixels, width * 4, 0);
            Texture2D texture = new Texture2D(_device, textureDesc, new[] { box });
            texture.DebugName = "FImgui Font Atlas";
            _fontTextureView = new ShaderResourceView(_device, texture);
            texture.Dispose();

            // Store our identifier
            io.Fonts.TexID = (IntPtr)_fontTextureView;

            var samplerDesc = new SamplerStateDescription()
                              {
                                  Filter = Filter.MinMagMipLinear,
                                  AddressU = TextureAddressMode.Wrap,
                                  AddressV = TextureAddressMode.Wrap,
                                  AddressW = TextureAddressMode.Wrap,
                                  MipLodBias = 0.0f,
                                  ComparisonFunction = Comparison.Always,
                                  MinimumLod = 0.0f,
                                  MaximumLod = 0.0f
                              };
            _fontSampler = new SamplerState(_device, samplerDesc);
        }

        public bool CreateDeviceObjects()
        {
            if (_device == null)
                return false;
            if (_fontSampler == null)
                InvalidateDeviceObjects();

            // Create the vertex shader
            string vertexShader =
                @"cbuffer vertexBuffer : register(b0) 
                {
                float4x4 ProjectionMatrix; 
                };
                struct VS_INPUT
                {
                float2 pos : POSITION;
                float4 col : COLOR0;
                float2 uv  : TEXCOORD0;
                };
                
                struct PS_INPUT
                {
                float4 pos : SV_POSITION;
                float4 col : COLOR0;
                float2 uv  : TEXCOORD0;
                };
                
                PS_INPUT main(VS_INPUT input)
                {
                PS_INPUT output;
                output.pos = mul( ProjectionMatrix, float4(input.pos.xy, 0.f, 1.f));
                output.col = input.col;
                output.uv  = input.uv;
                return output;
                }";

            _vertexShaderBlob = ShaderBytecode.Compile(vertexShader, "main", "vs_4_0", ShaderFlags.None, EffectFlags.None);
            if (_vertexShaderBlob == null)
                return false;
            _vertexShader = new VertexShader(_device, _vertexShaderBlob);
            if (_vertexShader == null)
                return false;

            // Create the input layout
            _inputLayout = new InputLayout(_device, ShaderSignature.GetInputSignature(_vertexShaderBlob),
                                           new[]
                                           {
                                               new InputElement("POSITION", 0, Format.R32G32_Float, 0, 0),
                                               new InputElement("TEXCOORD", 0, Format.R32G32_Float, 8, 0),
                                               new InputElement("COLOR", 0, Format.R8G8B8A8_UNorm, 16, 0)
                                           });

            // Create the constant buffer
            _vertexContantBuffer = new Buffer(_device,
                                              new BufferDescription()
                                              {
                                                  SizeInBytes = 4 * 4 * 4 /*TODO sizeof(Matrix4x4)*/,
                                                  Usage = ResourceUsage.Dynamic,
                                                  BindFlags = BindFlags.ConstantBuffer,
                                                  CpuAccessFlags = CpuAccessFlags.Write
                                              });

            // Create the pixel shader
            string pixelShader =
                @"struct PS_INPUT
                {
                float4 pos : SV_POSITION;
                float4 col : COLOR0;
                float2 uv  : TEXCOORD0;
                };
                sampler sampler0;
                Texture2D texture0;
                
                float4 main(PS_INPUT input) : SV_Target
                {
                float4 out_col = input.col * texture0.Sample(sampler0, input.uv); 
                return out_col; 
                }";
            _pixelShaderBlob = ShaderBytecode.Compile(pixelShader, "main", "ps_4_0", ShaderFlags.None, EffectFlags.None);
            if (_pixelShaderBlob == null)
                return false;
            _pixelShader = new PixelShader(_device, _pixelShaderBlob);
            if (_pixelShader == null)
                return false;

            // Create the blending setup
            var blendDesc = new BlendStateDescription() { AlphaToCoverageEnable = false, IndependentBlendEnable = false };
            blendDesc.RenderTarget[0].IsBlendEnabled = true;
            blendDesc.RenderTarget[0].SourceBlend = BlendOption.SourceAlpha;
            blendDesc.RenderTarget[0].DestinationBlend = BlendOption.InverseSourceAlpha;
            blendDesc.RenderTarget[0].BlendOperation = BlendOperation.Add;
            blendDesc.RenderTarget[0].SourceAlphaBlend = BlendOption.InverseSourceAlpha;
            blendDesc.RenderTarget[0].DestinationAlphaBlend = BlendOption.Zero;
            blendDesc.RenderTarget[0].AlphaBlendOperation = BlendOperation.Add;
            blendDesc.RenderTarget[0].RenderTargetWriteMask = ColorWriteMaskFlags.All;
            _blendState = new BlendState(_device, blendDesc);

            // Create the rasterizer state
            var rasterizerDesc = new RasterizerStateDescription()
                                 {
                                     FillMode = FillMode.Solid,
                                     CullMode = CullMode.None,
                                     IsScissorEnabled = true,
                                     IsDepthClipEnabled = true
                                 };
            _rasterizerState = new RasterizerState(_device, rasterizerDesc);

            // Create depth-stencil State
            var depthStencilDesc = new DepthStencilStateDescription()
            {
                IsDepthEnabled = false,
                DepthWriteMask = DepthWriteMask.All,
                DepthComparison = Comparison.Always,
                IsStencilEnabled = false
            };
            depthStencilDesc.FrontFace.FailOperation =
                depthStencilDesc.FrontFace.DepthFailOperation = depthStencilDesc.FrontFace.PassOperation = StencilOperation.Keep;
            depthStencilDesc.BackFace = depthStencilDesc.FrontFace;
            _depthStencilState = new DepthStencilState(_device, depthStencilDesc);

            CreateFontsTexture();

            return true;
        }

        void DisposeObj<T>(ref T obj) where T : class, IDisposable
        {
            obj?.Dispose();
            obj = null;
        }

        void InvalidateDeviceObjects()
        {
            if (_device == null)
                return;

            DisposeObj(ref _fontSampler);
            DisposeObj(ref _fontTextureView);
            DisposeObj(ref _ib);
            DisposeObj(ref _vb);
            DisposeObj(ref _blendState);
            DisposeObj(ref _depthStencilState);
            DisposeObj(ref _rasterizerState);
            DisposeObj(ref _pixelShader);
            DisposeObj(ref _pixelShaderBlob);
            DisposeObj(ref _vertexContantBuffer);
            DisposeObj(ref _inputLayout);
            DisposeObj(ref _vertexShader);
            DisposeObj(ref _vertexShaderBlob);
        }

        private void SetPerFrameImGuiData(float deltaSeconds)
        {
            ImGuiIOPtr io = ImGui.GetIO();
            io.DisplaySize = new Vector2(_windowWidth / _scaleFactor.X, _windowHeight / _scaleFactor.Y);
            io.DisplayFramebufferScale = _scaleFactor;
            io.DeltaTime = deltaSeconds; // DeltaTime is in seconds.
        }

        private static void SetKeyMappings()
        {
            ImGuiIOPtr io = ImGui.GetIO();
            io.KeyMap[(int)ImGuiKey.Tab] = (int)Keys.Tab;
            io.KeyMap[(int)ImGuiKey.LeftArrow] = (int)Keys.Left;
            io.KeyMap[(int)ImGuiKey.RightArrow] = (int)Keys.Right;
            io.KeyMap[(int)ImGuiKey.UpArrow] = (int)Keys.Up;
            io.KeyMap[(int)ImGuiKey.DownArrow] = (int)Keys.Down;
            io.KeyMap[(int)ImGuiKey.PageUp] = (int)Keys.PageUp;
            io.KeyMap[(int)ImGuiKey.PageDown] = (int)Keys.PageDown;
            io.KeyMap[(int)ImGuiKey.Home] = (int)Keys.Home;
            io.KeyMap[(int)ImGuiKey.End] = (int)Keys.End;
            io.KeyMap[(int)ImGuiKey.Delete] = (int)Keys.Delete;
            io.KeyMap[(int)ImGuiKey.Backspace] = (int)Keys.Back;
            io.KeyMap[(int)ImGuiKey.Enter] = (int)Keys.Enter;
            io.KeyMap[(int)ImGuiKey.Escape] = (int)Keys.Escape;
            io.KeyMap[(int)ImGuiKey.A] = (int)Keys.A;
            io.KeyMap[(int)ImGuiKey.C] = (int)Keys.C;
            io.KeyMap[(int)ImGuiKey.V] = (int)Keys.V;
            io.KeyMap[(int)ImGuiKey.X] = (int)Keys.X;
            io.KeyMap[(int)ImGuiKey.Y] = (int)Keys.Y;
            io.KeyMap[(int)ImGuiKey.Z] = (int)Keys.Z;
        }

        public void Dispose()
        {
            InvalidateDeviceObjects();
        }
    }
}