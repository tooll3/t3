using System;
using System.Collections.Generic;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.IO;
using SharpDX.WIC;
using T3.Core.Logging;
using T3.Core.Resource;
using T3.Core.Utils;

namespace T3.Editor.Gui.Windows.RenderExport;

public static class ScreenshotWriter
{
    public enum FileFormats
    {
        Png,
        Jpg,
    }
    public static List<Format> SupportedInputFormats { get; }= new()
                                                       {
                                                           SharpDX.DXGI.Format.R8G8B8A8_UNorm,
                                                           SharpDX.DXGI.Format.R16G16B16A16_UNorm,
                                                           SharpDX.DXGI.Format.R16G16B16A16_Float,
                                                           SharpDX.DXGI.Format.B8G8R8A8_UNorm
                                                       };
    
    public static bool SavingComplete => _saveQueue.Count == 0;
    
    public static bool StartSavingToFile(Texture2D texture2d, string filepath, FileFormats format)
    {
        if (texture2d == null)
            return false;
        
        PrepareCpuAccessTextures(texture2d.Description);
        _saveQueue.Add(new SaveRequest
                              {
                                  RequestIndex = _swapCounter,
                                  Filepath = filepath,
                                  FileFormat = format,
                              });

        // Copy the original texture to a readable image
        var immediateContext = ResourceManager.Device.ImmediateContext;
        _readableTexture = _imagesWithCpuAccess[SwapIndex];
        immediateContext.CopyResource(texture2d, _readableTexture);
        immediateContext.UnmapSubresource(_readableTexture, 0);
        return true;
    }
    
    /// <summary>
    /// Saving a screenshot will take several frames because it takes a while until the frames are
    /// downloaded from the gpu. The method need to be called until once a frame.
    /// </summary>
    /// <returns></returns>
    public static void UpdateSaving()
    {
        _swapCounter++;
        while(_saveQueue.Count >0 && _saveQueue[0].IsObsolete)
            _saveQueue.RemoveAt(0);

        if (_saveQueue.Count == 0)
            return;

        if (!_saveQueue[0].IsReady)
            return;

        try
        {
            Save(_saveQueue[0]);
        }
        catch (Exception e)
        {
            Log.Warning("Can't save image:" + e.Message);
        }
        
        _saveQueue.RemoveAt(0);
    }
    
    private static void Save(SaveRequest request) 
    {
        var immediateContext = ResourceManager.Device.ImmediateContext;
        
        var dataBox = immediateContext.MapSubresource(_readableTexture,
                                                      0,
                                                      0,
                                                      MapMode.Read,
                                                      SharpDX.Direct3D11.MapFlags.None,
                                                      out var imageStream);
        using var dataStream = imageStream;
        
        var width = _currentDesc.Width;
        var height = _currentDesc.Height;
        var factory = new ImagingFactory();

        var stream = new WICStream(factory, request.Filepath, NativeFileAccess.Write);

        // Initialize a Jpeg encoder with this stream
        BitmapEncoder encoder = (request.FileFormat == FileFormats.Png)
                                    ? new PngBitmapEncoder(factory)
                                    : new JpegBitmapEncoder(factory);
        encoder.Initialize(stream);

        // Create a Frame encoder
        var bitmapFrameEncode = new BitmapFrameEncode(encoder);
        bitmapFrameEncode.Initialize();
        bitmapFrameEncode.SetSize(width, height);
        var formatId = PixelFormat.Format32bppRGBA;
        bitmapFrameEncode.SetPixelFormat(ref formatId);

        // Write a pseudo-plasma to a buffer
        var rowStride = PixelFormat.GetStride(formatId, width);
        var outBufferSize = height * rowStride;
        var outDataStream = new DataStream(outBufferSize, true, true);
        
        try
        {
            switch (_currentDesc.Format)
            {
                case Format.R16G16B16A16_Float:
                    for (var y1 = 0; y1 < height; y1++)
                    {
                        for (var x1 = 0; x1 < width; x1++)
                        {
                            imageStream.Position = (long)(y1) * dataBox.RowPitch + (long)(x1) * 8;

                            var r = FormatConversion.Read2BytesToHalf(imageStream);
                            var g = FormatConversion.Read2BytesToHalf(imageStream);
                            var b = FormatConversion.Read2BytesToHalf(imageStream);
                            var a = FormatConversion.Read2BytesToHalf(imageStream);

                            outDataStream.WriteByte((byte)(b.Clamp(0, 1) * 255));
                            outDataStream.WriteByte((byte)(g.Clamp(0, 1) * 255));
                            outDataStream.WriteByte((byte)(r.Clamp(0, 1) * 255));
                            if (request.FileFormat == FileFormats.Png)
                            {
                                outDataStream.WriteByte((byte)(a.Clamp(0, 1) * 255));
                            }
                        }
                    }
                    break;

                case Format.R8G8B8A8_UNorm:
                    for (var y1 = 0; y1 < height; y1++)
                    {
                        imageStream.Position = (long)(y1) * dataBox.RowPitch;
                        for (var x1 = 0; x1 < width; x1++)
                        {
                            var r = (byte)imageStream.ReadByte();
                            var g = (byte)imageStream.ReadByte();
                            var b = (byte)imageStream.ReadByte();
                            outDataStream.WriteByte(b);
                            outDataStream.WriteByte(g);
                            outDataStream.WriteByte(r);

                            var a = imageStream.ReadByte();
                            if (request.FileFormat == FileFormats.Png)
                            {
                                outDataStream.WriteByte((byte)a);
                            }
                        }
                    }
                    break;
                
                case SharpDX.DXGI.Format.B8G8R8A8_UNorm:
                    if (request.FileFormat == FileFormats.Png)
                    {
                        // Note: dataBox.RowPitch and outputStream.RowPitch can diverge if width is not divisible by 16.
                        for (int loopY = 0; loopY < height; loopY++)
                        {
                            imageStream.Position = (long)(loopY) * dataBox.RowPitch;
                            outDataStream.WriteRange(imageStream.ReadRange<byte>(rowStride));
                        }
                    }
                    else
                    {
                        for (var y1 = 0; y1 < height; y1++)
                        {
                            imageStream.Position = (long)(y1) * dataBox.RowPitch;
                            for (var x1 = 0; x1 < width; x1++)
                            {
                                outDataStream.WriteRange(imageStream.ReadRange<byte>(3));
                                imageStream.ReadByte();
                            }
                        }
                    }

                    break;                

                case Format.R16G16B16A16_UNorm:
                    for (var y1 = 0; y1 < height; y1++)
                    {
                        imageStream.Position = (long)(y1) * dataBox.RowPitch;
                        for (var x1 = 0; x1 < width; x1++)
                        {
                            imageStream.ReadByte(); var r = (byte)imageStream.ReadByte();
                            imageStream.ReadByte(); var g = (byte)imageStream.ReadByte();
                            imageStream.ReadByte(); var b = (byte)imageStream.ReadByte();
                            
                            outDataStream.WriteByte(b);
                            outDataStream.WriteByte(g);
                            outDataStream.WriteByte(r);

                            imageStream.ReadByte(); var a = imageStream.ReadByte();
                            if (request.FileFormat == FileFormats.Png)
                            {
                                outDataStream.WriteByte((byte)a);
                            }
                        }
                    }
                    break;

                default:
                {
                    Log.Error($"Screenshot can't export unknown texture format {_currentDesc.Format}");
                    return;
                }
            }

            // Copy the pixels from the buffer to the Wic Bitmap Frame encoder
            bitmapFrameEncode.WritePixels(height, new DataRectangle(outDataStream.DataPointer, rowStride));

            // Commit changes
            bitmapFrameEncode.Commit();
            encoder.Commit();
        }
        catch (Exception e)
        {
            Log.Error($"Screenshot internal image copy failed : {e.Message}");
            return;
        }
        finally
        {
            immediateContext.UnmapSubresource(_readableTexture, 0);
            imageStream.Dispose();
            outDataStream.Dispose();
            bitmapFrameEncode.Dispose();
            encoder.Dispose();
            stream.Dispose();
            LastFilename = request.Filepath;
        }
    }

    private static void PrepareCpuAccessTextures(Texture2DDescription texture2DDescription)
    {
        if (_imagesWithCpuAccess.Count != 0
            && _imagesWithCpuAccess[0].Description.Format == texture2DDescription.Format
            && _imagesWithCpuAccess[0].Description.Width == texture2DDescription.Width
            && _imagesWithCpuAccess[0].Description.Height == texture2DDescription.Height
            && _imagesWithCpuAccess[0].Description.MipLevels == texture2DDescription.MipLevels)
            return;

        _currentDesc = texture2DDescription;
        if (_saveQueue.Count > 0)
        {
            Log.Warning("Cancelling save...");
            _saveQueue.Clear();
            _swapCounter = 0;
        }
        
        Dispose();
        var imageDesc = new Texture2DDescription
                            {
                                BindFlags = BindFlags.None,
                                Format = texture2DDescription.Format,
                                Width = texture2DDescription.Width,
                                Height = texture2DDescription.Height,
                                MipLevels = texture2DDescription.MipLevels,
                                SampleDescription = new SampleDescription(1, 0),
                                Usage = ResourceUsage.Staging,
                                OptionFlags = ResourceOptionFlags.None,
                                CpuAccessFlags = CpuAccessFlags.Read,
                                ArraySize = 1
                            };

        for (var i = 0; i < CpuAccessTextureCount; ++i)
        {
            _imagesWithCpuAccess.Add(new Texture2D(ResourceManager.Device, imageDesc));
        }

    }

    private static int SwapIndex => _swapCounter % CpuAccessTextureCount;

    public static string LastFilename;
    private const int CpuAccessTextureCount = 3;
    private static readonly List<Texture2D> _imagesWithCpuAccess = new();
    private static int _swapCounter;
    private static Texture2D _readableTexture;

    private static readonly List<SaveRequest> _saveQueue = new();
    private static Texture2DDescription _currentDesc;

    private struct SaveRequest
    {
        public int RequestIndex;
        public string Filepath;
        public FileFormats FileFormat;

        public bool IsReady=> RequestIndex == _swapCounter - (CpuAccessTextureCount-2);
        public bool IsObsolete => RequestIndex < _swapCounter - (CpuAccessTextureCount-2);
        
    }

    public static void Dispose()
    {
        foreach (var image in _imagesWithCpuAccess)
            image.Dispose();

        _imagesWithCpuAccess.Clear();
    }
}