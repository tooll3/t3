#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using JeremyAnsel.Media.Dds;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;
using SharpDX.WIC;
using T3.Core.DataTypes;
using T3.Core.Logging;
using T3.Core.Resource.Dds;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;
using Texture2D = T3.Core.DataTypes.Texture2D;
using Texture3D = T3.Core.DataTypes.Texture3D;

namespace T3.Core.Resource;

/// <summary>
/// Loads or creates resources that can be loaded into a shader, such as textures, buffers, SRVs, UAVs, etc
/// </summary>
public sealed partial class ResourceManager
{
    public static Device Device => _instance._device;

    public void Init(Device device)
    {
        InitializeDevice(device);
    }

    private static bool TryCreateTexture2d(Stream stream, out Texture2D? texture, [NotNullWhen(false)] out string? failureReason)
    {
        try
        {
            using var factory = new ImagingFactory();
            using var bitmapDecoder = new BitmapDecoder(factory, stream, DecodeOptions.CacheOnDemand);
            using var formatConverter = new FormatConverter(factory);
            using var bitmapFrameDecode = bitmapDecoder.GetFrame(0);
            formatConverter.Initialize(bitmapFrameDecode, PixelFormat.Format32bppRGBA, BitmapDitherType.None, null, 0.0, BitmapPaletteType.Custom);

            
            texture = CreateTexture2DFromBitmap(Device, formatConverter);
            failureReason = null;
            return true;
        }
        catch (Exception e)
        {
            failureReason = e.Message;
            Log.Info($"Info: couldn't access file: {e.Message}.");
            texture = null;
            return false;
        }
    }
    
    private static bool TryCreateTextureResourceFromFile(FileResource fileResource, Texture2D? currentValue, [NotNullWhen(true)] out Texture2D? resource, [NotNullWhen(false)] out string? failureReason)
    {
        var extension = fileResource.FileExtension;
        if (!fileResource.TryOpenFileStream(out var fileStream, out failureReason, FileAccess.Read))
        {
            resource = null;
            return false;
        }
        
        using var stream = fileStream;
        
        Texture2D? texture;
        
        if (string.Equals(extension, ".dds", StringComparison.OrdinalIgnoreCase))
        {
            var ddsFile = DdsFile.FromStream(stream);
            try
            {
                DdsDirectX.CreateTexture(ddsFile, Device, Device.ImmediateContext, out var dxTextureResource, out var srv);
                srv?.Dispose();
                var dxTex = (SharpDX.Direct3D11.Texture2D)dxTextureResource;
                texture = new Texture2D(dxTex);
            }
            catch (Exception e)
            {
                failureReason = $"Failed to create texture from DDS: {e}";
                resource = null;
                return false;
            }
        }
        else
        {
            if (!TryCreateTexture2d(stream, out texture, out failureReason))
            {
                resource = null;
                return false;
            }
        }
        
        if (texture == null)
        {
            failureReason = "Failed to create texture";
            resource = null;
            return false;
        }
        
        texture.Name = fileResource.FileInfo?.Name;
        resource = texture;
        failureReason = null;
        return true;
    }
    
    public static void CreateStructuredBufferUav(Buffer? buffer, UnorderedAccessViewBufferFlags bufferFlags, ref UnorderedAccessView? uav)
    {
        if (buffer == null)
            return;

        try
        {
            if ((buffer.Description.OptionFlags & ResourceOptionFlags.BufferStructured) == 0)
            {
                // Log.Warning($"{nameof(SrvFromStructuredBuffer)} - input buffer is not structured, skipping SRV creation.");
                return;
            }

            uav?.Dispose();
            var uavDesc = new UnorderedAccessViewDescription
                              {
                                  Dimension = UnorderedAccessViewDimension.Buffer,
                                  Format = Format.Unknown,
                                  Buffer = new UnorderedAccessViewDescription.BufferResource
                                               {
                                                   FirstElement = 0,
                                                   ElementCount = buffer.Description.SizeInBytes / buffer.Description.StructureByteStride,
                                                   Flags = bufferFlags
                                               }
                              };
            uav = new UnorderedAccessView(Device, buffer, uavDesc);
        }
        catch (Exception e)
        {
            Log.Warning("Failed to create UAV " + e.Message);
        }
    }

    public static void CreateStructuredBufferSrv(Buffer? buffer, ref ShaderResourceView? srv)
    {
        if (buffer == null)
            return;

        try
        {
            if ((buffer.Description.OptionFlags & ResourceOptionFlags.BufferStructured) == 0)
            {
                // Log.Warning($"{nameof(SrvFromStructuredBuffer)} - input buffer is not structured, skipping SRV creation.");
                return;
            }

            srv?.Dispose();
            var srvDesc = new ShaderResourceViewDescription
                              {
                                  Dimension = ShaderResourceViewDimension.ExtendedBuffer,
                                  Format = Format.Unknown,
                                  BufferEx = new ShaderResourceViewDescription.ExtendedBufferResource
                                                 {
                                                     FirstElement = 0,
                                                     ElementCount = buffer.Description.SizeInBytes / buffer.Description.StructureByteStride
                                                 }
                              };
            srv = new ShaderResourceView(Device, buffer, srvDesc);
        }
        catch (Exception e)
        {
            Log.Warning("Failed to create SRV:" + e.Message);
        }
    }

    public static void SetupStructuredBuffer(int sizeInBytes, int stride, ref Buffer? buffer)
    {
        try
        {
            if (buffer == null || buffer.Description.SizeInBytes != sizeInBytes)
            {
                buffer?.Dispose();
                var bufferDesc = new BufferDescription
                                     {
                                         Usage = ResourceUsage.Default,
                                         BindFlags = BindFlags.UnorderedAccess | BindFlags.ShaderResource,
                                         SizeInBytes = sizeInBytes,
                                         OptionFlags = ResourceOptionFlags.BufferStructured,
                                         StructureByteStride = stride
                                     };
                try
                {
                    buffer = new Buffer(Device, bufferDesc);
                }
                catch (Exception e)
                {
                    Log.Error($"Failed to setup structured buffer (stride:{stride} {sizeInBytes}b):" + e.Message);
                }
            }
        }
        catch (Exception e)
        {
            Log.Warning("Failed to create Structured buffer " + e.Message);
        }
    }

    public static void SetupStructuredBuffer(DataStream data, int sizeInBytes, int stride, ref Buffer? buffer)
    {
        if (buffer == null || buffer.Description.SizeInBytes != sizeInBytes)
        {
            buffer?.Dispose();
            var bufferDesc = new BufferDescription
                                 {
                                     Usage = ResourceUsage.Default,
                                     BindFlags = BindFlags.UnorderedAccess | BindFlags.ShaderResource,
                                     SizeInBytes = sizeInBytes,
                                     OptionFlags = ResourceOptionFlags.BufferStructured,
                                     StructureByteStride = stride
                                 };
            buffer = new Buffer(Device, data, bufferDesc);
        }
        else
        {
            Device.ImmediateContext.UpdateSubresource(new DataBox(data.DataPointer, 0, 0), buffer);
        }
    }

    public static void SetupStructuredBuffer<T>(T[] bufferData, int sizeInBytes, int stride, ref Buffer? buffer) where T : struct
    {
        using var data = new DataStream(sizeInBytes, true, true);

        data.WriteRange(bufferData);
        data.Position = 0;

        SetupStructuredBuffer(data, sizeInBytes, stride, ref buffer);
    }

    public static void SetupStructuredBuffer<T>(T[] bufferData, ref Buffer? buffer) where T : struct
    {
        int stride = Marshal.SizeOf(typeof(T));
        int sizeInBytes = stride * bufferData.Length;
        SetupStructuredBuffer(bufferData, sizeInBytes, stride, ref buffer);
    }

    public static void CreateBufferUav<T>(Buffer? buffer, Format format, ref UnorderedAccessView? uav)
    {
        if (buffer == null)
            return;

        if ((buffer.Description.OptionFlags & ResourceOptionFlags.BufferStructured) != 0)
        {
            Log.Warning("Input buffer is structured, skipping UAV creation.");
            return;
        }

        uav?.Dispose();
        var desc = new UnorderedAccessViewDescription
                       {
                           Dimension = UnorderedAccessViewDimension.Buffer,
                           Format = format,
                           Buffer = new UnorderedAccessViewDescription.BufferResource
                                        {
                                            FirstElement = 0,
                                            ElementCount = buffer.Description.SizeInBytes / Marshal.SizeOf<T>(),
                                            Flags = UnorderedAccessViewBufferFlags.None
                                        }
                       };
        uav = new UnorderedAccessView(Device, buffer, desc);
    }

    public static void SetupIndirectBuffer(int sizeInBytes, ref Buffer? buffer)
    {
        var bufferDesc = new BufferDescription
                             {
                                 Usage = ResourceUsage.Default,
                                 BindFlags = BindFlags.UnorderedAccess | BindFlags.ShaderResource,
                                 SizeInBytes = sizeInBytes,
                                 OptionFlags = ResourceOptionFlags.DrawIndirectArguments,
                             };
        SetupBuffer(bufferDesc, ref buffer);
    }

    public static void SetupBuffer(BufferDescription bufferDesc, ref Buffer? buffer)
    {
        buffer ??= new Buffer(Device, bufferDesc);
    }

    public static void SetupConstBuffer<T>(T bufferData, [NotNull] ref Buffer? buffer) where T : struct
    {
        using var data = new DataStream(Marshal.SizeOf(typeof(T)), true, true);

        data.Write(bufferData);
        data.Position = 0;

        if (buffer == null)
        {
            var bufferDesc = new BufferDescription
                                 {
                                     Usage = ResourceUsage.Default,
                                     SizeInBytes = Marshal.SizeOf(typeof(T)),
                                     BindFlags = BindFlags.ConstantBuffer
                                 };
            buffer = new Buffer(Device, data, bufferDesc);
        }
        else
        {
            Device.ImmediateContext.UpdateSubresource(new DataBox(data.DataPointer, 0, 0), buffer);
        }
    }

    private void InitializeDevice(Device device)
    {
        _device = device;
        var samplerDesc = new SamplerStateDescription
                              {
                                  Filter = Filter.MinMagMipLinear,
                                  AddressU = TextureAddressMode.Clamp,
                                  AddressV = TextureAddressMode.Clamp,
                                  AddressW = TextureAddressMode.Clamp,
                                  MipLodBias = 0.0f,
                                  MaximumAnisotropy = 1,
                                  ComparisonFunction = Comparison.Never,
                                  BorderColor = new RawColor4(1.0f, 1.0f, 1.0f, 1.0f),
                                  MinimumLod = -Single.MaxValue,
                                  MaximumLod = Single.MaxValue,
                              };

        DefaultSamplerState = new SamplerState(device, samplerDesc);
    }

    public static void CreateShaderResourceView<T>(T resource, string? name, [NotNullWhen(true)] ref ShaderResourceView? shaderResourceView)
    where T : AbstractTexture
    {
        CreateTextureView(resource, name, ref shaderResourceView, 
                                    constructor: (device, texture) => new ShaderResourceView(device, texture));
    }

    public static void CreateRenderTargetView<T>(T resource, string? name, ref RenderTargetView? renderTargetView)
        where T : AbstractTexture
    {
        CreateTextureView(resource, name, ref renderTargetView, 
                             constructor: (device, texture) => new RenderTargetView(device, texture));
    }

    public static void CreateUnorderedAccessView<T>(T resource, string? name, ref UnorderedAccessView? unorderedAccessView)
        where T : AbstractTexture
    {
        CreateTextureView(resource, name, ref unorderedAccessView, 
                             constructor: (device, texture) => new UnorderedAccessView(device, texture));
    }
    
    private static void CreateTextureView<T>(AbstractTexture resource, string? name, ref T? view, Func<Device, AbstractTexture, T> constructor) where T : ResourceView
    {
        view?.Dispose();
        view = constructor(Device, resource);
        view.DebugName = name;
    }

    public static Texture2D CreateTexture2D(Texture2DDescription description, DataRectangle[]? dataRectangles = null)
    {
        var dxTexture = new SharpDX.Direct3D11.Texture2D(Device, description, dataRectangles);
        return new Texture2D(dxTexture);
    }

    public static Texture3D CreateTexture3D(Texture3DDescription description)
    {
        var dxTexture = new SharpDX.Direct3D11.Texture3D(Device, description);
        return new Texture3D(dxTexture);
    }


    public static bool CreateTexture3d(Texture3DDescription description, [NotNullWhen(true)] ref Texture3D? texture)
    {
        var shouldCreateNew = texture == null;
        try
        {
            if (texture != null)
            {
                shouldCreateNew = shouldCreateNew || !EqualityComparer<Texture3DDescription>.Default.Equals(texture.Description, description);
            }
        }
        catch (Exception e)
        {
            shouldCreateNew = true;
            Log.Warning($"Failed to get texture description: {e}");
        }

        if (shouldCreateNew)
        {
            texture?.Dispose();
            texture = CreateTexture3D(description);
            return true;
        }

        // unchanged
        return false;
    }

    private Device _device = null!;
    public SamplerState DefaultSamplerState { get; private set; } = null!;

    public static Texture2D CreateTexture2DFromBitmap(Device device, BitmapSource bitmapSource)
    {
        // Allocate DataStream to receive the WIC image pixels
        var stride = bitmapSource.Size.Width * 4;
        using var buffer = new DataStream(bitmapSource.Size.Height * stride, true, true);

        // Copy the content of the WIC to the buffer
        bitmapSource.CopyPixels(stride, buffer);
        int mipLevels = (int)Math.Log(bitmapSource.Size.Width, 2.0) + 1;
        var texDesc = new Texture2DDescription
                          {
                              Width = bitmapSource.Size.Width,
                              Height = bitmapSource.Size.Height,
                              ArraySize = 1,
                              BindFlags = BindFlags.ShaderResource | BindFlags.RenderTarget,
                              Usage = ResourceUsage.Default,
                              CpuAccessFlags = CpuAccessFlags.None,
                              Format = Format.R8G8B8A8_UNorm,
                              MipLevels = mipLevels,
                              OptionFlags = ResourceOptionFlags.GenerateMipMaps,
                              SampleDescription = new SampleDescription(1, 0),
                          };
        var dataRectangles = new DataRectangle[mipLevels];
        for (var i = 0; i < mipLevels; i++)
        {
            dataRectangles[i] = new DataRectangle(buffer.DataPointer, stride);
            stride /= 2;
        }

        var dxTexture = new SharpDX.Direct3D11.Texture2D(device, texDesc, dataRectangles);
        return new Texture2D(dxTexture);
    }
}