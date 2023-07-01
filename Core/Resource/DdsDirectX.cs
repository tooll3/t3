// using JeremyAnsel.DirectX.D3D11;
// using JeremyAnsel.DirectX.Dxgi;

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using JeremyAnsel.Media.Dds;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Device = SharpDX.Direct3D11.Device;
using Resource = SharpDX.Direct3D11.Resource;

// ReSharper disable InconsistentNaming

namespace JeremyAnsel.DirectX.Dds
{
    public static class DdsDirectX
    {
        public static void CreateTexture(string fileName,
                                         Device device,
                                         DeviceContext context,
                                         out ShaderResourceView textureView)
        {
            DdsFile dds = DdsFile.FromFile(fileName);
            CreateTexture(dds, device, context, out textureView);
        }

        public static void CreateTexture(string fileName,
                                         Device device,
                                         DeviceContext context,
                                         out Resource texture,
                                         out ShaderResourceView textureView)
        {
            DdsFile dds = DdsFile.FromFile(fileName);
            CreateTexture(dds, device, context, out texture, out textureView);
        }

        public static void CreateTexture(Stream stream,
                                         Device device,
                                         DeviceContext context,
                                         out ShaderResourceView textureView)
        {
            DdsFile dds = DdsFile.FromStream(stream);
            CreateTexture(dds, device, context, out textureView);
        }

        public static void CreateTexture(Stream stream,
                                         Device device,
                                         DeviceContext context,
                                         out Resource texture,
                                         out ShaderResourceView textureView)
        {
            DdsFile dds = DdsFile.FromStream(stream);
            CreateTexture(dds, device, context, out texture, out textureView);
        }

        static void DisposeAndNull<T>(ref T obj) where T : class, IDisposable
        {
            obj?.Dispose();
            obj = null;
        }

        public static void CreateTexture(DdsFile dds,
                                         Device device,
                                         DeviceContext context,
                                         out ShaderResourceView textureView)
        {
            CreateTexture(dds, device, context, 0, out Resource texture, out textureView, out _);

            DisposeAndNull(ref texture);
        }

        public static void CreateTexture(DdsFile dds,
                                         Device device,
                                         DeviceContext context,
                                         out Resource texture,
                                         out ShaderResourceView textureView)
        {
            CreateTexture(dds, device, context, 0, out texture, out textureView, out _);
        }

        public static void CreateTexture(DdsFile dds,
                                         Device device,
                                         DeviceContext context,
                                         int maxSize,
                                         out ShaderResourceView textureView)
        {
            CreateTexture(dds, device, context, maxSize, out Resource texture, out textureView, out _);
            DisposeAndNull(ref texture);
        }

        public static void CreateTexture(DdsFile dds,
                                         Device device,
                                         DeviceContext context,
                                         int maxSize,
                                         out Resource texture,
                                         out ShaderResourceView textureView,
                                         out DdsAlphaMode alphaMode)
        {
            CreateTexture(
                          dds,
                          device,
                          context,
                          maxSize,
                          ResourceUsage.Default, // .Default,
                          BindFlags.ShaderResource,
                          CpuAccessFlags.None,
                          ResourceOptionFlags.None,
                          false,
                          out texture,
                          out textureView,
                          out alphaMode);
        }

        public static void CreateTexture(DdsFile dds,
                                         Device device,
                                         DeviceContext context,
                                         int maxSize,
                                         ResourceUsage usage,
                                         BindFlags bindOptions,
                                         CpuAccessFlags cpuAccessOptions,
                                         ResourceOptionFlags miscOptions,
                                         bool forceSRGB,
                                         out Resource texture,
                                         out ShaderResourceView textureView,
                                         out DdsAlphaMode alphaMode)
        {
            if (dds == null)
            {
                throw new ArgumentNullException(nameof(dds));
            }

            if (device == null)
            {
                throw new ArgumentNullException(nameof(device));
            }

            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            CreateTextureFromDDS(
                                 device,
                                 context,
                                 dds,
                                 dds.Data,
                                 maxSize,
                                 usage,
                                 bindOptions,
                                 cpuAccessOptions,
                                 miscOptions,
                                 forceSRGB,
                                 out texture,
                                 out textureView);

            alphaMode = dds.AlphaMode;
        }

        private static bool FillInitData(int width,
                                         int height,
                                         int depth,
                                         int mipCount,
                                         int arraySize,
                                         Format format,
                                         int maxSize,
                                         byte[] bitData,
                                         out int twidth,
                                         out int theight,
                                         out int tdepth,
                                         out int skipMip,
                                         out D3D11SubResourceData[] initData)
        {
            skipMip = 0;
            twidth = 0;
            theight = 0;
            tdepth = 0;
            initData = new D3D11SubResourceData[mipCount * arraySize];

            int pSrcBits = 0;
            int index = 0;

            for (int j = 0; j < arraySize; j++)
            {
                int w = width;
                int h = height;
                int d = depth;

                for (int i = 0; i < mipCount; i++)
                {
                    DdsHelpers.GetSurfaceInfo(w, h, (DdsFormat)format, out int NumBytes, out int RowBytes, out _);

                    if ((mipCount <= 1) || maxSize == 0 || (w <= maxSize && h <= maxSize && d <= maxSize))
                    {
                        if (twidth == 0)
                        {
                            twidth = w;
                            theight = h;
                            tdepth = d;
                        }

                        int dataLength = NumBytes * d;
                        var data = new byte[dataLength];
                        Array.Copy(bitData, pSrcBits, data, 0, dataLength);

                        initData[index] = new D3D11SubResourceData(data, (uint)RowBytes, (uint)NumBytes);
                        index++;
                    }
                    else if (j == 0)
                    {
                        // Count number of skipped mipmaps (first item only)
                        skipMip++;
                    }

                    pSrcBits += NumBytes * d;

                    w >>= 1;
                    h >>= 1;
                    d >>= 1;

                    if (w == 0)
                    {
                        w = 1;
                    }

                    if (h == 0)
                    {
                        h = 1;
                    }

                    if (d == 0)
                    {
                        d = 1;
                    }
                }
            }

            return index > 0;
        }

        private static void CreateD3DResources(Device device,
                                               ResourceDimension resDim,
                                               int width,
                                               int height,
                                               int depth,
                                               int mipCount,
                                               int arraySize,
                                               Format format,
                                               ResourceUsage usage,
                                               BindFlags bindFlags,
                                               CpuAccessFlags cpuAccessFlags,
                                               ResourceOptionFlags miscFlags,
                                               bool forceSRGB,
                                               bool isCubeMap,
                                               D3D11SubResourceData[] initData,
                                               out Resource texture,
                                               out ShaderResourceView textureView)
        {
            texture = null;
            textureView = null;

            if (forceSRGB)
            {
                format = (Format)DdsHelpers.MakeSrgb((DdsFormat)format);
            }

            switch (resDim)
            {
                case ResourceDimension.Texture1D:
                {
                    var tex1dDescription = new Texture1DDescription
                                               {
                                                   Format = format,
                                                   Width = width,
                                                   ArraySize = arraySize,
                                                   MipLevels = mipCount,
                                                   BindFlags = bindFlags,
                                                   Usage = usage,
                                                   CpuAccessFlags = cpuAccessFlags,
                                                   OptionFlags = miscFlags & ~ResourceOptionFlags.TextureCube,
                                               };

                    // We keep these for reference...
                    
                    // texture = device.CreateTexture1D(desc, initData);  // Jeremy implementation with device helper method
                    // texture = new Texture1D(device, tex1dDescription, dataRectangles);  // Failed because Texture1d doesn't support DataRectangles constructor
                    // var tex2d = new Texture2D(device, new Texture2DDescription(), dataRectangles); // Reference from DataRectangles with Texture2d

                    var dataBoxes = CreateDataBoxesFromSubresourceData(initData);
                    texture = new Texture1D(device, tex1dDescription, dataBoxes);

                    try
                    {
                        var SRVDesc = new ShaderResourceViewDescription
                                          {
                                              Format = format
                                          };

                        if (arraySize > 1)
                        {
                            SRVDesc.Dimension = ShaderResourceViewDimension.Texture1DArray;
                            SRVDesc.Texture1DArray = new ShaderResourceViewDescription.Texture1DArrayResource
                                                         {
                                                             MipLevels = (mipCount == 0) ? int.MaxValue : tex1dDescription.MipLevels,
                                                             ArraySize = arraySize
                                                         };
                        }
                        else
                        {
                            SRVDesc.Dimension = ShaderResourceViewDimension.Texture1D;
                            SRVDesc.Texture1D = new ShaderResourceViewDescription.Texture1DResource
                                                    {
                                                        MipLevels = (mipCount == 0) ? int.MaxValue : tex1dDescription.MipLevels
                                                    };
                        }

                        textureView = new ShaderResourceView(device, texture, SRVDesc);
                    }
                    catch
                    {
                        DisposeAndNull(ref texture);
                        throw;
                    }

                    break;
                }

                case ResourceDimension.Texture2D:
                {
                    var desc = new Texture2DDescription
                                   {
                                       Format = format,
                                       Width = width,
                                       Height = height,
                                       ArraySize = arraySize,
                                       MipLevels = mipCount,
                                       BindFlags = bindFlags,
                                       Usage = usage,
                                       CpuAccessFlags = cpuAccessFlags,
                                       OptionFlags = ResourceOptionFlags.None,
                                       //1,
                                       //0;
                                   };

                    if (isCubeMap)
                    {
                        desc.OptionFlags = miscFlags | ResourceOptionFlags.TextureCube;
                    }
                    else
                    {
                        desc.OptionFlags = miscFlags & ~ResourceOptionFlags.TextureCube;
                    }

                    if (format == Format.BC1_UNorm || format == Format.BC2_UNorm || format == Format.BC3_UNorm)
                    {
                        if ((width & 3) != 0 || (height & 3) != 0)
                        {
                            desc.Width = (int)((width + 3) & ~3U);
                            desc.Height = (int)((height + 3) & ~3U);
                        }
                    }

                    var dataBoxes = CreateDataBoxesFromSubresourceData(initData);
                    texture = new Texture2D(device, desc, dataBoxes);
                    //texture = device.CreateTexture2D(desc, initData);
                    
                    try
                    {
                        var SRVDesc = new ShaderResourceViewDescription
                                          {
                                              Format = format
                                          };

                        if (isCubeMap)
                        {
                            if (arraySize > 6)
                            {
                                SRVDesc.Dimension = ShaderResourceViewDimension.TextureCubeArray;
                                SRVDesc.TextureCubeArray = new ShaderResourceViewDescription.TextureCubeArrayResource
                                                               {
                                                                   MipLevels = (mipCount == 0) ? int.MaxValue : desc.MipLevels,
                                                                   // Earlier we set arraySize to (NumCubes * 6)
                                                                   CubeCount = arraySize / 6
                                                               };
                            }
                            else
                            {
                                SRVDesc.Dimension = ShaderResourceViewDimension.TextureCube;
                                SRVDesc.TextureCube = new ShaderResourceViewDescription.TextureCubeResource
                                                          {
                                                              MipLevels = (mipCount == 0) ? int.MaxValue : desc.MipLevels,
                                                          };
                            }
                        }
                        else if (arraySize > 1)
                        {
                            SRVDesc.Dimension = ShaderResourceViewDimension.Texture2DArray;
                            SRVDesc.Texture2DArray = new ShaderResourceViewDescription.Texture2DArrayResource
                                                         {
                                                             MipLevels = (mipCount == 0) ? int.MaxValue : desc.MipLevels,
                                                             ArraySize = arraySize
                                                         };
                        }
                        else
                        {
                            SRVDesc.Dimension = ShaderResourceViewDimension.Texture2D;
                            SRVDesc.Texture2D = new ShaderResourceViewDescription.Texture2DResource
                                                    {
                                                        MipLevels = (mipCount == 0) ? int.MaxValue : desc.MipLevels,
                                                    };
                        }

                        textureView = new ShaderResourceView(device, texture, SRVDesc);
                    }
                    catch
                    {
                        DisposeAndNull(ref texture);
                        throw;
                    }

                    break;
                }

                case ResourceDimension.Texture3D:
                {
                    var desc = new Texture3DDescription
                                   {
                                       Format = format,
                                       Width = width,
                                       Height = height,
                                       Depth = depth,
                                       MipLevels = mipCount,
                                       BindFlags = bindFlags,
                                       Usage = usage,
                                       CpuAccessFlags = cpuAccessFlags,
                                       OptionFlags = miscFlags & ~ResourceOptionFlags.TextureCube
                                   };

                    var dataBoxes = CreateDataBoxesFromSubresourceData(initData);
                    texture = new Texture3D(device, desc, dataBoxes);
                    //texture = device.CreateTexture3D(desc, initData);

                    try
                    {
                        var SRVDesc = new ShaderResourceViewDescription
                                          {
                                              Format = format,
                                              Dimension = ShaderResourceViewDimension.Texture3D,
                                              Texture3D = new ShaderResourceViewDescription.Texture3DResource
                                                              {
                                                                  MipLevels = (mipCount == 0) ? int.MaxValue : desc.MipLevels,
                                                              }
                                          };

                        textureView = new ShaderResourceView(device, texture, SRVDesc);
                    }
                    catch
                    {
                        DisposeAndNull(ref texture);
                        throw;
                    }

                    break;
                }
            }
        }

        private static DataBox[] CreateDataBoxesFromSubresourceData(D3D11SubResourceData[] initData)
        {
            var dataBoxes = new DataBox[initData.Length];
            //var dataRectangles = new DataRectangle[mipCount];
            for (var dataIndex = 0; dataIndex < initData.Length; dataIndex++)
            {
                var dataPointer = Marshal.UnsafeAddrOfPinnedArrayElement(initData[dataIndex].Data, 0);
                // dataRectangles[dataIndex] = new DataRectangle(dataPointer, (int)initData[0].Pitch);

                dataBoxes[dataIndex] = new DataBox
                                           {
                                               DataPointer = dataPointer,
                                               RowPitch = (int)initData[dataIndex].Pitch,
                                               SlicePitch = (int)initData[dataIndex].SlicePitch
                                           };
            }

            return dataBoxes;
        }

        private static bool IsBitMask(DdsPixelFormat ddpf, uint r, uint g, uint b, uint a)
        {
            return ddpf.RedBitMask == r && ddpf.GreenBitMask == g && ddpf.BlueBitMask == b && ddpf.AlphaBitMask == a;
        }

        [SuppressMessage("Microsoft.Maintainability", "CA1505:AvoidUnmaintainableCode")]
        [SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        [SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling")]
        private static void CreateTextureFromDDS(Device device,
                                                 DeviceContext context,
                                                 DdsFile dds,
                                                 byte[] bitData,
                                                 int maxSize,
                                                 ResourceUsage usage,
                                                 BindFlags bindOptions,
                                                 CpuAccessFlags cpuAccessOptions,
                                                 ResourceOptionFlags miscOptions,
                                                 bool forceSRGB,
                                                 out Resource texture,
                                                 out ShaderResourceView textureView)
        {
            int width = dds.Width;
            int height = dds.Height;
            int depth = dds.Depth;

            ResourceDimension resDim = (ResourceDimension)dds.ResourceDimension;
            int arraySize = Math.Max(1, dds.ArraySize);
            Format format = (Format)dds.Format;
            bool isCubeMap = false;

            if (dds.Format == DdsFormat.Unknown)
            {
                if (dds.PixelFormat.RgbBitCount == 32)
                {
                    if (IsBitMask(dds.PixelFormat, 0x000000ff, 0x0000ff00, 0x00ff0000, 0x00000000))
                    {
                        format = Format.B8G8R8X8_UNorm;
                        int length = bitData.Length / 4;
                        var bytes = new byte[length * 4];

                        for (int i = 0; i < length; i++)
                        {
                            bytes[i * 4 + 0] = bitData[i * 4 + 2];
                            bytes[i * 4 + 1] = bitData[i * 4 + 1];
                            bytes[i * 4 + 2] = bitData[i * 4 + 0];
                        }

                        bitData = bytes;
                    }
                }
                else if (dds.PixelFormat.RgbBitCount == 24)
                {
                    if (IsBitMask(dds.PixelFormat, 0x00ff0000, 0x0000ff00, 0x000000ff, 0x00000000))
                    {
                        format = Format.B8G8R8X8_UNorm;
                        int length = bitData.Length / 3;
                        var bytes = new byte[length * 4];

                        for (int i = 0; i < length; i++)
                        {
                            bytes[i * 4 + 0] = bitData[i * 3 + 0];
                            bytes[i * 4 + 1] = bitData[i * 3 + 1];
                            bytes[i * 4 + 2] = bitData[i * 3 + 2];
                        }

                        bitData = bytes;
                    }
                }
            }

            int mipCount = Math.Max(1, dds.MipmapCount);

            switch (format)
            {
                case Format.AI44:
                case Format.IA44:
                case Format.P8:
                case Format.A8P8:
                    throw new NotSupportedException(format + " format is not supported.");

                default:
                    if (DdsHelpers.GetBitsPerPixel((DdsFormat)format) == 0)
                    {
                        throw new NotSupportedException(format + " format is not supported.");
                    }

                    break;
            }

            switch (resDim)
            {
                case ResourceDimension.Texture1D:
                    // D3DX writes 1D textures with a fixed Height of 1
                    if ((dds.Options & DdsOptions.Height) != 0 && height != 1)
                    {
                        throw new InvalidDataException();
                    }

                    height = 1;
                    depth = 1;
                    break;

                case ResourceDimension.Texture2D:
                    if ((dds.ResourceMiscOptions & DdsResourceMiscOptions.TextureCube) != 0)
                    {
                        arraySize *= 6;
                        isCubeMap = true;
                    }

                    depth = 1;
                    break;

                case ResourceDimension.Texture3D:
                    if ((dds.Options & DdsOptions.Depth) == 0)
                    {
                        throw new InvalidDataException();
                    }

                    if (arraySize > 1)
                    {
                        throw new NotSupportedException();
                    }

                    break;

                default:
                    if ((dds.Options & DdsOptions.Depth) != 0)
                    {
                        resDim = ResourceDimension.Texture3D;
                    }
                    else
                    {
                        if ((dds.Caps2 & DdsAdditionalCaps.CubeMap) != 0)
                        {
                            // We require all six faces to be defined
                            if ((dds.Caps2 & DdsAdditionalCaps.CubeMapAllFaces) != DdsAdditionalCaps.CubeMapAllFaces)
                            {
                                throw new NotSupportedException();
                            }

                            arraySize = 6;
                            isCubeMap = true;
                        }

                        depth = 1;
                        resDim = ResourceDimension.Texture2D;

                        // Note there's no way for a legacy Direct3D 9 DDS to express a '1D' texture
                    }

                    break;
            }

            if ((miscOptions & ResourceOptionFlags.TextureCube) != 0
                && resDim == ResourceDimension.Texture2D
                && (arraySize % 6 == 0))
            {
                isCubeMap = true;
            }

            // Bound sizes (for security purposes we don't trust DDS file metadata larger than the D3D 11.x hardware requirements)
            if (mipCount > D3D11Constants.ReqMipLevels)
            {
                throw new NotSupportedException();
            }

            switch (resDim)
            {
                case ResourceDimension.Texture1D:
                    if (arraySize > D3D11Constants.ReqTexture1DArrayAxisDimension
                        || width > D3D11Constants.ReqTexture1DDimension)
                    {
                        throw new NotSupportedException();
                    }

                    break;

                case ResourceDimension.Texture2D:
                    if (isCubeMap)
                    {
                        // This is the right bound because we set arraySize to (NumCubes*6) above
                        if (arraySize > D3D11Constants.ReqTexture2DArrayAxisDimension
                            || width > D3D11Constants.ReqTextureCubeDimension
                            || height > D3D11Constants.ReqTextureCubeDimension)
                        {
                            throw new NotSupportedException();
                        }
                    }
                    else if (arraySize > D3D11Constants.ReqTexture2DArrayAxisDimension
                             || width > D3D11Constants.ReqTexture2DDimension
                             || height > D3D11Constants.ReqTexture2DDimension)
                    {
                        throw new NotSupportedException();
                    }

                    break;

                case ResourceDimension.Texture3D:
                    if (arraySize > 1
                        || width > D3D11Constants.ReqTexture3DDimension
                        || height > D3D11Constants.ReqTexture3DDimension
                        || depth > D3D11Constants.ReqTexture3DDimension)
                    {
                        throw new NotSupportedException();
                    }

                    break;

                default:
                    throw new NotSupportedException();
            }

            bool autogen = false;

            if (mipCount == 1)
            {
                // See if format is supported for auto-gen mipmaps (varies by feature level)
                // if (!device.CheckFormatSupport(format, out FormatSupport fmtSupport)
                //     && (fmtSupport & FormatSupport.MipAutogen) != 0)
                var fmtSupport = device.CheckFormatSupport(format);
                if ((fmtSupport & FormatSupport.MipAutogen) != 0)
                {
                    // 10level9 feature levels do not support auto-gen mipgen for volume textures
                    if (resDim != ResourceDimension.Texture3D
                        || device.FeatureLevel >= FeatureLevel.Level_10_0)
                    {
                        autogen = true;
                    }
                }
            }

            if (autogen)
            {
                // Create texture with auto-generated mipmaps
                CreateD3DResources(
                                   device,
                                   resDim,
                                   width,
                                   height,
                                   depth,
                                   0,
                                   arraySize,
                                   format,
                                   usage,
                                   bindOptions | BindFlags.RenderTarget,
                                   cpuAccessOptions,
                                   miscOptions | ResourceOptionFlags.GenerateMipMaps,
                                   forceSRGB,
                                   isCubeMap,
                                   null,
                                   out texture,
                                   out textureView);

                try
                {
                    DdsHelpers.GetSurfaceInfo(width, height, (DdsFormat)format, out int numBytes, out int rowBytes, out int numRows);

                    if (numBytes > bitData.Length)
                    {
                        throw new EndOfStreamException();
                    }

                    ShaderResourceViewDescription desc = textureView.Description;
                    int mipLevels = 1;

                    switch (desc.Dimension)
                    {
                        case ShaderResourceViewDimension.Texture1D:
                            mipLevels = desc.Texture1D.MipLevels;
                            break;

                        case ShaderResourceViewDimension.Texture1DArray:
                            mipLevels = desc.Texture1DArray.MipLevels;
                            break;

                        case ShaderResourceViewDimension.Texture2D:
                            mipLevels = desc.Texture2D.MipLevels;
                            break;

                        case ShaderResourceViewDimension.Texture2DArray:
                            mipLevels = desc.Texture2DArray.MipLevels;
                            break;

                        case ShaderResourceViewDimension.TextureCube:
                            mipLevels = desc.TextureCube.MipLevels;
                            break;

                        case ShaderResourceViewDimension.TextureCubeArray:
                            mipLevels = desc.TextureCubeArray.MipLevels;
                            break;

                        case ShaderResourceViewDimension.Texture3D:
                            mipLevels = desc.Texture3D.MipLevels;
                            break;

                        default:
                            throw new InvalidDataException();
                    }

                    if (arraySize > 1)
                    {
                        int pSrcBits = 0;

                        for (int item = 0; item < (uint)arraySize; item++)
                        {
                            if (pSrcBits + numBytes > bitData.Length)
                            {
                                throw new EndOfStreamException();
                            }

                            var data = new byte[numBytes];
                            Array.Copy(bitData, pSrcBits, data, 0, numBytes);

                            int res = Resource.CalculateSubResourceIndex(0, item, mipLevels);
                            
                            var dataPointer = Marshal.UnsafeAddrOfPinnedArrayElement(data, 0);
                            context.UpdateSubresource(texture, res, null, dataPointer, rowBytes, numBytes);

                            pSrcBits += numBytes;
                        }
                    }
                    else
                    {
                        var dataPointer = Marshal.UnsafeAddrOfPinnedArrayElement(bitData, 0);
                        context.UpdateSubresource(texture, 0, null, dataPointer, rowBytes, numBytes);
                    }

                    context.GenerateMips(textureView);
                }
                catch
                {
                    DisposeAndNull(ref textureView);
                    DisposeAndNull(ref texture);
                    throw;
                }
            }
            else
            {
                // Create the texture

                if (!FillInitData(
                                  width,
                                  height,
                                  depth,
                                  mipCount,
                                  arraySize,
                                  format,
                                  maxSize,
                                  bitData,
                                  out int twidth,
                                  out int theight,
                                  out int tdepth,
                                  out int skipMip,
                                  out D3D11SubResourceData[] initData))
                {
                    throw new InvalidDataException();
                }

                try
                {
                    CreateD3DResources(
                                       device,
                                       resDim,
                                       twidth,
                                       theight,
                                       tdepth,
                                       mipCount - skipMip,
                                       arraySize,
                                       format,
                                       usage,
                                       bindOptions,
                                       cpuAccessOptions,
                                       miscOptions,
                                       forceSRGB,
                                       isCubeMap,
                                       initData,
                                       out texture,
                                       out textureView);
                }
                catch
                {
                    if (maxSize == 0 && mipCount > 1)
                    {
                        // Retry with a maxsize determined by feature level
                        switch (device.FeatureLevel)
                        {
                            case FeatureLevel.Level_9_1:
                            case FeatureLevel.Level_9_2:
                                if (isCubeMap)
                                {
                                    maxSize = (int)D3D11Constants.FeatureLevel91ReqTextureCubeDimension;
                                }
                                else
                                {
                                    maxSize = resDim == ResourceDimension.Texture3D
                                                  ? (int)D3D11Constants.FeatureLevel91ReqTexture3DDimension
                                                  : (int)D3D11Constants.FeatureLevel91ReqTexture2DDimension;
                                }

                                break;

                            case FeatureLevel.Level_9_3:
                                maxSize = resDim == ResourceDimension.Texture3D
                                              ? (int)D3D11Constants.FeatureLevel91ReqTexture3DDimension
                                              : (int)D3D11Constants.FeatureLevel93ReqTexture2DDimension;
                                break;

                            case FeatureLevel.Level_10_0:
                            case FeatureLevel.Level_10_1:
                                maxSize = resDim == ResourceDimension.Texture3D
                                              ? (int)D3D11Constants.D3D10ReqTexture3DDimension
                                              : (int)D3D11Constants.D3D10ReqTexture2DDimension;
                                break;

                            default:
                                maxSize = resDim == ResourceDimension.Texture3D
                                              ? (int)D3D11Constants.ReqTexture3DDimension
                                              : (int)D3D11Constants.ReqTexture2DDimension;
                                break;
                        }

                        if (!FillInitData(
                                          width,
                                          height,
                                          depth,
                                          mipCount,
                                          arraySize,
                                          format,
                                          maxSize,
                                          bitData,
                                          out twidth,
                                          out theight,
                                          out tdepth,
                                          out skipMip,
                                          out initData))
                        {
                            throw new InvalidDataException();
                        }

                        CreateD3DResources(
                                           device,
                                           resDim,
                                           twidth,
                                           theight,
                                           tdepth,
                                           mipCount - skipMip,
                                           arraySize,
                                           format,
                                           usage,
                                           bindOptions,
                                           cpuAccessOptions,
                                           miscOptions,
                                           forceSRGB,
                                           isCubeMap,
                                           initData,
                                           out texture,
                                           out textureView);
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }
    }
}

/// <summary>
/// Specifies data for initializing a subresource.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct D3D11SubResourceData : IEquatable<D3D11SubResourceData>
{
    /// <summary>
    /// The initialization data.
    /// </summary>
    private readonly Array data;

    /// <summary>
    /// The distance (in bytes) from the beginning of one line of a texture to the next line.
    /// </summary>
    private readonly uint pitch;

    /// <summary>
    /// The distance (in bytes) from the beginning of one depth level to the next.
    /// </summary>
    private readonly uint slicePitch;

    /// <summary>
    /// Initializes a new instance of the <see cref="D3D11SubResourceData"/> struct.
    /// </summary>
    /// <param name="data">The initialization data.</param>
    /// <param name="pitch">The distance (in bytes) from the beginning of one line of a texture to the next line.</param>
    public D3D11SubResourceData(Array data, uint pitch)
    {
        this.data = data;
        this.pitch = pitch;
        slicePitch = 0;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="D3D11SubResourceData"/> struct.
    /// </summary>
    /// <param name="data">The initialization data.</param>
    /// <param name="pitch">The distance (in bytes) from the beginning of one line of a texture to the next line.</param>
    /// <param name="slicePitch">The distance (in bytes) from the beginning of one depth level to the next.</param>
    public D3D11SubResourceData(Array data, uint pitch, uint slicePitch)
    {
        this.data = data;
        this.pitch = pitch;
        this.slicePitch = slicePitch;
    }

    /// <summary>
    /// Gets the initialization data.
    /// </summary>
    [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Justification = "Reviewed")]
    public Array Data { get { return data; } }

    /// <summary>
    /// Gets the distance (in bytes) from the beginning of one line of a texture to the next line.
    /// </summary>
    public uint Pitch { get { return pitch; } }

    /// <summary>
    /// Gets the distance (in bytes) from the beginning of one depth level to the next.
    /// </summary>
    public uint SlicePitch { get { return slicePitch; } }

    /// <summary>
    /// Compares two <see cref="D3D11SubResourceData"/> objects. The result specifies whether the values of the two objects are equal.
    /// </summary>
    /// <param name="left">The left <see cref="D3D11SubResourceData"/> to compare.</param>
    /// <param name="right">The right <see cref="D3D11SubResourceData"/> to compare.</param>
    /// <returns><value>true</value> if the values of left and right are equal; otherwise, <value>false</value>.</returns>
    public static bool operator ==(D3D11SubResourceData left, D3D11SubResourceData right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Compares two <see cref="D3D11SubResourceData"/> objects. The result specifies whether the values of the two objects are unequal.
    /// </summary>
    /// <param name="left">The left <see cref="D3D11SubResourceData"/> to compare.</param>
    /// <param name="right">The right <see cref="D3D11SubResourceData"/> to compare.</param>
    /// <returns><value>true</value> if the values of left and right differ; otherwise, <value>false</value>.</returns>
    public static bool operator !=(D3D11SubResourceData left, D3D11SubResourceData right)
    {
        return !(left == right);
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current object.
    /// </summary>
    /// <param name="obj">The object to compare with the current object.</param>
    /// <returns><value>true</value> if the specified object is equal to the current object; otherwise, <value>false</value>.</returns>
    public override bool Equals(object obj)
    {
        if (!(obj is D3D11SubResourceData))
        {
            return false;
        }

        return Equals((D3D11SubResourceData)obj);
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current object.
    /// </summary>
    /// <param name="other">The object to compare with the current object.</param>
    /// <returns><value>true</value> if the specified object is equal to the current object; otherwise, <value>false</value>.</returns>
    public bool Equals(D3D11SubResourceData other)
    {
        return data == other.data
               && pitch == other.pitch
               && slicePitch == other.slicePitch;
    }

    /// <summary>
    /// Returns the hash code for this instance.
    /// </summary>
    /// <returns>A 32-bit signed integer that is the hash code for this instance.</returns>
    public override int GetHashCode()
    {
        return new
                   {
                       data,
                       pitch,
                       slicePitch
                   }
           .GetHashCode();
    }
}

public static class D3D11Constants
{
    /// <summary>
    /// D3D10 Index Strip Cut Value 16-Bit.
    /// </summary>
    public const uint D3D10IndexStripCutValue16Bit = 0xffff;

    /// <summary>
    /// D3D10 Index Strip Cut Value 32-Bit.
    /// </summary>
    public const uint D3D10IndexStripCutValue32Bit = 0xffffffff;

    /// <summary>
    /// D3D10 Index Strip Cut Value 8-Bit.
    /// </summary>
    public const uint D3D10IndexStripCutValue8Bit = 0xff;

    /// <summary>
    /// D3D10 Array Axis Address Range Bit Count.
    /// </summary>
    public const uint D3D10ArrayAxisAddressRangeBitCount = 9;

    /// <summary>
    /// D3D10 Clip Or Cull Distance Count.
    /// </summary>
    public const uint D3D10ClipOrCullDistanceCount = 8;

    /// <summary>
    /// D3D10 Clip Or Cull Distance Element Count.
    /// </summary>
    public const uint D3D10ClipOrCullDistanceElementCount = 2;

    /// <summary>
    /// D3D10 Compute Shader Constant Buffer Api Slot Count.
    /// </summary>
    public const uint D3D10ComputeShaderConstantBufferApiSlotCount = 14;

    /// <summary>
    /// D3D10 Compute Shader Constant Buffer Components.
    /// </summary>
    public const uint D3D10ComputeShaderConstantBufferComponents = 4;

    /// <summary>
    /// D3D10 Compute Shader Buffer Component Bit Count.
    /// </summary>
    public const uint D3D10ComputeShaderBufferComponentBitCount = 32;

    /// <summary>
    /// D3D10 Compute Shader Constant Buffer Hardware Slot Count.
    /// </summary>
    public const uint D3D10ComputeShaderConstantBufferHardwareSlotCount = 15;

    /// <summary>
    /// D3D10 Compute Shader Constant Buffer Register Components.
    /// </summary>
    public const uint D3D10ComputeShaderConstantBufferRegisterComponents = 4;

    /// <summary>
    /// D3D10 Compute Shader Constant Buffer Register Count.
    /// </summary>
    public const uint D3D10ComputeShaderConstantBufferRegisterCount = 15;

    /// <summary>
    /// D3D10 Compute Shader Constant Buffer Register Reads Per Instance.
    /// </summary>
    public const uint D3D10ComputeShaderConstantBufferRegisterReadsPerInstance = 1;

    /// <summary>
    /// D3D10 Compute Shader Constant Buffer Register Read Ports.
    /// </summary>
    public const uint D3D10ComputeShaderConstantBufferRegisterReadPorts = 1;

    /// <summary>
    /// D3D10 Compute Shader Flow Control Nesting Limit.
    /// </summary>
    public const uint D3D10ComputeShaderFlowControlNestingLimit = 64;

    /// <summary>
    /// D3D10 Compute Shader Immediate Constant Buffer Register Components.
    /// </summary>
    public const uint D3D10ComputeShaderImmediateConstantBufferRegisterComponents = 4;

    /// <summary>
    /// D3D10 Compute Shader Immediate Constant Buffer Register Count.
    /// </summary>
    public const uint D3D10ComputeShaderImmediateConstantBufferRegisterCount = 1;

    /// <summary>
    /// D3D10 Compute Shader Immediate Constant Buffer Register Reads Per Instance.
    /// </summary>
    public const uint D3D10ComputeShaderImmediateConstantBufferRegisterReadsPerInstance = 1;

    /// <summary>
    /// D3D10 Compute Shader Immediate Constant Buffer Register Read Ports.
    /// </summary>
    public const uint D3D10ComputeShaderImmediateConstantBufferRegisterReadPorts = 1;

    /// <summary>
    /// D3D10 Compute Shader Immediate Value Component Bit Count.
    /// </summary>
    public const uint D3D10ComputeShaderImmediateValueComponentBitCount = 32;

    /// <summary>
    /// D3D10 Compute Shader Input Resource Register Components.
    /// </summary>
    public const uint D3D10ComputeShaderInputResourceRegisterComponents = 1;

    /// <summary>
    /// D3D10 Compute Shader Input Resource Register Count.
    /// </summary>
    public const uint D3D10ComputeShaderInputResourceRegisterCount = 128;

    /// <summary>
    /// D3D10 Compute Shader Input Resource Register Reads Per Instance.
    /// </summary>
    public const uint D3D10ComputeShaderInputResourceRegisterReadsPerInstance = 1;

    /// <summary>
    /// D3D10 Compute Shader Input Resource Register Read Ports.
    /// </summary>
    public const uint D3D10ComputeShaderInputResourceRegisterReadPorts = 1;

    /// <summary>
    /// D3D10 Compute Shader Input Resource Slot Count.
    /// </summary>
    public const uint D3D10ComputeShaderInputResourceSlotCount = 128;

    /// <summary>
    /// D3D10 Compute Shader Sampler Register Components.
    /// </summary>
    public const uint D3D10ComputeShaderSamplerRegisterComponents = 1;

    /// <summary>
    /// D3D10 Compute Shader Sampler Register Count.
    /// </summary>
    public const uint D3D10ComputeShaderSamplerRegisterCount = 16;

    /// <summary>
    /// D3D10 Compute Shader Sampler Register Reads Per Instance.
    /// </summary>
    public const uint D3D10ComputeShaderSamplerRegisterReadsPerInstance = 1;

    /// <summary>
    /// D3D10 Compute Shader Sampler Register Read Ports.
    /// </summary>
    public const uint D3D10ComputeShaderSamplerRegisterReadPorts = 1;

    /// <summary>
    /// D3D10 Compute Shader Sampler Slot Count.
    /// </summary>
    public const uint D3D10ComputeShaderSamplerSlotCount = 16;

    /// <summary>
    /// D3D10 Compute Shader Subroutine Nesting Limit.
    /// </summary>
    public const uint D3D10ComputeShaderSubroutineNestingLimit = 32;

    /// <summary>
    /// D3D10 Compute Shader Temp Register Components.
    /// </summary>
    public const uint D3D10ComputeShaderTempRegisterComponents = 4;

    /// <summary>
    /// D3D10 Compute Shader Temp Register Component Bit Count.
    /// </summary>
    public const uint D3D10ComputeShaderTempRegisterComponentBitCount = 32;

    /// <summary>
    /// D3D10 Compute Shader Temp Register Count.
    /// </summary>
    public const uint D3D10ComputeShaderTempRegisterCount = 4096;

    /// <summary>
    /// D3D10 Compute Shader Temp Register Reads Per Instance.
    /// </summary>
    public const uint D3D10ComputeShaderTempRegisterReadsPerInstance = 3;

    /// <summary>
    /// D3D10 Compute Shader Temp Register Read Ports.
    /// </summary>
    public const uint D3D10ComputeShaderTempRegisterReadPorts = 3;

    /// <summary>
    /// D3D10 Compute Shader Tex Coord Range Reduction Max.
    /// </summary>
    public const int D3D10ComputeShaderTexCoordRangeReductionMax = 10;

    /// <summary>
    /// D3D10 Compute Shader Tex Coord Range Reduction Min.
    /// </summary>
    public const int D3D10ComputeShaderTexCoordRangeReductionMin = -10;

    /// <summary>
    /// D3D10 Compute Shader Texel Offset Max Negative.
    /// </summary>
    public const int D3D10ComputeShaderTexelOffsetMaxNegative = -8;

    /// <summary>
    /// D3D10 Compute Shader Texel Offset Max Positive.
    /// </summary>
    public const int D3D10ComputeShaderTexelOffsetMaxPositive = 7;

    /// <summary>
    /// D3D10 Default Blend Factor Alpha.
    /// </summary>
    public const float D3D10DefaultBlendFactorAlpha = 1.0f;

    /// <summary>
    /// D3D10 Default Blend Factor Blue.
    /// </summary>
    public const float D3D10DefaultBlendFactorBlue = 1.0f;

    /// <summary>
    /// D3D10 Default Blend Factor Green.
    /// </summary>
    public const float D3D10DefaultBlendFactorGreen = 1.0f;

    /// <summary>
    /// D3D10 Default Blend Factor Red.
    /// </summary>
    public const float D3D10DefaultBlendFactorRed = 1.0f;

    /// <summary>
    /// D3D10 Default Border Color Component.
    /// </summary>
    public const float D3D10DefaultBorderColorComponent = 0.0f;

    /// <summary>
    /// D3D10 Default Depth Bias.
    /// </summary>
    public const uint D3D10DefaultDepthBias = 0;

    /// <summary>
    /// D3D10 Default Depth Bias Clamp.
    /// </summary>
    public const float D3D10DefaultDepthBiasClamp = 0.0f;

    /// <summary>
    /// D3D10 Default Max Anisotropy.
    /// </summary>
    public const uint D3D10DefaultMaxAnisotropy = 16;

    /// <summary>
    /// D3D10 Default Mip Lod Bias.
    /// </summary>
    public const float D3D10DefaultMipLodBias = 0.0f;

    /// <summary>
    /// D3D10 Default Render Target Array Index.
    /// </summary>
    public const uint D3D10DefaultRenderTargetArrayIndex = 0;

    /// <summary>
    /// D3D10 Default Sample Mask.
    /// </summary>
    public const uint D3D10DefaultSampleMask = 0xffffffff;

    /// <summary>
    /// D3D10 Default Scissor End X.
    /// </summary>
    public const uint D3D10DefaultScissorEndX = 0;

    /// <summary>
    /// D3D10 Default Scissor End Y.
    /// </summary>
    public const uint D3D10DefaultScissorEndY = 0;

    /// <summary>
    /// D3D10 Default Scissor Start X.
    /// </summary>
    public const uint D3D10DefaultScissorStartX = 0;

    /// <summary>
    /// D3D10 Default Scissor Start Y.
    /// </summary>
    public const uint D3D10DefaultScissorStartY = 0;

    /// <summary>
    /// D3D10 Default Slope Scaled Depth Bias.
    /// </summary>
    public const float D3D10DefaultSlopeScaledDepthBias = 0.0f;

    /// <summary>
    /// D3D10 Default Stencil Read Mask.
    /// </summary>
    public const uint D3D10DefaultStencilReadMask = 0xff;

    /// <summary>
    /// D3D10 Default Stencil Reference.
    /// </summary>
    public const uint D3D10DefaultStencilReference = 0;

    /// <summary>
    /// D3D10 Default Stencil Write Mask.
    /// </summary>
    public const uint D3D10DefaultStencilWriteMask = 0xff;

    /// <summary>
    /// D3D10 Default Viewport And Scissor Rect Index.
    /// </summary>
    public const uint D3D10DefaultViewportAndScissorRectIndex = 0;

    /// <summary>
    /// D3D10 Default Viewport Height.
    /// </summary>
    public const uint D3D10DefaultViewportHeight = 0;

    /// <summary>
    /// D3D10 Default Viewport Max Depth.
    /// </summary>
    public const float D3D10DefaultViewportMaxDepth = 0.0f;

    /// <summary>
    /// D3D10 Default Viewport Min Depth.
    /// </summary>
    public const float D3D10DefaultViewportMinDepth = 0.0f;

    /// <summary>
    /// D3D10 Default Viewport Top Left X.
    /// </summary>
    public const uint D3D10DefaultViewportTopLeftX = 0;

    /// <summary>
    /// D3D10 Default Viewport Top Left Y.
    /// </summary>
    public const uint D3D10DefaultViewportTopLeftY = 0;

    /// <summary>
    /// D3D10 Default Viewport Width.
    /// </summary>
    public const uint D3D10DefaultViewportWidth = 0;

    /// <summary>
    /// D3D10 Float16 Fused Tolerance In Ulp.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", MessageId = "float", Justification = "Reviewed")]
    public const float D3D10Float16FusedToleranceInUlp = 0.6f;

    /// <summary>
    /// D3D10 Float32 Max.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", MessageId = "float32", Justification = "Reviewed")]
    public const float D3D10Float32Max = 3.402823466e+38f;

    /// <summary>
    /// D3D10 Float32 To Integer Tolerance In Ulp.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", MessageId = "float32", Justification = "Reviewed")]
    public const float D3D10Float32ToIntegerToleranceInUlp = 0.6f;

    /// <summary>
    /// D3D10 Float To Srgb Exponent Denominator.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", MessageId = "float", Justification = "Reviewed")]
    public const float D3D10FloatToSrgbExponentDenominator = 2.4f;

    /// <summary>
    /// D3D10 Float To Srgb Exponent Numerator.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", MessageId = "float", Justification = "Reviewed")]
    public const float D3D10FloatToSrgbExponentNumerator = 1.0f;

    /// <summary>
    /// D3D10 Float To Srgb Offset.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", MessageId = "float", Justification = "Reviewed")]
    public const float D3D10FloatToSrgbOffset = 0.055f;

    /// <summary>
    /// D3D10 Float To Srgb Scale 1.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", MessageId = "float", Justification = "Reviewed")]
    public const float D3D10FloatToSrgbScale1 = 12.92f;

    /// <summary>
    /// D3D10 Float To Srgb Scale 2.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", MessageId = "float", Justification = "Reviewed")]
    public const float D3D10FloatToSrgbScale2 = 1.055f;

    /// <summary>
    /// D3D10 Float To Srgb Threshold.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", MessageId = "float", Justification = "Reviewed")]
    public const float D3D10FloatToSrgbThreshold = 0.0031308f;

    /// <summary>
    /// D3D10 Float To Int Instruction Max Input.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", MessageId = "float", Justification = "Reviewed")]
    public const float D3D10FloatToIntInstructionMaxInput = 2147483647.999f;

    /// <summary>
    /// D3D10 Float To Int Instruction Min Input.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", MessageId = "float", Justification = "Reviewed")]
    public const float D3D10FloatToIntInstructionMinInput = -2147483648.999f;

    /// <summary>
    /// D3D10 Float To UInt Instruction Max Input.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", MessageId = "float", Justification = "Reviewed")]
    public const float D3D10FloatToUIntInstructionMaxInput = 4294967295.999f;

    /// <summary>
    /// D3D10 Float To UInt Instruction Min Input.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", MessageId = "float", Justification = "Reviewed")]
    public const float D3D10FloatToUIntInstructionMinInput = 0.0f;

    /// <summary>
    /// D3D10 Geometry Shader Input Prim Const Register Components.
    /// </summary>
    public const uint D3D10GeometryShaderInputPrimConstRegisterComponents = 1;

    /// <summary>
    /// D3D10 Geometry Shader Input Prim Const Register Component Bit Count.
    /// </summary>
    public const uint D3D10GeometryShaderInputPrimConstRegisterComponentBitCount = 32;

    /// <summary>
    /// D3D10 Geometry Shader Input Prim Const Register Count.
    /// </summary>
    public const uint D3D10GeometryShaderInputPrimConstRegisterCount = 1;

    /// <summary>
    /// D3D10 Geometry Shader Input Prim Const Register Reads Per Instance.
    /// </summary>
    public const uint D3D10GeometryShaderInputPrimConstRegisterReadsPerInstance = 2;

    /// <summary>
    /// D3D10 Geometry Shader Input Prim Const Register Read Ports.
    /// </summary>
    public const uint D3D10GeometryShaderInputPrimConstRegisterReadPorts = 1;

    /// <summary>
    /// D3D10 Geometry Shader Input Register Components.
    /// </summary>
    public const uint D3D10GeometryShaderInputRegisterComponents = 4;

    /// <summary>
    /// D3D10 Geometry Shader Input Register Component Bit Count.
    /// </summary>
    public const uint D3D10GeometryShaderInputRegisterComponentBitCount = 32;

    /// <summary>
    /// D3D10 Geometry Shader Input Register Count.
    /// </summary>
    public const uint D3D10GeometryShaderInputRegisterCount = 16;

    /// <summary>
    /// D3D10 Geometry Shader Input Register Reads Per Instance.
    /// </summary>
    public const uint D3D10GeometryShaderInputRegisterReadsPerInstance = 2;

    /// <summary>
    /// D3D10 Geometry Shader Input Register Read Ports.
    /// </summary>
    public const uint D3D10GeometryShaderInputRegisterReadPorts = 1;

    /// <summary>
    /// D3D10 Geometry Shader Input Register Vertices.
    /// </summary>
    public const uint D3D10GeometryShaderInputRegisterVertices = 6;

    /// <summary>
    /// D3D10 Geometry Shader Output Elements.
    /// </summary>
    public const uint D3D10GeometryShaderOutputElements = 32;

    /// <summary>
    /// D3D10 Geometry Shader Output Register Components.
    /// </summary>
    public const uint D3D10GeometryShaderOutputRegisterComponents = 4;

    /// <summary>
    /// D3D10 Geometry Shader Output Register Component Bit Count.
    /// </summary>
    public const uint D3D10GeometryShaderOutputRegisterComponentBitCount = 32;

    /// <summary>
    /// D3D10 Geometry Shader Output Register Count.
    /// </summary>
    public const uint D3D10GeometryShaderOutputRegisterCount = 32;

    /// <summary>
    /// D3D10 Input Assembler Default Index Buffer Offset In Bytes.
    /// </summary>
    public const uint D3D10InputAssemblerDefaultIndexBufferOffsetInBytes = 0;

    /// <summary>
    /// D3D10 Input Assembler Default Primitive Topology.
    /// </summary>
    public const uint D3D10InputAssemblerDefaultPrimitiveTopology = 0;

    /// <summary>
    /// D3D10 Input Assembler Default Vertex Buffer Offset In Bytes.
    /// </summary>
    public const uint D3D10InputAssemblerDefaultVertexBufferOffsetInBytes = 0;

    /// <summary>
    /// D3D10 Input Assembler Index Input Resource Slot Count.
    /// </summary>
    public const uint D3D10InputAssemblerIndexInputResourceSlotCount = 1;

    /// <summary>
    /// D3D10 Input Assembler Instance Id Bit Count.
    /// </summary>
    public const uint D3D10InputAssemblerInstanceIdBitCount = 32;

    /// <summary>
    /// D3D10 Input Assembler Integer Arithmetic Bit Count.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", MessageId = "integer", Justification = "Reviewed")]
    public const uint D3D10InputAssemblerIntegerArithmeticBitCount = 32;

    /// <summary>
    /// D3D10 Input Assembler Primitive Id Bit Count.
    /// </summary>
    public const uint D3D10InputAssemblerPrimitiveIdBitCount = 32;

    /// <summary>
    /// D3D10 Input Assembler Vertex Id Bit Count.
    /// </summary>
    public const uint D3D10InputAssemblerVertexIdBitCount = 32;

    /// <summary>
    /// D3D10 Input Assembler Vertex Input Resource Slot Count.
    /// </summary>
    public const uint D3D10InputAssemblerVertexInputResourceSlotCount = 16;

    /// <summary>
    /// D3D10 Input Assembler Vertex Input Structure Elements Components.
    /// </summary>
    public const uint D3D10InputAssemblerVertexInputStructureElementsComponents = 64;

    /// <summary>
    /// D3D10 Input Assembler Vertex Input Structure Element Count.
    /// </summary>
    public const uint D3D10InputAssemblerVertexInputStructureElementCount = 16;

    /// <summary>
    /// D3D10 Integer Divide By Zero Quotient.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", MessageId = "integer", Justification = "Reviewed")]
    public const uint D3D10IntegerDivideByZeroQuotient = 0xffffffff;

    /// <summary>
    /// D3D10 Integer Divide By Zero Remainder.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", MessageId = "integer", Justification = "Reviewed")]
    public const uint D3D10IntegerDivideByZeroRemainder = 0xffffffff;

    /// <summary>
    /// D3D10 Linear Gamma.
    /// </summary>
    public const float D3D10LinearGamma = 1.0f;

    /// <summary>
    /// D3D10 Max Border Color Component.
    /// </summary>
    public const float D3D10MaxBorderColorComponent = 1.0f;

    /// <summary>
    /// D3D10 Max Depth.
    /// </summary>
    public const float D3D10MaxDepth = 1.0f;

    /// <summary>
    /// D3D10 Max Anisotropy.
    /// </summary>
    public const uint D3D10MaxAnisotropy = 16;

    /// <summary>
    /// D3D10 Max Multisample Sample Count.
    /// </summary>
    public const uint D3D10MaxMultisampleSampleCount = 32;

    /// <summary>
    /// D3D10 Max Position Value.
    /// </summary>
    public const float D3D10MaxPositionValue = 3.402823466e+34f;

    /// <summary>
    /// D3D10 Max Texture Dimension 2 To Exp.
    /// </summary>
    public const uint D3D10MaxTextureDimension2ToExp = 17;

    /// <summary>
    /// D3D10 Min Border Color Component.
    /// </summary>
    public const float D3D10MinBorderColorComponent = 0.0f;

    /// <summary>
    /// D3D10 Min Depth.
    /// </summary>
    public const float D3D10MinDepth = 0.0f;

    /// <summary>
    /// D3D10 Min Max Anisotropy.
    /// </summary>
    public const uint D3D10MinMaxAnisotropy = 0;

    /// <summary>
    /// D3D10 Mip Lod Bias Max.
    /// </summary>
    public const float D3D10MipLodBiasMax = 15.99f;

    /// <summary>
    /// D3D10 Mip Lod Bias Min.
    /// </summary>
    public const float D3D10MipLodBiasMin = -16.0f;

    /// <summary>
    /// D3D10 Mip Lod Fractional Bit Count.
    /// </summary>
    public const uint D3D10MipLodFractionalBitCount = 6;

    /// <summary>
    /// D3D10 Mip Lod Range Bit Count.
    /// </summary>
    public const uint D3D10MipLodRangeBitCount = 8;

    /// <summary>
    /// D3D10 Multisample Antialias Line Width.
    /// </summary>
    public const float D3D10MultisampleAntialiasLineWidth = 1.4f;

    /// <summary>
    /// D3D10 Non Sample Fetch Out Of Range Access Result.
    /// </summary>
    public const uint D3D10NonSampleFetchOutOfRangeAccessResult = 0;

    /// <summary>
    /// D3D10 Pixel Address Range Bit Count.
    /// </summary>
    public const uint D3D10PixelAddressRangeBitCount = 13;

    /// <summary>
    /// D3D10 Pre Scissor Pixel Address Range Bit Count.
    /// </summary>
    public const uint D3D10PreScissorPixelAddressRangeBitCount = 15;

    /// <summary>
    /// D3D10 Pixel Shader Front Facing Default Value.
    /// </summary>
    public const uint D3D10PixelShaderFrontFacingDefaultValue = 0xFFFFFFFF;

    /// <summary>
    /// D3D10 Pixel Shader Front Facing False Value.
    /// </summary>
    public const uint D3D10PixelShaderFrontFacingFalseValue = 0x00000000;

    /// <summary>
    /// D3D10 Pixel Shader Front Facing True Value.
    /// </summary>
    public const uint D3D10PixelShaderFrontFacingTrueValue = 0xFFFFFFFF;

    /// <summary>
    /// D3D10 Pixel Shader Register Components.
    /// </summary>
    public const uint D3D10PixelShaderRegisterComponents = 4;

    /// <summary>
    /// D3D10 Pixel Shader Input Register Component Bit Count.
    /// </summary>
    public const uint D3D10PixelShaderInputRegisterComponentBitCount = 32;

    /// <summary>
    /// D3D10 Pixel Shader Input Register Count.
    /// </summary>
    public const uint D3D10PixelShaderInputRegisterCount = 32;

    /// <summary>
    /// D3D10 Pixel Shader Input Register Reads Per Instance.
    /// </summary>
    public const uint D3D10PixelShaderInputRegisterReadsPerInstance = 2;

    /// <summary>
    /// D3D10 Pixel Shader Input Register Read Ports.
    /// </summary>
    public const uint D3D10PixelShaderInputRegisterReadPorts = 1;

    /// <summary>
    /// D3D10 Pixel Shader Legacy Pixel Center Fractional Component.
    /// </summary>
    public const float D3D10PixelShaderLegacyPixelCenterFractionalComponent = 0.0f;

    /// <summary>
    /// D3D10 Pixel Shader Output Depth Register Components.
    /// </summary>
    public const uint D3D10PixelShaderOutputDepthRegisterComponents = 1;

    /// <summary>
    /// D3D10 Pixel Shader Output Depth Register Component Bit Count.
    /// </summary>
    public const uint D3D10PixelShaderOutputDepthRegisterComponentBitCount = 32;

    /// <summary>
    /// D3D10 Pixel Shader Output Depth Register Count.
    /// </summary>
    public const uint D3D10PixelShaderOutputDepthRegisterCount = 1;

    /// <summary>
    /// D3D10 Pixel Shader Output Register Components.
    /// </summary>
    public const uint D3D10PixelShaderOutputRegisterComponents = 4;

    /// <summary>
    /// D3D10 Pixel Shader Output Register Component Bit Count.
    /// </summary>
    public const uint D3D10PixelShaderOutputRegisterComponentBitCount = 32;

    /// <summary>
    /// D3D10 Pixel Shader Output Register Count.
    /// </summary>
    public const uint D3D10PixelShaderOutputRegisterCount = 8;

    /// <summary>
    /// D3D10 Pixel Shader Pixel Center Fractional Component.
    /// </summary>
    public const float D3D10PixelShaderPixelCenterFractionalComponent = 0.5f;

    /// <summary>
    /// D3D10 Req Blend Object Count Per Context.
    /// </summary>
    public const uint D3D10ReqBlendObjectCountPerContext = 4096;

    /// <summary>
    /// D3D10 Req Buffer Resource Texel Count 2 To Exp.
    /// </summary>
    public const uint D3D10ReqBufferResourceTexelCount2ToExp = 27;

    /// <summary>
    /// D3D10 Req Constant Buffer Element Count.
    /// </summary>
    public const uint D3D10ReqConstantBufferElementCount = 4096;

    /// <summary>
    /// D3D10 Req Depth Stencil Object Count Per Context.
    /// </summary>
    public const uint D3D10ReqDepthStencilObjectCountPerContext = 4096;

    /// <summary>
    /// D3D10 Req Draw Indexed Index Count 2 To Exp.
    /// </summary>
    public const uint D3D10ReqDrawIndexedIndexCount2ToExp = 32;

    /// <summary>
    /// D3D10 Req Draw Vertex Count 2 To Exp.
    /// </summary>
    public const uint D3D10ReqDrawVertexCount2ToExp = 32;

    /// <summary>
    /// D3D10 Req Filtering Hardware Addressable Resource Dimension.
    /// </summary>
    public const uint D3D10ReqFilteringHardwareAddressableResourceDimension = 8192;

    /// <summary>
    /// D3D10 Req Geometry Shader Invocation 32-Bit Output Component Limit.
    /// </summary>
    public const uint D3D10ReqGeometryShaderInvocation32BitOutputComponentLimit = 1024;

    /// <summary>
    /// D3D10 Req Immediate Constant Buffer Element Count.
    /// </summary>
    public const uint D3D10ReqImmediateConstantBufferElementCount = 4096;

    /// <summary>
    /// D3D10 Req Max Anisotropy.
    /// </summary>
    public const uint D3D10ReqMaxAnisotropy = 16;

    /// <summary>
    /// D3D10 Req Mip Levels.
    /// </summary>
    public const uint D3D10ReqMipLevels = 14;

    /// <summary>
    /// D3D10 Req Multi Element Structure Size In Bytes.
    /// </summary>
    public const uint D3D10ReqMultiElementStructureSizeInBytes = 2048;

    /// <summary>
    /// D3D10 Req Rasterizer Object Count Per Context.
    /// </summary>
    public const uint D3D10ReqRasterizerObjectCountPerContext = 4096;

    /// <summary>
    /// D3D10 Req Render To Buffer Window Width.
    /// </summary>
    public const uint D3D10ReqRenderToBufferWindowWidth = 8192;

    /// <summary>
    /// D3D10 Req Resource Size In Megabytes.
    /// </summary>
    public const uint D3D10ReqResourceSizeInMegabytes = 128;

    /// <summary>
    /// D3D10 Req Resource View Count Per Context 2 To Exp.
    /// </summary>
    public const uint D3D10ReqResourceViewCountPerContext2ToExp = 20;

    /// <summary>
    /// D3D10 Req Sampler Object Count Per Context.
    /// </summary>
    public const uint D3D10ReqSamplerObjectCountPerContext = 4096;

    /// <summary>
    /// D3D10 Req Texture 1D Array Axis Dimension.
    /// </summary>
    public const uint D3D10ReqTexture1DArrayAxisDimension = 512;

    /// <summary>
    /// D3D10 Req Texture 1D Dimension.
    /// </summary>
    public const uint D3D10ReqTexture1DDimension = 8192;

    /// <summary>
    /// D3D10 Req Texture 2D Array Axis Dimension.
    /// </summary>
    public const uint D3D10ReqTexture2DArrayAxisDimension = 512;

    /// <summary>
    /// D3D10 Req Texture 2D Dimension.
    /// </summary>
    public const uint D3D10ReqTexture2DDimension = 8192;

    /// <summary>
    /// D3D10 Req Texture 3D Dimension.
    /// </summary>
    public const uint D3D10ReqTexture3DDimension = 2048;

    /// <summary>
    /// D3D10 Req Texture Cube Dimension.
    /// </summary>
    public const uint D3D10ReqTextureCubeDimension = 8192;

    /// <summary>
    /// D3D10 Resinfo Instruction Missing Component Retval.
    /// </summary>
    public const uint D3D10ResinfoInstructionMissingComponentRetval = 0;

    /// <summary>
    /// D3D10 Shift Instruction Pad Value.
    /// </summary>
    public const uint D3D10ShiftInstructionPadValue = 0;

    /// <summary>
    /// D3D10 Shift Instruction Shift Value Bit Count.
    /// </summary>
    public const uint D3D10ShiftInstructionShiftValueBitCount = 5;

    /// <summary>
    /// D3D10 Simultaneous Render Target Count.
    /// </summary>
    public const uint D3D10SimultaneousRenderTargetCount = 8;

    /// <summary>
    /// D3D10 Stream Output Buffer Max Stride In Bytes.
    /// </summary>
    public const uint D3D10StreamOutputBufferMaxStrideInBytes = 2048;

    /// <summary>
    /// D3D10 Stream Output Buffer Max Write Window In Bytes.
    /// </summary>
    public const uint D3D10StreamOutputBufferMaxWriteWindowInBytes = 256;

    /// <summary>
    /// D3D10 Stream Output Buffer Slot Count.
    /// </summary>
    public const uint D3D10StreamOutputBufferSlotCount = 4;

    /// <summary>
    /// D3D10 Stream Output Ddi Register Index Denoting Gap.
    /// </summary>
    public const uint D3D10StreamOutputDdiRegisterIndexDenotingGap = 0xffffffff;

    /// <summary>
    /// D3D10 Stream Output Multiple Buffer Elements Per Buffer.
    /// </summary>
    public const uint D3D10StreamOutputMultipleBufferElementsPerBuffer = 1;

    /// <summary>
    /// D3D10 Stream Output Single Buffer Component Limit.
    /// </summary>
    public const uint D3D10StreamOutputSingleBufferComponentLimit = 64;

    /// <summary>
    /// D3D10 Srgb Gamma.
    /// </summary>
    public const float D3D10SrgbGamma = 2.2f;

    /// <summary>
    /// D3D10 Srgb To Float Denominator 1.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", MessageId = "float", Justification = "Reviewed")]
    public const float D3D10SrgbToFloatDenominator1 = 12.92f;

    /// <summary>
    /// D3D10 Srgb To Float Denominator 2.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", MessageId = "float", Justification = "Reviewed")]
    public const float D3D10SrgbToFloatDenominator2 = 1.055f;

    /// <summary>
    /// D3D10 Srgb To Float Exponent.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", MessageId = "float", Justification = "Reviewed")]
    public const float D3D10SrgbToFloatExponent = 2.4f;

    /// <summary>
    /// D3D10 Srgb To Float Offset.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", MessageId = "float", Justification = "Reviewed")]
    public const float D3D10SrgbToFloatOffset = 0.055f;

    /// <summary>
    /// D3D10 Srgb To Float Threshold.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", MessageId = "float", Justification = "Reviewed")]
    public const float D3D10SrgbToFloatThreshold = 0.04045f;

    /// <summary>
    /// D3D10 Srgb To Float Tolerance In Ulp.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", MessageId = "float", Justification = "Reviewed")]
    public const float D3D10SrgbToFloatToleranceInUlp = 0.5f;

    /// <summary>
    /// D3D10 Standard Component Bit Count.
    /// </summary>
    public const uint D3D10StandardComponentBitCount = 32;

    /// <summary>
    /// D3D10 Standard Component Bit Count Doubled.
    /// </summary>
    public const uint D3D10StandardComponentBitCountDoubled = 64;

    /// <summary>
    /// D3D10 Standard Maximum Element Alignment Byte Multiple.
    /// </summary>
    public const uint D3D10StandardMaximumElementAlignmentByteMultiple = 4;

    /// <summary>
    /// D3D10 Standard Pixel Component Count.
    /// </summary>
    public const uint D3D10StandardPixelComponentCount = 128;

    /// <summary>
    /// D3D10 Standard Pixel Element Count.
    /// </summary>
    public const uint D3D10StandardPixelElementCount = 32;

    /// <summary>
    /// D3D10 Standard Vector Size.
    /// </summary>
    public const uint D3D10StandardVectorSize = 4;

    /// <summary>
    /// D3D10 Standard Vertex Element Count.
    /// </summary>
    public const uint D3D10StandardVertexElementCount = 16;

    /// <summary>
    /// D3D10 Standard Vertex Total Component Count.
    /// </summary>
    public const uint D3D10StandardVertexTotalComponentCount = 64;

    /// <summary>
    /// D3D10 Subpixel Fractional Bit Count.
    /// </summary>
    public const uint D3D10SubpixelFractionalBitCount = 8;

    /// <summary>
    /// D3D10 Subtexel Fractional Bit Count.
    /// </summary>
    public const uint D3D10SubtexelFractionalBitCount = 6;

    /// <summary>
    /// D3D10 Texel Address Range Bit Count.
    /// </summary>
    public const uint D3D10TexelAddressRangeBitCount = 18;

    /// <summary>
    /// D3D10 Unbound Memory Access Result.
    /// </summary>
    public const uint D3D10UnboundMemoryAccessResult = 0;

    /// <summary>
    /// D3D10 Viewport And Scissor Rect Max Index.
    /// </summary>
    public const uint D3D10ViewportAndScissorRectMaxIndex = 15;

    /// <summary>
    /// D3D10 Viewport And Scissor Rect Object Count Per Pipeline.
    /// </summary>
    public const uint D3D10ViewportAndScissorRectObjectCountPerPipeline = 16;

    /// <summary>
    /// D3D10 Viewport Bounds Max.
    /// </summary>
    public const int D3D10ViewportBoundsMax = 16383;

    /// <summary>
    /// D3D10 Viewport Bounds Min.
    /// </summary>
    public const int D3D10ViewportBoundsMin = -16384;

    /// <summary>
    /// D3D10 Vertex Shader Input Register Components.
    /// </summary>
    public const uint D3D10VertexShaderInputRegisterComponents = 4;

    /// <summary>
    /// D3D10 Vertex Shader Input Register Component Bit Count.
    /// </summary>
    public const uint D3D10VertexShaderInputRegisterComponentBitCount = 32;

    /// <summary>
    /// D3D10 Vertex Shader Input Register Count.
    /// </summary>
    public const uint D3D10VertexShaderInputRegisterCount = 16;

    /// <summary>
    /// D3D10 Vertex Shader Input Register Reads Per Instance.
    /// </summary>
    public const uint D3D10VertexShaderInputRegisterReadsPerInstance = 2;

    /// <summary>
    /// D3D10 Vertex Shader Input Register Read Ports.
    /// </summary>
    public const uint D3D10VertexShaderInputRegisterReadPorts = 1;

    /// <summary>
    /// D3D10 Vertex Shader Output Register Components.
    /// </summary>
    public const uint D3D10VertexShaderOutputRegisterComponents = 4;

    /// <summary>
    /// D3D10 Vertex Shader Output Register Component Bit Count.
    /// </summary>
    public const uint D3D10VertexShaderOutputRegisterComponentBitCount = 32;

    /// <summary>
    /// D3D10 Vertex Shader Output Register Count.
    /// </summary>
    public const uint D3D10VertexShaderOutputRegisterCount = 16;

    /// <summary>
    /// D3D10 WHQL Context Count For Resource Limit.
    /// </summary>
    public const uint D3D10WhqlContextCountForResourceLimit = 10;

    /// <summary>
    /// D3D10 WHQL Draw Indexed Index Count 2 To Exp.
    /// </summary>
    public const uint D3D10WhqlDrawIndexedIndexCount2ToExp = 25;

    /// <summary>
    /// D3D10 WHQL Draw Vertex Count 2 To Exp.
    /// </summary>
    public const uint D3D10WhqlDrawVertexCount2ToExp = 25;

    /// <summary>
    /// D3D10.1 Default Sample Mask.
    /// </summary>
    public const uint D3D101DefaultSampleMask = 0xffffffff;

    /// <summary>
    /// D3D10.1 Float16 Fused Tolerance In Ulp.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", MessageId = "float", Justification = "Reviewed")]
    public const float D3D101Float16FusedToleranceInUlp = 0.6f;

    /// <summary>
    /// D3D10.1 Float32 To Integer Tolerance In Ulp.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", MessageId = "float32", Justification = "Reviewed")]
    public const float D3D101Float32ToIntegerToleranceInUlp = 0.6f;

    /// <summary>
    /// D3D10.1 Geometry Shader Input Register Count.
    /// </summary>
    public const uint D3D101GeometryShaderInputRegisterCount = 32;

    /// <summary>
    /// D3D10.1 Input Assembler Vertex Input Resource Slot Count.
    /// </summary>
    public const uint D3D101InputAssemblerVertexInputResourceSlotCount = 32;

    /// <summary>
    /// D3D10.1 Input Assembler Vertex Input Structure Elements Components.
    /// </summary>
    public const uint D3D101InputAssemblerVertexInputStructureElementsComponents = 128;

    /// <summary>
    /// D3D10.1 Input Assembler Vertex Input Structure Element Count.
    /// </summary>
    public const uint D3D101InputAssemblerVertexInputStructureElementCount = 32;

    /// <summary>
    /// D3D10.1 Pixel Shader Output Mask Register Components.
    /// </summary>
    public const uint D3D101PixelShaderOutputMaskRegisterComponents = 1;

    /// <summary>
    /// D3D10.1 Pixel Shader Output Mask Register Component BitCount.
    /// </summary>
    public const uint D3D101PixelShaderOutputMaskRegisterComponentBitCount = 32;

    /// <summary>
    /// D3D10.1 Pixel Shader Output Mask Register Count.
    /// </summary>
    public const uint D3D101PixelShaderOutputMaskRegisterCount = 1;

    /// <summary>
    /// D3D10.1 Stream Output Buffer Max Stride In Bytes.
    /// </summary>
    public const uint D3D101StreamOutputBufferMaxStrideInBytes = 2048;

    /// <summary>
    /// D3D10.1 Stream Output Buffer Max Write Window In Bytes.
    /// </summary>
    public const uint D3D101StreamOutputBufferMaxWriteWindowInBytes = 256;

    /// <summary>
    /// D3D10.1 Stream Output Buffer Slot Count.
    /// </summary>
    public const uint D3D101StreamOutputBufferSlotCount = 4;

    /// <summary>
    /// D3D10.1 Stream Output Multiple Buffer Elements Per Buffer.
    /// </summary>
    public const uint D3D101StreamOutputMultipleBufferElementsPerBuffer = 1;

    /// <summary>
    /// D3D10.1 Stream Output Single Buffer Component Limit.
    /// </summary>
    public const uint D3D101StreamOutputSingleBufferComponentLimit = 64;

    /// <summary>
    /// D3D10.1 Standard Vertex Element Count.
    /// </summary>
    public const uint D3D101StandardVertexElementCount = 32;

    /// <summary>
    /// D3D10.1 Subpixel Fractional Bit Count.
    /// </summary>
    public const uint D3D101SubpixelFractionalBitCount = 8;

    /// <summary>
    /// D3D10.1 Vertex Shader Input Register Count.
    /// </summary>
    public const uint D3D101VertexShaderInputRegisterCount = 32;

    /// <summary>
    /// D3D10.1 Vertex Shader Output Register Count.
    /// </summary>
    public const uint D3D101VertexShaderOutputRegisterCount = 32;

    /// <summary>
    /// Feature Level 9.1 Req Texture 1D Dimension.
    /// </summary>
    public const uint FeatureLevel91ReqTexture1DDimension = 2048;

    /// <summary>
    /// Feature Level 9.3 Req Texture 1D Dimension.
    /// </summary>
    public const uint FeatureLevel93ReqTexture1DDimension = 4096;

    /// <summary>
    /// Feature Level 9.1 Req Texture 2D Dimension.
    /// </summary>
    public const uint FeatureLevel91ReqTexture2DDimension = 2048;

    /// <summary>
    /// Feature Level 9.3 Req Texture 2D Dimension.
    /// </summary>
    public const uint FeatureLevel93ReqTexture2DDimension = 4096;

    /// <summary>
    /// Feature Level 9.1 Req Texture Cube Dimension.
    /// </summary>
    public const uint FeatureLevel91ReqTextureCubeDimension = 512;

    /// <summary>
    /// Feature Level 9.3 Req Texture Cube Dimension.
    /// </summary>
    public const uint FeatureLevel93ReqTextureCubeDimension = 4096;

    /// <summary>
    /// Feature Level 9.1 Req Texture 3D Dimension.
    /// </summary>
    public const uint FeatureLevel91ReqTexture3DDimension = 256;

    /// <summary>
    /// Feature Level 9.1 Default Max Anisotropy.
    /// </summary>
    public const uint FeatureLevel91DefaultMaxAnisotropy = 2;

    /// <summary>
    /// Feature Level 9.1 Input Assembler Primitive Max Count.
    /// </summary>
    public const uint FeatureLevel91InputAssemblerPrimitiveMaxCount = 65535;

    /// <summary>
    /// Feature Level 9.2 Input Assembler Primitive Max Count.
    /// </summary>
    public const uint FeatureLevel92InputAssemblerPrimitiveMaxCount = 1048575;

    /// <summary>
    /// Feature Level 9.1 Simultaneous Render Target Count.
    /// </summary>
    public const uint FeatureLevel91SimultaneousRenderTargetCount = 1;

    /// <summary>
    /// Feature Level 9.3 Simultaneous Render Target Count.
    /// </summary>
    public const uint FeatureLevel93SimultaneousRenderTargetCount = 4;

    /// <summary>
    /// Feature Level 9.1 Max Texture Repeat.
    /// </summary>
    public const uint FeatureLevel91MaxTextureRepeat = 128;

    /// <summary>
    /// Feature Level 9.2 Max Texture Repeat.
    /// </summary>
    public const uint FeatureLevel92MaxTextureRepeat = 2048;

    /// <summary>
    /// Feature Level 9.3 Max Texture Repeat.
    /// </summary>
    public const uint FeatureLevel93MaxTextureRepeat = 8192;

    /// <summary>
    /// Index Strip Cut Value 16-Bit.
    /// </summary>
    public const uint IndexStripCutValue16Bit = 0xffff;

    /// <summary>
    /// Index Strip Cut Value 32-Bit.
    /// </summary>
    public const uint IndexStripCutValue32Bit = 0xffffffff;

    /// <summary>
    /// Index Strip Cut Value 8-Bit.
    /// </summary>
    public const uint IndexStripCutValue8Bit = 0xff;

    /// <summary>
    /// Array Axis Address Range Bit Count.
    /// </summary>
    public const uint ArrayAxisAddressRangeBitCount = 9;

    /// <summary>
    /// Clip Or Cull Distance Cull.
    /// </summary>
    public const uint ClipOrCullDistanceCull = 8;

    /// <summary>
    /// Clip Or Cull Distance Element Count.
    /// </summary>
    public const uint ClipOrCullDistanceElementCount = 2;

    /// <summary>
    /// Compute Shader Constant Buffer Api Slot Count.
    /// </summary>
    public const uint ComputeShaderConstantBufferApiSlotCount = 14;

    /// <summary>
    /// Compute Shader Constant Buffer Components.
    /// </summary>
    public const uint ComputeShaderConstantBufferComponents = 4;

    /// <summary>
    /// Compute Shader Constant Buffer Component Bit Count.
    /// </summary>
    public const uint ComputeShaderConstantBufferComponentBitCount = 32;

    /// <summary>
    /// Compute Shader Constant Buffer Hardware Slot Count.
    /// </summary>
    public const uint ComputeShaderConstantBufferHardwareSlotCount = 15;

    /// <summary>
    /// Compute Shader Constant Buffer Partial Update Extents Byte Alignment.
    /// </summary>
    public const uint ComputeShaderConstantBufferPartialUpdateExtentsByteAlignment = 16;

    /// <summary>
    /// Compute Shader Constant Buffer Register Components.
    /// </summary>
    public const uint ComputeShaderConstantBufferRegisterComponents = 4;

    /// <summary>
    /// Compute Shader Constant Buffer Register Count.
    /// </summary>
    public const uint ComputeShaderConstantBufferRegisterCount = 15;

    /// <summary>
    /// Compute Shader Constant Buffer Register Reads Per Instance.
    /// </summary>
    public const uint ComputeShaderConstantBufferRegisterReadsPerInstance = 1;

    /// <summary>
    /// Compute Shader Constant Buffer Register Read Ports.
    /// </summary>
    public const uint ComputeShaderConstantBufferRegisterReadPorts = 1;

    /// <summary>
    /// Compute Shader Flow Control Nesting Limit.
    /// </summary>
    public const uint ComputeShaderFlowControlNestingLimit = 64;

    /// <summary>
    /// Compute Shader Immediate Constant Buffer Register Components.
    /// </summary>
    public const uint ComputeShaderImmediateConstantBufferRegisterComponents = 4;

    /// <summary>
    /// Compute Shader Immediate Constant Buffer Register Count.
    /// </summary>
    public const uint ComputeShaderImmediateConstantBufferRegisterCount = 1;

    /// <summary>
    /// Compute Shader Immediate Constant Buffer Register Reads Per Instance.
    /// </summary>
    public const uint ComputeShaderImmediateConstantBufferRegisterReadsPerInstance = 1;

    /// <summary>
    /// Compute Shader Immediate Constant Buffer Register Read Ports.
    /// </summary>
    public const uint ComputeShaderImmediateConstantBufferRegisterReadPorts = 1;

    /// <summary>
    /// Compute Shader Immediate Value Component Bit Count.
    /// </summary>
    public const uint ComputeShaderImmediateValueComponentBitCount = 32;

    /// <summary>
    /// Compute Shader Input Resource Register Components.
    /// </summary>
    public const uint ComputeShaderInputResourceRegisterComponents = 1;

    /// <summary>
    /// Compute Shader Input Resource Register Count.
    /// </summary>
    public const uint ComputeShaderInputResourceRegisterCount = 128;

    /// <summary>
    /// Compute Shader Input Resource Register Reads Per Instance.
    /// </summary>
    public const uint ComputeShaderInputResourceRegisterReadsPerInstance = 1;

    /// <summary>
    /// Compute Shader Input Resource Register Read Ports.
    /// </summary>
    public const uint ComputeShaderInputResourceRegisterReadPorts = 1;

    /// <summary>
    /// Compute Shader Input Resource Slot Count.
    /// </summary>
    public const uint ComputeShaderInputResourceSlotCount = 128;

    /// <summary>
    /// Compute Shader Sampler Register Components.
    /// </summary>
    public const uint ComputeShaderSamplerRegisterComponents = 1;

    /// <summary>
    /// Compute Shader Sampler Register Count.
    /// </summary>
    public const uint ComputeShaderSamplerRegisterCount = 16;

    /// <summary>
    /// Compute Shader Sampler Register Reads Per Instance.
    /// </summary>
    public const uint ComputeShaderSamplerRegisterReadsPerInstance = 1;

    /// <summary>
    /// Compute Shader Sampler Register Read Ports.
    /// </summary>
    public const uint ComputeShaderSamplerRegisterReadPorts = 1;

    /// <summary>
    /// Compute Shader Sampler Slot Count.
    /// </summary>
    public const uint ComputeShaderSamplerSlotCount = 16;

    /// <summary>
    /// Compute Shader Subroutine Nesting Limit.
    /// </summary>
    public const uint ComputeShaderSubroutineNestingLimit = 32;

    /// <summary>
    /// Compute Shader Temp Register Components.
    /// </summary>
    public const uint ComputeShaderTempRegisterComponents = 4;

    /// <summary>
    /// Compute Shader Temp Register Component Bit Count.
    /// </summary>
    public const uint ComputeShaderTempRegisterComponentBitCount = 32;

    /// <summary>
    /// Compute Shader Temp Register Count.
    /// </summary>
    public const uint ComputeShaderTempRegisterCount = 4096;

    /// <summary>
    /// Compute Shader Temp Register Reads Per Instance.
    /// </summary>
    public const uint ComputeShaderTempRegisterReadsPerInstance = 3;

    /// <summary>
    /// Compute Shader Temp Register Read Ports.
    /// </summary>
    public const uint ComputeShaderTempRegisterReadPorts = 3;

    /// <summary>
    /// Compute Shader Tex Coord Range Reduction Max.
    /// </summary>
    public const int ComputeShaderTexCoordRangeReductionMax = 10;

    /// <summary>
    /// Compute Shader Tex Coord Range Reduction Min.
    /// </summary>
    public const int ComputeShaderTexCoordRangeReductionMin = -10;

    /// <summary>
    /// Compute Shader Texel Offset Max Negative.
    /// </summary>
    public const int ComputeShaderTexelOffsetMaxNegative = -8;

    /// <summary>
    /// Compute Shader Texel Offset Max Positive.
    /// </summary>
    public const int ComputeShaderTexelOffsetMaxPositive = 7;

    /// <summary>
    /// Compute Shader 4X Bucket 00 Max Bytes Tgsm Writable Per Thread.
    /// </summary>
    public const uint ComputeShader4XBucket00MaxBytesTgsmWritablePerThread = 256;

    /// <summary>
    /// Compute Shader 4X Bucket 00 Max Num Threads Per Group.
    /// </summary>
    public const uint ComputeShader4XBucket00MaxNumThreadsPerGroup = 64;

    /// <summary>
    /// Compute Shader 4X Bucket 01 Max Bytes Tgsm Writable Per Thread.
    /// </summary>
    public const uint ComputeShader4XBucket01MaxBytesTgsmWritablePerThread = 240;

    /// <summary>
    /// Compute Shader 4X Bucket 01 Max Num Threads Per Group.
    /// </summary>
    public const uint ComputeShader4XBucket01MaxNumThreadsPerGroup = 68;

    /// <summary>
    /// Compute Shader 4X Bucket 02 Max Bytes Tgsm Writable Per Thread.
    /// </summary>
    public const uint ComputeShader4XBucket02MaxBytesTgsmWritablePerThread = 224;

    /// <summary>
    /// Compute Shader 4X Bucket 02 Max Num Threads Per Group.
    /// </summary>
    public const uint ComputeShader4XBucket02MaxNumThreadsPerGroup = 72;

    /// <summary>
    /// Compute Shader 4X Bucket 03 Max Bytes Tgsm Writable Per Thread.
    /// </summary>
    public const uint ComputeShader4XBucket03MaxBytesTgsmWritablePerThread = 208;

    /// <summary>
    /// Compute Shader 4X Bucket 03 Max Num Threads Per Group.
    /// </summary>
    public const uint ComputeShader4XBucket03MaxNumThreadsPerGroup = 76;

    /// <summary>
    /// Compute Shader 4X Bucket 04 Max Bytes Tgsm Writable Per Thread.
    /// </summary>
    public const uint ComputeShader4XBucket04MaxBytesTgsmWritablePerThread = 192;

    /// <summary>
    /// Compute Shader 4X Bucket 04 Max Num Threads Per Group.
    /// </summary>
    public const uint ComputeShader4XBucket04MaxNumThreadsPerGroup = 84;

    /// <summary>
    /// Compute Shader 4X Bucket 05 Max Bytes Tgsm Writable Per Thread.
    /// </summary>
    public const uint ComputeShader4XBucket05MaxBytesTgsmWritablePerThread = 176;

    /// <summary>
    /// Compute Shader 4X Bucket 05 Max Num Threads Per Group.
    /// </summary>
    public const uint ComputeShader4XBucket05MaxNumThreadsPerGroup = 92;

    /// <summary>
    /// Compute Shader 4X Bucket 06 Max Bytes Tgsm Writable Per Thread.
    /// </summary>
    public const uint ComputeShader4XBucket06MaxBytesTgsmWritablePerThread = 160;

    /// <summary>
    /// Compute Shader 4X Bucket 06 Max Num Threads Per Group.
    /// </summary>
    public const uint ComputeShader4XBucket06MaxNumThreadsPerGroup = 100;

    /// <summary>
    /// Compute Shader 4X Bucket 07 Max Bytes Tgsm Writable Per Thread.
    /// </summary>
    public const uint ComputeShader4XBucket07MaxBytesTgsmWritablePerThread = 144;

    /// <summary>
    /// Compute Shader 4X Bucket 07 Max Num Threads Per Group.
    /// </summary>
    public const uint ComputeShader4XBucket07MaxNumThreadsPerGroup = 112;

    /// <summary>
    /// Compute Shader 4X Bucket 08 Max Bytes Tgsm Writable Per Thread.
    /// </summary>
    public const uint ComputeShader4XBucket08MaxBytesTgsmWritablePerThread = 128;

    /// <summary>
    /// Compute Shader 4X Bucket 08 Max Num Threads Per Group.
    /// </summary>
    public const uint ComputeShader4XBucket08MaxNumThreadsPerGroup = 128;

    /// <summary>
    /// Compute Shader 4X Bucket 09 Max Bytes Tgsm Writable Per Thread.
    /// </summary>
    public const uint ComputeShader4XBucket09MaxBytesTgsmWritablePerThread = 112;

    /// <summary>
    /// Compute Shader 4X Bucket 09 Max Num Threads Per Group.
    /// </summary>
    public const uint ComputeShader4XBucket09MaxNumThreadsPerGroup = 144;

    /// <summary>
    /// Compute Shader 4X Bucket 10 Max Bytes Tgsm Writable Per Thread.
    /// </summary>
    public const uint ComputeShader4XBucket10MaxBytesTgsmWritablePerThread = 96;

    /// <summary>
    /// Compute Shader 4X Bucket 10 Max Num Threads Per Group.
    /// </summary>
    public const uint ComputeShader4XBucket10MaxNumThreadsPerGroup = 168;

    /// <summary>
    /// Compute Shader 4X Bucket 11 Max Bytes Tgsm Writable Per Thread.
    /// </summary>
    public const uint ComputeShader4XBucket11MaxBytesTgsmWritablePerThread = 80;

    /// <summary>
    /// Compute Shader 4X Bucket 11 Max Num Threads Per Group.
    /// </summary>
    public const uint ComputeShader4XBucket11MaxNumThreadsPerGroup = 204;

    /// <summary>
    /// Compute Shader 4X Bucket 12 Max Bytes Tgsm Writable Per Thread.
    /// </summary>
    public const uint ComputeShader4XBucket12MaxBytesTgsmWritablePerThread = 64;

    /// <summary>
    /// Compute Shader 4X Bucket 12 Max Threads Per Group.
    /// </summary>
    public const uint ComputeShader4XBucket12MaxThreadsPerGroup = 256;

    /// <summary>
    /// Compute Shader 4X Bucker 13 Max Bytes Tgsm Writable Per Thread.
    /// </summary>
    public const uint ComputeShader4XBucker13MaxBytesTgsmWritablePerThread = 48;

    /// <summary>
    /// Compute Shader 4X Bucket 13 Max Num Threads Per Group.
    /// </summary>
    public const uint ComputeShader4XBucket13MaxNumThreadsPerGroup = 340;

    /// <summary>
    /// Compute Shader 4X Bucket 14 Max Bytes Tgsm Writable Per Thread.
    /// </summary>
    public const uint ComputeShader4XBucket14MaxBytesTgsmWritablePerThread = 32;

    /// <summary>
    /// Compute Shader 4X Bucket 14 Max Num Threads Per Group.
    /// </summary>
    public const uint ComputeShader4XBucket14MaxNumThreadsPerGroup = 512;

    /// <summary>
    /// Compute Shader 4X Bucket 15 Max Bytes Tgsm Writable Per Thread.
    /// </summary>
    public const uint ComputeShader4XBucket15MaxBytesTgsmWritablePerThread = 16;

    /// <summary>
    /// Compute Shader 4X Bucket 15 Max Num Threads Per Group.
    /// </summary>
    public const uint ComputeShader4XBucket15MaxNumThreadsPerGroup = 768;

    /// <summary>
    /// Compute Shader 4X Dispatch Max Thread Groups In Z Dimension.
    /// </summary>
    public const uint ComputeShader4XDispatchMaxThreadGroupsInZDimension = 1;

    /// <summary>
    /// Compute Shader 4X Raw UAV Byte Alignment.
    /// </summary>
    public const uint ComputeShader4XRawUavByteAlignment = 256;

    /// <summary>
    /// Compute Shader 4X Thread Group Max Threads Per Group.
    /// </summary>
    public const uint ComputeShader4XThreadGroupMaxThreadsPerGroup = 768;

    /// <summary>
    /// Compute Shader 4X Thread Group Max X.
    /// </summary>
    public const uint ComputeShader4XThreadGroupMaxX = 768;

    /// <summary>
    /// Compute Shader 4X Thread Group Max Y.
    /// </summary>
    public const uint ComputeShader4XThreadGroupMaxY = 768;

    /// <summary>
    /// Compute Shader 4X UAV Register Count.
    /// </summary>
    public const uint ComputeShader4XUavRegisterCount = 1;

    /// <summary>
    /// Compute Shader Dispatch Max Thread Groups Per Dimension.
    /// </summary>
    public const uint ComputeShaderDispatchMaxThreadGroupsPerDimension = 65535;

    /// <summary>
    /// Compute Shader Tgsm Register Count.
    /// </summary>
    public const uint ComputeShaderTgsmRegisterCount = 8192;

    /// <summary>
    /// Compute Shader Tgsm Register Reads Per Instance.
    /// </summary>
    public const uint ComputeShaderTgsmRegisterReadsPerInstance = 1;

    /// <summary>
    /// Compute Shader Tgsm Resource Register Components.
    /// </summary>
    public const uint ComputeShaderTgsmResourceRegisterComponents = 1;

    /// <summary>
    /// Compute Shader Tgsm Resource Register Read Ports.
    /// </summary>
    public const uint ComputeShaderTgsmResourceRegisterReadPorts = 1;

    /// <summary>
    /// Compute Shader Thread Group Id Register Components.
    /// </summary>
    public const uint ComputeShaderThreadGroupIdRegisterComponents = 3;

    /// <summary>
    /// Compute Shader Thread Group Id Register Count.
    /// </summary>
    public const uint ComputeShaderThreadGroupIdRegisterCount = 1;

    /// <summary>
    /// Compute Shader Thread Id In Group Flattened Register Components.
    /// </summary>
    public const uint ComputeShaderThreadIdInGroupFlattenedRegisterComponents = 1;

    /// <summary>
    /// Compute Shader Thread Id In Group Flattened Register Count.
    /// </summary>
    public const uint ComputeShaderThreadIdInGroupFlattenedRegisterCount = 1;

    /// <summary>
    /// Compute Shader Thread Id In Group Register Components.
    /// </summary>
    public const uint ComputeShaderThreadIdInGroupRegisterComponents = 3;

    /// <summary>
    /// Compute Shader Thread Id In Group Register Count.
    /// </summary>
    public const uint ComputeShaderThreadIdInGroupRegisterCount = 1;

    /// <summary>
    /// Compute Shader Thread Id Register Components.
    /// </summary>
    public const uint ComputeShaderThreadIdRegisterComponents = 3;

    /// <summary>
    /// Compute Shader Thread Id Register Count.
    /// </summary>
    public const uint ComputeShaderThreadIdRegisterCount = 1;

    /// <summary>
    /// Compute Shader Thread Group Max Threads Per Group.
    /// </summary>
    public const uint ComputeShaderThreadGroupMaxThreadsPerGroup = 1024;

    /// <summary>
    /// Compute Shader Thread Group Max X.
    /// </summary>
    public const uint ComputeShaderThreadGroupMaxX = 1024;

    /// <summary>
    /// Compute Shader Thread Group Max Y.
    /// </summary>
    public const uint ComputeShaderThreadGroupMaxY = 1024;

    /// <summary>
    /// Compute Shader Thread Group Max Z.
    /// </summary>
    public const uint ComputeShaderThreadGroupMaxZ = 64;

    /// <summary>
    /// Compute Shader Thread Group Min X.
    /// </summary>
    public const uint ComputeShaderThreadGroupMinX = 1;

    /// <summary>
    /// Compute Shader Thread Group Min Y.
    /// </summary>
    public const uint ComputeShaderThreadGroupMinY = 1;

    /// <summary>
    /// Compute Shader Thread Group Min Z.
    /// </summary>
    public const uint ComputeShaderThreadGroupMinZ = 1;

    /// <summary>
    /// Compute Shader Thread Local Temp Register Pool.
    /// </summary>
    public const uint ComputeShaderThreadLocalTempRegisterPool = 16384;

    /// <summary>
    /// Default Blend Factor Alpha.
    /// </summary>
    public const float DefaultBlendFactorAlpha = 1.0f;

    /// <summary>
    /// Default Blend Factor Blue.
    /// </summary>
    public const float DefaultBlendFactorBlue = 1.0f;

    /// <summary>
    /// Default Blend Factor Green.
    /// </summary>
    public const float DefaultBlendFactorGreen = 1.0f;

    /// <summary>
    /// Default Blend Factor Red
    /// </summary>
    public const float DefaultBlendFactorRed = 1.0f;

    /// <summary>
    /// Default Border Color Component.
    /// </summary>
    public const float DefaultBorderColorComponent = 0.0f;

    /// <summary>
    /// Default Depth Bias.
    /// </summary>
    public const uint DefaultDepthBias = 0;

    /// <summary>
    /// Default Depth Bias Clamp.
    /// </summary>
    public const float DefaultDepthBiasClamp = 0.0f;

    /// <summary>
    /// Default Max Anisotropy.
    /// </summary>
    public const uint DefaultMaxAnisotropy = 16;

    /// <summary>
    /// Default Mip Lod Bias.
    /// </summary>
    public const float DefaultMipLodBias = 0.0f;

    /// <summary>
    /// Default Render Target Array Index.
    /// </summary>
    public const uint DefaultRenderTargetArrayIndex = 0;

    /// <summary>
    /// Default Sample Mask.
    /// </summary>
    public const uint DefaultSampleMask = 0xffffffff;

    /// <summary>
    /// Default Scissor End X.
    /// </summary>
    public const uint DefaultScissorEndX = 0;

    /// <summary>
    /// Default Scissor End Y.
    /// </summary>
    public const uint DefaultScissorEndY = 0;

    /// <summary>
    /// Default Scissor Start X.
    /// </summary>
    public const uint DefaultScissorStartX = 0;

    /// <summary>
    /// Default Scissor Start Y.
    /// </summary>
    public const uint DefaultScissorStartY = 0;

    /// <summary>
    /// Default Slope Scaled Depth Bias.
    /// </summary>
    public const float DefaultSlopeScaledDepthBias = 0.0f;

    /// <summary>
    /// Default Stencil Read Mask.
    /// </summary>
    public const uint DefaultStencilReadMask = 0xff;

    /// <summary>
    /// Default Stencil Reference.
    /// </summary>
    public const uint DefaultStencilReference = 0;

    /// <summary>
    /// Default Stencil Write Mask.
    /// </summary>
    public const uint DefaultStencilWriteMask = 0xff;

    /// <summary>
    /// Default Viewport And Scissor Rect Index.
    /// </summary>
    public const uint DefaultViewportAndScissorRectIndex = 0;

    /// <summary>
    /// Default Viewport Height.
    /// </summary>
    public const uint DefaultViewportHeight = 0;

    /// <summary>
    /// Default Viewport Max Depth.
    /// </summary>
    public const float DefaultViewportMaxDepth = 0.0f;

    /// <summary>
    /// Default Viewport Min Depth.
    /// </summary>
    public const float DefaultViewportMinDepth = 0.0f;

    /// <summary>
    /// Default Viewport Top Left X.
    /// </summary>
    public const uint DefaultViewportTopLeftX = 0;

    /// <summary>
    /// Default Viewport Top Left Y.
    /// </summary>
    public const uint DefaultViewportTopLeftY = 0;

    /// <summary>
    /// Default Viewport Width.
    /// </summary>
    public const uint DefaultViewportWidth = 0;

    /// <summary>
    /// Domain Shader Input Control Points Max Total Scalars.
    /// </summary>
    public const uint DomainShaderInputControlPointsMaxTotalScalars = 3968;

    /// <summary>
    /// Domain Shader Input Control Point Register Components.
    /// </summary>
    public const uint DomainShaderInputControlPointRegisterComponents = 4;

    /// <summary>
    /// Domain Shader Input Control Point Register Component Bit Count.
    /// </summary>
    public const uint DomainShaderInputControlPointRegisterComponentBitCount = 32;

    /// <summary>
    /// Domain Shader Input Control Point Register Count.
    /// </summary>
    public const uint DomainShaderInputControlPointRegisterCount = 32;

    /// <summary>
    /// Domain Shader Input Control Point Register Reads Per Instance.
    /// </summary>
    public const uint DomainShaderInputControlPointRegisterReadsPerInstance = 2;

    /// <summary>
    /// Domain Shader Input Control Point Register Read Ports.
    /// </summary>
    public const uint DomainShaderInputControlPointRegisterReadPorts = 1;

    /// <summary>
    /// Domain Shader Input Domain Point Register Components.
    /// </summary>
    public const uint DomainShaderInputDomainPointRegisterComponents = 3;

    /// <summary>
    /// Domain Shader Input Domain Point Register Component Bit Count.
    /// </summary>
    public const uint DomainShaderInputDomainPointRegisterComponentBitCount = 32;

    /// <summary>
    /// Domain Shader Input Domain Point Register Count.
    /// </summary>
    public const uint DomainShaderInputDomainPointRegisterCount = 1;

    /// <summary>
    /// Domain Shader Input Domain Point Register Reads Per Instance.
    /// </summary>
    public const uint DomainShaderInputDomainPointRegisterReadsPerInstance = 2;

    /// <summary>
    /// Domain Shader Input Domain Point Register Read Ports.
    /// </summary>
    public const uint DomainShaderInputDomainPointRegisterReadPorts = 1;

    /// <summary>
    /// Domain Shader Input Patch Constant Register Components.
    /// </summary>
    public const uint DomainShaderInputPatchConstantRegisterComponents = 4;

    /// <summary>
    /// Domain Shader Input Patch Constant Register Bit Count.
    /// </summary>
    public const uint DomainShaderInputPatchConstantRegisterBitCount = 32;

    /// <summary>
    /// Domain Shader Input Patch Constant Register Count.
    /// </summary>
    public const uint DomainShaderInputPatchConstantRegisterCount = 32;

    /// <summary>
    /// Domain Shader Input Patch Constant Register Reads Per Instance.
    /// </summary>
    public const uint DomainShaderInputPatchConstantRegisterReadsPerInstance = 2;

    /// <summary>
    /// Domain Shader Input Patch Constant Register Read Ports.
    /// </summary>
    public const uint DomainShaderInputPatchConstantRegisterReadPorts = 1;

    /// <summary>
    /// Domain Shader Input Primitive Id Register Components.
    /// </summary>
    public const uint DomainShaderInputPrimitiveIdRegisterComponents = 1;

    /// <summary>
    /// Domain Shader Input Primitive Id Register Bit Count.
    /// </summary>
    public const uint DomainShaderInputPrimitiveIdRegisterBitCount = 32;

    /// <summary>
    /// Domain Shader Input Primitive Id Register Count.
    /// </summary>
    public const uint DomainShaderInputPrimitiveIdRegisterCount = 1;

    /// <summary>
    /// Domain Shader Input Primitive Id Register Reads Per Instance.
    /// </summary>
    public const uint DomainShaderInputPrimitiveIdRegisterReadsPerInstance = 2;

    /// <summary>
    /// Domain Shader Input Primitive Id Register Read Ports.
    /// </summary>
    public const uint DomainShaderInputPrimitiveIdRegisterReadPorts = 1;

    /// <summary>
    /// Domain Shader Output Register Components.
    /// </summary>
    public const uint DomainShaderOutputRegisterComponents = 4;

    /// <summary>
    /// Domain Shader Output Register Component Bit Count.
    /// </summary>
    public const uint DomainShaderOutputRegisterComponentBitCount = 32;

    /// <summary>
    /// Domain Shader Output Register Count.
    /// </summary>
    public const uint DomainShaderOutputRegisterCount = 32;

    /// <summary>
    /// Float16 Fused Tolerance In Ulp.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", MessageId = "float", Justification = "Reviewed")]
    public const float Float16FusedToleranceInUlp = 0.6f;

    /// <summary>
    /// Float32 Max.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", MessageId = "float32", Justification = "Reviewed")]
    public const float Float32Max = 3.402823466e+38f;

    /// <summary>
    /// Float32 To Integer Tolerance In Ulp.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", MessageId = "float32", Justification = "Reviewed")]
    public const float Float32ToIntegerToleranceInUlp = 0.6f;

    /// <summary>
    /// Float To Srgb Exponent Denominator.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", MessageId = "float", Justification = "Reviewed")]
    public const float FloatToSrgbExponentDenominator = 2.4f;

    /// <summary>
    /// Float To Srgb Exponent Numerator.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", MessageId = "float", Justification = "Reviewed")]
    public const float FloatToSrgbExponentNumerator = 1.0f;

    /// <summary>
    /// Float To Srgb Offset.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", MessageId = "float", Justification = "Reviewed")]
    public const float FloatToSrgbOffset = 0.055f;

    /// <summary>
    /// Float To Srgb Scale 1.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", MessageId = "float", Justification = "Reviewed")]
    public const float FloatToSrgbScale1 = 12.92f;

    /// <summary>
    /// Float To Srgb Scale 2.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", MessageId = "float", Justification = "Reviewed")]
    public const float FloatToSrgbScale2 = 1.055f;

    /// <summary>
    /// Float To Srgb Threshold.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", MessageId = "float", Justification = "Reviewed")]
    public const float FloatToSrgbThreshold = 0.0031308f;

    /// <summary>
    /// Float To Int Instruction Max Input.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", MessageId = "float", Justification = "Reviewed")]
    public const float FloatToIntInstructionMaxInput = 2147483647.999f;

    /// <summary>
    /// Float To Int Instruction Min Input.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", MessageId = "float", Justification = "Reviewed")]
    public const float FloatToIntInstructionMinInput = -2147483648.999f;

    /// <summary>
    /// Float To UInt Instruction Max Input.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", MessageId = "float", Justification = "Reviewed")]
    public const float FloatToUIntInstructionMaxInput = 4294967295.999f;

    /// <summary>
    /// Float To UInt Instruction Min Input.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", MessageId = "float", Justification = "Reviewed")]
    public const float FloatToUIntInstructionMinInput = 0.0f;

    /// <summary>
    /// Geometry Shader Input Instance Id Reads Per Instance.
    /// </summary>
    public const uint GeometryShaderInputInstanceIdReadsPerInstance = 2;

    /// <summary>
    /// Geometry Shader Input Instance Id Read Ports.
    /// </summary>
    public const uint GeometryShaderInputInstanceIdReadPorts = 1;

    /// <summary>
    /// Geometry Shader Input Instance Id Register Components.
    /// </summary>
    public const uint GeometryShaderInputInstanceIdRegisterComponents = 1;

    /// <summary>
    /// Geometry Shader Input Instance Id Register Component Bit Count.
    /// </summary>
    public const uint GeometryShaderInputInstanceIdRegisterComponentBitCount = 32;

    /// <summary>
    /// Geometry Shader Input Instance Id Register Count.
    /// </summary>
    public const uint GeometryShaderInputInstanceIdRegisterCount = 1;

    /// <summary>
    /// Geometry Shader Input Prim Const Register Components.
    /// </summary>
    public const uint GeometryShaderInputPrimConstRegisterComponents = 1;

    /// <summary>
    /// Geometry Shader Input Prim Const Register Component Bit Count.
    /// </summary>
    public const uint GeometryShaderInputPrimConstRegisterComponentBitCount = 32;

    /// <summary>
    /// Geometry Shader Input Prim Const Register Count.
    /// </summary>
    public const uint GeometryShaderInputPrimConstRegisterCount = 1;

    /// <summary>
    /// Geometry Shader Input Prim Const Register Reads Per Instance.
    /// </summary>
    public const uint GeometryShaderInputPrimConstRegisterReadsPerInstance = 2;

    /// <summary>
    /// Geometry Shader Input Prim Const Register Read Ports.
    /// </summary>
    public const uint GeometryShaderInputPrimConstRegisterReadPorts = 1;

    /// <summary>
    /// Geometry Shader Input Register Components.
    /// </summary>
    public const uint GeometryShaderInputRegisterComponents = 4;

    /// <summary>
    /// Geometry Shader Input Register Component Bit Count.
    /// </summary>
    public const uint GeometryShaderInputRegisterComponentBitCount = 32;

    /// <summary>
    /// Geometry Shader Input Register Count.
    /// </summary>
    public const uint GeometryShaderInputRegisterCount = 32;

    /// <summary>
    /// Geometry Shader Input Register Reads Per Instance.
    /// </summary>
    public const uint GeometryShaderInputRegisterReadsPerInstance = 2;

    /// <summary>
    /// Geometry Shader Input Register Read Ports.
    /// </summary>
    public const uint GeometryShaderInputRegisterReadPorts = 1;

    /// <summary>
    /// Geometry Shader Input Register Vertices.
    /// </summary>
    public const uint GeometryShaderInputRegisterVertices = 32;

    /// <summary>
    /// Geometry Shader Max Instance Count.
    /// </summary>
    public const uint GeometryShaderMaxInstanceCount = 32;

    /// <summary>
    /// Geometry Shader Max Output Vertex Count Across Instances.
    /// </summary>
    public const uint GeometryShaderMaxOutputVertexCountAcrossInstances = 1024;

    /// <summary>
    /// Geometry Shader Output Elements.
    /// </summary>
    public const uint GeometryShaderOutputElements = 32;

    /// <summary>
    /// Geometry Shader Output Register Components.
    /// </summary>
    public const uint GeometryShaderOutputRegisterComponents = 4;

    /// <summary>
    /// Geometry Shader Output Register Component Bit Count.
    /// </summary>
    public const uint GeometryShaderOutputRegisterComponentBitCount = 32;

    /// <summary>
    /// Geometry Shader Output Register Count.
    /// </summary>
    public const uint GeometryShaderOutputRegisterCount = 32;

    /// <summary>
    /// Hull Shader Control Point Phase Input Register Count.
    /// </summary>
    public const uint HullShaderControlPointPhaseInputRegisterCount = 32;

    /// <summary>
    /// Hull Shader Control Point Phase Output Register Count.
    /// </summary>
    public const uint HullShaderControlPointPhaseOutputRegisterCount = 32;

    /// <summary>
    /// Hull Shader Control Point Register Components.
    /// </summary>
    public const uint HullShaderControlPointRegisterComponents = 4;

    /// <summary>
    /// Hull Shader Control Point Register Component Bit Count.
    /// </summary>
    public const uint HullShaderControlPointRegisterComponentBitCount = 32;

    /// <summary>
    /// Hull Shader Control Point Register Reads Per Instance.
    /// </summary>
    public const uint HullShaderControlPointRegisterReadsPerInstance = 2;

    /// <summary>
    /// Hull Shader Control Point Register Read Ports.
    /// </summary>
    public const uint HullShaderControlPointRegisterReadPorts = 1;

    /// <summary>
    /// Hull Shader Fork Phase Instance Count Upper Bound.
    /// </summary>
    public const uint HullShaderForkPhaseInstanceCountUpperBound = 0xFFFFFFFF;

    /// <summary>
    /// Hull Shader Input Fork Instance Id Register Components.
    /// </summary>
    public const uint HullShaderInputForkInstanceIdRegisterComponents = 1;

    /// <summary>
    /// Hull Shader Input Fork Instance Id Register Component Bit Count.
    /// </summary>
    public const uint HullShaderInputForkInstanceIdRegisterComponentBitCount = 32;

    /// <summary>
    /// Hull Shader Input Fork Instance Id Register Count.
    /// </summary>
    public const uint HullShaderInputForkInstanceIdRegisterCount = 1;

    /// <summary>
    /// Hull Shader Input Fork Instance Id Register Reads Per Instance.
    /// </summary>
    public const uint HullShaderInputForkInstanceIdRegisterReadsPerInstance = 2;

    /// <summary>
    /// Hull Shader Input Fork Instance Id Register Read Ports.
    /// </summary>
    public const uint HullShaderInputForkInstanceIdRegisterReadPorts = 1;

    /// <summary>
    /// Hull Shader Input Join Instance Id Register Components.
    /// </summary>
    public const uint HullShaderInputJoinInstanceIdRegisterComponents = 1;

    /// <summary>
    /// Hull Shader Input Join Instance Id Register Component Bit Count.
    /// </summary>
    public const uint HullShaderInputJoinInstanceIdRegisterComponentBitCount = 32;

    /// <summary>
    /// Hull Shader Input Join Instance Id Register Count.
    /// </summary>
    public const uint HullShaderInputJoinInstanceIdRegisterCount = 1;

    /// <summary>
    /// Hull Shader Input Join Instance Id Register Reads Per Instance.
    /// </summary>
    public const uint HullShaderInputJoinInstanceIdRegisterReadsPerInstance = 2;

    /// <summary>
    /// Hull Shader Input Join Instance Id Register Read Ports.
    /// </summary>
    public const uint HullShaderInputJoinInstanceIdRegisterReadPorts = 1;

    /// <summary>
    /// Hull Shader Input Primitive Id Register Components.
    /// </summary>
    public const uint HullShaderInputPrimitiveIdRegisterComponents = 1;

    /// <summary>
    /// Hull Shader Input Primitive Id Register Component Bit Count.
    /// </summary>
    public const uint HullShaderInputPrimitiveIdRegisterComponentBitCount = 32;

    /// <summary>
    /// Hull Shader Input Primitive Id Register Count.
    /// </summary>
    public const uint HullShaderInputPrimitiveIdRegisterCount = 1;

    /// <summary>
    /// Hull Shader Input Primitive Id Register Reads Per Instance.
    /// </summary>
    public const uint HullShaderInputPrimitiveIdRegisterReadsPerInstance = 2;

    /// <summary>
    /// Hull Shader Input Primitive Id Register Read Ports.
    /// </summary>
    public const uint HullShaderInputPrimitiveIdRegisterReadPorts = 1;

    /// <summary>
    /// Hull Shader Join Phase Instance Count Upper Bound.
    /// </summary>
    public const uint HullShaderJoinPhaseInstanceCountUpperBound = 0xFFFFFFFF;

    /// <summary>
    /// Hull Shader Max Tessellation Factor Lower Bound.
    /// </summary>
    public const float HullShaderMaxTessellationFactorLowerBound = 1.0f;

    /// <summary>
    /// Hull Shader Max Tessellation Factor Upper Bound.
    /// </summary>
    public const float HullShaderMaxTessellationFactorUpperBound = 64.0f;

    /// <summary>
    /// Hull Shader Output Control Points Max Total Scalars.
    /// </summary>
    public const uint HullShaderOutputControlPointsMaxTotalScalars = 3968;

    /// <summary>
    /// Hull Shader Output Control Point Id Register Components.
    /// </summary>
    public const uint HullShaderOutputControlPointIdRegisterComponents = 1;

    /// <summary>
    /// Hull Shader Output Control Point Id Register Component Bit Count.
    /// </summary>
    public const uint HullShaderOutputControlPointIdRegisterComponentBitCount = 32;

    /// <summary>
    /// Hull Shader Output Control Point Id Register Count.
    /// </summary>
    public const uint HullShaderOutputControlPointIdRegisterCount = 1;

    /// <summary>
    /// Hull Shader Output Control Point Id Register Reads Per Instance.
    /// </summary>
    public const uint HullShaderOutputControlPointIdRegisterReadsPerInstance = 2;

    /// <summary>
    /// Hull Shader Output Control Point Id Register Read Ports.
    /// </summary>
    public const uint HullShaderOutputControlPointIdRegisterReadPorts = 1;

    /// <summary>
    /// Hull Shader Output Patch Constant Register Components.
    /// </summary>
    public const uint HullShaderOutputPatchConstantRegisterComponents = 4;

    /// <summary>
    /// Hull Shader Output Patch Constant Register Component Bit Count.
    /// </summary>
    public const uint HullShaderOutputPatchConstantRegisterComponentBitCount = 32;

    /// <summary>
    /// Hull Shader Output Patch Constant Register Count.
    /// </summary>
    public const uint HullShaderOutputPatchConstantRegisterCount = 32;

    /// <summary>
    /// Hull Shader Output Patch Constant Register Reads Per Instance.
    /// </summary>
    public const uint HullShaderOutputPatchConstantRegisterReadsPerInstance = 2;

    /// <summary>
    /// Hull Shader Output Patch Constant Register Read Ports.
    /// </summary>
    public const uint HullShaderOutputPatchConstantRegisterReadPorts = 1;

    /// <summary>
    /// Hull Shader Output Patch Constant Register Scalar Components.
    /// </summary>
    public const uint HullShaderOutputPatchConstantRegisterScalarComponents = 128;

    /// <summary>
    /// Input Assembler Default Index Buffer Offset In Bytes.
    /// </summary>
    public const uint InputAssemblerDefaultIndexBufferOffsetInBytes = 0;

    /// <summary>
    /// Input Assembler Default Primitive Topology.
    /// </summary>
    public const uint InputAssemblerDefaultPrimitiveTopology = 0;

    /// <summary>
    /// Input Assembler Default Vertex Buffer Offset In Bytes.
    /// </summary>
    public const uint InputAssemblerDefaultVertexBufferOffsetInBytes = 0;

    /// <summary>
    /// Input Assembler Index Input Resource Slot Count.
    /// </summary>
    public const uint InputAssemblerIndexInputResourceSlotCount = 1;

    /// <summary>
    /// Input Assembler Instance Id Bit Count.
    /// </summary>
    public const uint InputAssemblerInstanceIdBitCount = 32;

    /// <summary>
    /// Input Assembler Integer Arithmetic Bit Count.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", MessageId = "integer", Justification = "Reviewed")]
    public const uint InputAssemblerIntegerArithmeticBitCount = 32;

    /// <summary>
    /// Input Assembler Patch Max Control Point Count.
    /// </summary>
    public const uint InputAssemblerPatchMaxControlPointCount = 32;

    /// <summary>
    /// Input Assembler Primitive Id Bit Count.
    /// </summary>
    public const uint InputAssemblerPrimitiveIdBitCount = 32;

    /// <summary>
    /// Input Assembler Vertex Id Bit Count.
    /// </summary>
    public const uint InputAssemblerVertexIdBitCount = 32;

    /// <summary>
    /// Input Assembler Vertex Input Resource Slot Count.
    /// </summary>
    public const uint InputAssemblerVertexInputResourceSlotCount = 32;

    /// <summary>
    /// Input Assembler Vertex Input Structure Elements Components.
    /// </summary>
    public const uint InputAssemblerVertexInputStructureElementsComponents = 128;

    /// <summary>
    /// Input Assembler Vertex Input Structure Element Count.
    /// </summary>
    public const uint InputAssemblerVertexInputStructureElementCount = 32;

    /// <summary>
    /// Integer Divide By Zero Quotient.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", MessageId = "integer", Justification = "Reviewed")]
    public const uint IntegerDivideByZeroQuotient = 0xffffffff;

    /// <summary>
    /// Integer Divide By Zero Remainder.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", MessageId = "integer", Justification = "Reviewed")]
    public const uint IntegerDivideByZeroRemainder = 0xffffffff;

    /// <summary>
    /// Keep Render Targets And Depth Stencil.
    /// </summary>
    public const uint KeepRenderTargetsAndDepthStencil = 0xffffffff;

    /// <summary>
    /// Keep Unordered Access Views.
    /// </summary>
    public const uint KeepUnorderedAccessViews = 0xffffffff;

    /// <summary>
    /// Linear Gamma.
    /// </summary>
    public const float LinearGamma = 1.0f;

    /// <summary>
    /// Major Version.
    /// </summary>
    public const uint MajorVersion = 11;

    /// <summary>
    /// Max Border Color Component.
    /// </summary>
    public const float MaxBorderColorComponent = 1.0f;

    /// <summary>
    /// Max Depth.
    /// </summary>
    public const float MaxDepth = 1.0f;

    /// <summary>
    /// Max Anisotropy.
    /// </summary>
    public const uint MaxAnisotropy = 16;

    /// <summary>
    /// Max Multisample Sample Count.
    /// </summary>
    public const uint MaxMultisampleSampleCount = 32;

    /// <summary>
    /// Max Position Value.
    /// </summary>
    public const float MaxPositionValue = 3.402823466e+34f;

    /// <summary>
    /// Max Texture Dimension 2 To Exp.
    /// </summary>
    public const uint MaxTextureDimension2ToExp = 17;

    /// <summary>
    /// Minor Version.
    /// </summary>
    public const uint MinorVersion = 0;

    /// <summary>
    /// Min Border Color Component.
    /// </summary>
    public const float MinBorderColorComponent = 0.0f;

    /// <summary>
    /// Min Depth.
    /// </summary>
    public const float MinDepth = 0.0f;

    /// <summary>
    /// Min Max Anisotropy.
    /// </summary>
    public const uint MinMaxAnisotropy = 0;

    /// <summary>
    /// Mip Lod Bias Max.
    /// </summary>
    public const float MipLodBiasMax = 15.99f;

    /// <summary>
    /// Mip Lod Bias Min.
    /// </summary>
    public const float MipLodBiasMin = -16.0f;

    /// <summary>
    /// Mip Lod Fractional Bit Count.
    /// </summary>
    public const uint MipLodFractionalBitCount = 8;

    /// <summary>
    /// Mip Lod Range Bit Count.
    /// </summary>
    public const uint MipLodRangeBitCount = 8;

    /// <summary>
    /// Multisample Antialias Line Width.
    /// </summary>
    public const float MultisampleAntialiasLineWidth = 1.4f;

    /// <summary>
    /// Non Sample Fetch Out Of Range Access Result.
    /// </summary>
    public const uint NonSampleFetchOutOfRangeAccessResult = 0;

    /// <summary>
    /// Pixel Address Range Bit Count.
    /// </summary>
    public const uint PixelAddressRangeBitCount = 15;

    /// <summary>
    /// Pre Scissor Pixel Address Range Bit Count.
    /// </summary>
    public const uint PreScissorPixelAddressRangeBitCount = 16;

    /// <summary>
    /// Pixel Shader Compute Shader UAV Register Components.
    /// </summary>
    public const uint PixelShaderComputeShaderUavRegisterComponents = 1;

    /// <summary>
    /// Pixel Shader Compute Shader UAV Register Count.
    /// </summary>
    public const uint PixelShaderComputeShaderUavRegisterCount = 8;

    /// <summary>
    /// Pixel Shader Compute Shader UAV Register Reads Per Instance.
    /// </summary>
    public const uint PixelShaderComputeShaderUavRegisterReadsPerInstance = 1;

    /// <summary>
    /// Pixel Shader Compute Shader UAV Register Read Ports.
    /// </summary>
    public const uint PixelShaderComputeShaderUavRegisterReadPorts = 1;

    /// <summary>
    /// Pixel Shader Front Facing Default Value.
    /// </summary>
    public const uint PixelShaderFrontFacingDefaultValue = 0xFFFFFFFF;

    /// <summary>
    /// Pixel Shader Front Facing False Value.
    /// </summary>
    public const uint PixelShaderFrontFacingFalseValue = 0x00000000;

    /// <summary>
    /// Pixel Shader Front Facing True Value.
    /// </summary>
    public const uint PixelShaderFrontFacingTrueValue = 0xFFFFFFFF;

    /// <summary>
    /// Pixel Shader Input Register Components.
    /// </summary>
    public const uint PixelShaderInputRegisterComponents = 4;

    /// <summary>
    /// Pixel Shader Input Register Component Bit Count.
    /// </summary>
    public const uint PixelShaderInputRegisterComponentBitCount = 32;

    /// <summary>
    /// Pixel Shader Input Register Count.
    /// </summary>
    public const uint PixelShaderInputRegisterCount = 32;

    /// <summary>
    /// Pixel Shader Input Register Reads Per Instance.
    /// </summary>
    public const uint PixelShaderInputRegisterReadsPerInstance = 2;

    /// <summary>
    /// Pixel Shader Input Register Read Ports.
    /// </summary>
    public const uint PixelShaderInputRegisterReadPorts = 1;

    /// <summary>
    /// Pixel Shader Legacy Pixel Center Fractional Component.
    /// </summary>
    public const float PixelShaderLegacyPixelCenterFractionalComponent = 0.0f;

    /// <summary>
    /// Pixel Shader Output Depth Register Components.
    /// </summary>
    public const uint PixelShaderOutputDepthRegisterComponents = 1;

    /// <summary>
    /// Pixel Shader Output Depth Register Component Bit Count.
    /// </summary>
    public const uint PixelShaderOutputDepthRegisterComponentBitCount = 32;

    /// <summary>
    /// Pixel Shader Output Depth Register Count.
    /// </summary>
    public const uint PixelShaderOutputDepthRegisterCount = 1;

    /// <summary>
    /// Pixel Shader Output Mask Register Components.
    /// </summary>
    public const uint PixelShaderOutputMaskRegisterComponents = 1;

    /// <summary>
    /// Pixel Shader Output Mask Register Component Bit Count.
    /// </summary>
    public const uint PixelShaderOutputMaskRegisterComponentBitCount = 32;

    /// <summary>
    /// Pixel Shader Output Mask Register Count.
    /// </summary>
    public const uint PixelShaderOutputMaskRegisterCount = 1;

    /// <summary>
    /// Pixel Shader Output Register Components.
    /// </summary>
    public const uint PixelShaderOutputRegisterComponents = 4;

    /// <summary>
    /// Pixel Shader Output Register Component Bit Count.
    /// </summary>
    public const uint PixelShaderOutputRegisterComponentBitCount = 32;

    /// <summary>
    /// Pixel Shader Output Register Count.
    /// </summary>
    public const uint PixelShaderOutputRegisterCount = 8;

    /// <summary>
    /// Pixel Shader Pixel Center Fractional Component.
    /// </summary>
    public const float PixelShaderPixelCenterFractionalComponent = 0.5f;

    /// <summary>
    /// Raw UAV SRV Byte Alignment.
    /// </summary>
    public const uint RawUavSrvByteAlignment = 16;

    /// <summary>
    /// Req Blend Object Count Per Device.
    /// </summary>
    public const uint ReqBlendObjectCountPerDevice = 4096;

    /// <summary>
    /// Req Buffer Resource Texel Count 2 To Exp.
    /// </summary>
    public const uint ReqBufferResourceTexelCount2ToExp = 27;

    /// <summary>
    /// Req Constant Buffer Element Count.
    /// </summary>
    public const uint ReqConstantBufferElementCount = 4096;

    /// <summary>
    /// Req Depth Stencil Object Count Per Device.
    /// </summary>
    public const uint ReqDepthStencilObjectCountPerDevice = 4096;

    /// <summary>
    /// Req Draw Indexed Index Count 2 To Exp.
    /// </summary>
    public const uint ReqDrawIndexedIndexCount2ToExp = 32;

    /// <summary>
    /// Req Draw Vertex Count 2 To Exp.
    /// </summary>
    public const uint ReqDrawVertexCount2ToExp = 32;

    /// <summary>
    /// Req Filtering Hardware Addressable Resource Dimension.
    /// </summary>
    public const uint ReqFilteringHardwareAddressableResourceDimension = 16384;

    /// <summary>
    /// Req Geometry Shader Invocation 32 Bit Output Component Limit.
    /// </summary>
    public const uint ReqGeometryShaderInvocation32BitOutputComponentLimit = 1024;

    /// <summary>
    /// Req Immediate Constant Buffer Element Count.
    /// </summary>
    public const uint ReqImmediateConstantBufferElementCount = 4096;

    /// <summary>
    /// Req Max Anisotropy.
    /// </summary>
    public const uint ReqMaxAnisotropy = 16;

    /// <summary>
    /// Req Mip Levels.
    /// </summary>
    public const uint ReqMipLevels = 15;

    /// <summary>
    /// Req Multi Element Structure Size In Bytes.
    /// </summary>
    public const uint ReqMultiElementStructureSizeInBytes = 2048;

    /// <summary>
    /// Req Rasterizer Object Count Per Device.
    /// </summary>
    public const uint ReqRasterizerObjectCountPerDevice = 4096;

    /// <summary>
    /// Req Render To Buffer Window Width.
    /// </summary>
    public const uint ReqRenderToBufferWindowWidth = 16384;

    /// <summary>
    /// Req Resource Size In Megabytes Expression A Term.
    /// </summary>
    public const uint ReqResourceSizeInMegabytesExpressionATerm = 128;

    /// <summary>
    /// Req Resource Size In Megabytes Expression B Term.
    /// </summary>
    public const float ReqResourceSizeInMegabytesExpressionBTerm = 0.25f;

    /// <summary>
    /// Req Resource Size In Megabytes Expression C Term.
    /// </summary>
    public const uint ReqResourceSizeInMegabytesExpressionCTerm = 2048;

    /// <summary>
    /// Req Resource View Count Per Device 2 To Exp.
    /// </summary>
    public const uint ReqResourceViewCountPerDevice2ToExp = 20;

    /// <summary>
    /// Req Sampler Object Count Per Device.
    /// </summary>
    public const uint ReqSamplerObjectCountPerDevice = 4096;

    /// <summary>
    /// Req Texture 1D Array Axis Dimension.
    /// </summary>
    public const uint ReqTexture1DArrayAxisDimension = 2048;

    /// <summary>
    /// Req Texture 1D Dimension.
    /// </summary>
    public const uint ReqTexture1DDimension = 16384;

    /// <summary>
    /// Req Texture 2D Array Axis Dimension.
    /// </summary>
    public const uint ReqTexture2DArrayAxisDimension = 2048;

    /// <summary>
    /// Req Texture 2D Dimension.
    /// </summary>
    public const uint ReqTexture2DDimension = 16384;

    /// <summary>
    /// Req Texture 3D Dimension.
    /// </summary>
    public const uint ReqTexture3DDimension = 2048;

    /// <summary>
    /// Req Texture Cube Dimension.
    /// </summary>
    public const uint ReqTextureCubeDimension = 16384;

    /// <summary>
    /// Resinfo Instruction Missing Component Retval.
    /// </summary>
    public const uint ResinfoInstructionMissingComponentRetval = 0;

    /// <summary>
    /// Shader Major Version.
    /// </summary>
    public const uint ShaderMajorVersion = 5;

    /// <summary>
    /// Shader Max Instances.
    /// </summary>
    public const uint ShaderMaxInstances = 65535;

    /// <summary>
    /// Shader Max Interfaces.
    /// </summary>
    public const uint ShaderMaxInterfaces = 253;

    /// <summary>
    /// Shader Max Interface Call Sites.
    /// </summary>
    public const uint ShaderMaxInterfaceCallSites = 4096;

    /// <summary>
    /// Shader Max Types.
    /// </summary>
    public const uint ShaderMaxTypes = 65535;

    /// <summary>
    /// Shader Minor Version.
    /// </summary>
    public const uint ShaderMinorVersion = 0;

    /// <summary>
    /// Shift Instruction Pad Value.
    /// </summary>
    public const uint ShiftInstructionPadValue = 0;

    /// <summary>
    /// Shift Instruction Shift Value Bit Count.
    /// </summary>
    public const uint ShiftInstructionShiftValueBitCount = 5;

    /// <summary>
    /// Simultaneous Render Target Count.
    /// </summary>
    public const uint SimultaneousRenderTargetCount = 8;

    /// <summary>
    /// Stream Output Buffer Max Stride In Bytes.
    /// </summary>
    public const uint StreamOutputBufferMaxStrideInBytes = 2048;

    /// <summary>
    /// Stream Output Buffer Max Write Window In Bytes.
    /// </summary>
    public const uint StreamOutputBufferMaxWriteWindowInBytes = 512;

    /// <summary>
    /// Stream Output Buffer Slot Count.
    /// </summary>
    public const uint StreamOutputBufferSlotCount = 4;

    /// <summary>
    /// Stream Output Ddi Register Index Denoting Gap.
    /// </summary>
    public const uint StreamOutputDdiRegisterIndexDenotingGap = 0xffffffff;

    /// <summary>
    /// Stream Output No Rasterized Stream.
    /// </summary>
    public const uint StreamOutputNoRasterizedStream = 0xffffffff;

    /// <summary>
    /// Stream Output Output Component Count.
    /// </summary>
    public const uint StreamOutputOutputComponentCount = 128;

    /// <summary>
    /// Stream Output Stream Count.
    /// </summary>
    public const uint StreamOutputStreamCount = 4;

    /// <summary>
    /// Spec Date Day.
    /// </summary>
    public const uint SpecDateDay = 16;

    /// <summary>
    /// Spec Date Month.
    /// </summary>
    public const uint SpecDateMonth = 05;

    /// <summary>
    /// Spec Date Year.
    /// </summary>
    public const uint SpecDateYear = 2011;

    /// <summary>
    /// Spec Version.
    /// </summary>
    public const float SpecVersion = 1.07f;

    /// <summary>
    /// Srgb Gamma.
    /// </summary>
    public const float SrgbGamma = 2.2f;

    /// <summary>
    /// Srgb To Float Denominator 1.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", MessageId = "float", Justification = "Reviewed")]
    public const float SrgbToFloatDenominator1 = 12.92f;

    /// <summary>
    /// Srgb To Float Denominator 2.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", MessageId = "float", Justification = "Reviewed")]
    public const float SrgbToFloatDenominator2 = 1.055f;

    /// <summary>
    /// Srgb To Float Exponent.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", MessageId = "float", Justification = "Reviewed")]
    public const float SrgbToFloatExponent = 2.4f;

    /// <summary>
    /// Srgb To Float Offset.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", MessageId = "float", Justification = "Reviewed")]
    public const float SrgbToFloatOffset = 0.055f;

    /// <summary>
    /// Srgb To Float Threshold.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", MessageId = "float", Justification = "Reviewed")]
    public const float SrgbToFloatThreshold = 0.04045f;

    /// <summary>
    /// Srgb To Float Tolerance In Ulp.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", MessageId = "float", Justification = "Reviewed")]
    public const float SrgbToFloatToleranceInUlp = 0.5f;

    /// <summary>
    /// Standard Component Bit Count.
    /// </summary>
    public const uint StandardComponentBitCount = 32;

    /// <summary>
    /// Standard Component Bit Count Doubled.
    /// </summary>
    public const uint StandardComponentBitCountDoubled = 64;

    /// <summary>
    /// Standard Maximum Element Alignment Byte Multiple.
    /// </summary>
    public const uint StandardMaximumElementAlignmentByteMultiple = 4;

    /// <summary>
    /// Standard Pixel Component Count.
    /// </summary>
    public const uint StandardPixelComponentCount = 128;

    /// <summary>
    /// Standard Pixel Element Count.
    /// </summary>
    public const uint StandardPixelElementCount = 32;

    /// <summary>
    /// Standard Vector Size.
    /// </summary>
    public const uint StandardVectorSize = 4;

    /// <summary>
    /// Standard Vertex Element Count.
    /// </summary>
    public const uint StandardVertexElementCount = 32;

    /// <summary>
    /// Standard Vertex Total Component Count.
    /// </summary>
    public const uint StandardVertexTotalComponentCount = 64;

    /// <summary>
    /// Subpixel Fractional Bit Count.
    /// </summary>
    public const uint SubpixelFractionalBitCount = 8;

    /// <summary>
    /// Subtexel Fractional Bit Count.
    /// </summary>
    public const uint SubtexelFractionalBitCount = 8;

    /// <summary>
    /// Tesselator Max Even Tessellation Factor.
    /// </summary>
    public const uint TesselatorMaxEvenTessellationFactor = 64;

    /// <summary>
    /// Tesselator Max Isoline Density Tessellation Factor.
    /// </summary>
    public const uint TesselatorMaxIsolineDensityTessellationFactor = 64;

    /// <summary>
    /// Tesselator Max Odd Tessellation Factor.
    /// </summary>
    public const uint TesselatorMaxOddTessellationFactor = 63;

    /// <summary>
    /// Tesselator Max Tessellation Factor.
    /// </summary>
    public const uint TesselatorMaxTessellationFactor = 64;

    /// <summary>
    /// Tesselator Min Even Tessellation Factor.
    /// </summary>
    public const uint TesselatorMinEvenTessellationFactor = 2;

    /// <summary>
    /// Tesselator Min Isoline Density Tessellation Factor.
    /// </summary>
    public const uint TesselatorMinIsolineDensityTessellationFactor = 1;

    /// <summary>
    /// Tesselator Min Odd Tessellation Factor.
    /// </summary>
    public const uint TesselatorMinOddTessellationFactor = 1;

    /// <summary>
    /// Texel Address Range Bit Count.
    /// </summary>
    public const uint TexelAddressRangeBitCount = 16;

    /// <summary>
    /// Unbound Memory Access Result.
    /// </summary>
    public const uint UnboundMemoryAccessResult = 0;

    /// <summary>
    /// Viewport And Scissor Rect Max Index.
    /// </summary>
    public const uint ViewportAndScissorRectMaxIndex = 15;

    /// <summary>
    /// Viewport And Scissor Rect Object Count Per Pipeline.
    /// </summary>
    public const uint ViewportAndScissorRectObjectCountPerPipeline = 16;

    /// <summary>
    /// Viewport Bounds Max.
    /// </summary>
    public const uint ViewportBoundsMax = 32767;

    /// <summary>
    /// Viewport Bounds Min.
    /// </summary>
    public const int ViewportBoundsMin = -32768;

    /// <summary>
    /// Vertex Shader Input Register Components.
    /// </summary>
    public const uint VertexShaderInputRegisterComponents = 4;

    /// <summary>
    /// Vertex Shader Input Register Component Bit Count.
    /// </summary>
    public const uint VertexShaderInputRegisterComponentBitCount = 32;

    /// <summary>
    /// Vertex Shader Input Register Count.
    /// </summary>
    public const uint VertexShaderInputRegisterCount = 32;

    /// <summary>
    /// Vertex Shader Input Register Reads Per Instance.
    /// </summary>
    public const uint VertexShaderInputRegisterReadsPerInstance = 2;

    /// <summary>
    /// Vertex Shader Input Register Read Ports.
    /// </summary>
    public const uint VertexShaderInputRegisterReadPorts = 1;

    /// <summary>
    /// Vertex Shader Output Register Components.
    /// </summary>
    public const uint VertexShaderOutputRegisterComponents = 4;

    /// <summary>
    /// Vertex Shader Output Register Component Bit Count.
    /// </summary>
    public const uint VertexShaderOutputRegisterComponentBitCount = 32;

    /// <summary>
    /// Vertex Shader Output Register Count.
    /// </summary>
    public const uint VertexShaderOutputRegisterCount = 32;

    /// <summary>
    /// WHQL Context Count For Resource Limit.
    /// </summary>
    public const uint WhqlContextCountForResourceLimit = 10;

    /// <summary>
    /// WHQL Draw Indexed Index Count 2 To Exp.
    /// </summary>
    public const uint WhqlDrawIndexedIndexCount2ToExp = 25;

    /// <summary>
    /// WHQL Draw Vertex Count 2 To Exp.
    /// </summary>
    public const uint WhqlDrawVertexCount2ToExp = 25;

    /// <summary>
    /// D3D11.1 Unordered Access View Slot Count.
    /// </summary>
    public const uint D3D111UnorderedAccessViewSlotCount = 64;

    /// <summary>
    /// D3D11.2 Tiled Resource Tile Size In Bytes.
    /// </summary>
    public const uint D3D112TiledResourceTileSizeInBytes = 65536;
}

/// <summary>
/// Describes the set of features targeted by a Direct3D device.
/// </summary>
[SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue", Justification = "Reviewed")]
public enum D3D11FeatureLevel
{
    /// <summary>
    /// Targets features supported by feature level 9.1 including shader model 2.
    /// </summary>
    FeatureLevel91 = 0x9100,

    /// <summary>
    /// Targets features supported by feature level 9.2 including shader model 2.
    /// </summary>
    FeatureLevel92 = 0x9200,

    /// <summary>
    /// Targets features supported by feature level 9.3 including shader model 2.0b.
    /// </summary>
    FeatureLevel93 = 0x9300,

    /// <summary>
    /// Targets features supported by Direct3D 10.0 including shader model 4.
    /// </summary>
    FeatureLevel100 = 0xa000,

    /// <summary>
    /// Targets features supported by Direct3D 10.1 including shader model 4.
    /// </summary>
    FeatureLevel101 = 0xa100,

    /// <summary>
    /// Targets features supported by Direct3D 11.0 including shader model 5.
    /// </summary>
    FeatureLevel110 = 0xb000,

    /// <summary>
    /// Targets features supported by Direct3D 11.1 including shader model 5 and logical blend operations.
    /// </summary>
    FeatureLevel111 = 0xb100
}