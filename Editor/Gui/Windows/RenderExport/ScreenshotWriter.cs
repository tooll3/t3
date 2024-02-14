using System;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.IO;
using SharpDX.WIC;
using T3.Core.Logging;
using T3.Core.Resource;

namespace T3.Editor.Gui.Windows.RenderExport;

public static class ScreenshotWriter
{
    public enum FileFormats
    {
        Png,
        Jpg,
    }

    public static string LastFilename { get; private set; }

    public static bool StartSavingToFile(Texture2D gpuTexture, string filepath, FileFormats format)
    {
        return TextureReadAccess.InitiateRead(gpuTexture, OnReadComplete, filepath);
    }
    
    private static void OnReadComplete(TextureReadAccess.ReadRequestItem request)
    {
        var immediateContext = ResourceManager.Device.ImmediateContext;
        
        var dataBox = immediateContext.MapSubresource(request.CpuAccessTexture,
                                                      0,
                                                      0,
                                                      MapMode.Read,
                                                      SharpDX.Direct3D11.MapFlags.None,
                                                      out var imageStream);
        using var dataStream = imageStream;
        
        var width = request.CpuAccessTexture.Description.Width;
        var height = request.CpuAccessTexture.Description.Height;
        var factory = new ImagingFactory();

        var stream = new WICStream(factory, request.Filepath, NativeFileAccess.Write);

        // Initialize a Jpeg encoder with this stream
        BitmapEncoder encoder = request.FileFormat == FileFormats.Png
                                    ? new PngBitmapEncoder(factory)
                                    : new JpegBitmapEncoder(factory);
        encoder.Initialize(stream);

        // Create a Frame encoder
        var bitmapFrameEncode = new BitmapFrameEncode(encoder);
        bitmapFrameEncode.Initialize();
        bitmapFrameEncode.SetSize(width, height);
        var formatId = PixelFormat.Format32bppRGBA;
        bitmapFrameEncode.SetPixelFormat(ref formatId);

        var rowStride = PixelFormat.GetStride(formatId, width);
        var outBufferSize = height * rowStride;
        var outDataStream = new DataStream(outBufferSize, true, true);
        
        try
        {
            if (request.FileFormat == FileFormats.Png)
            {
                // Note: dataBox.RowPitch and outputStream.RowPitch can diverge if width is not divisible by 16.
                for (var loopY = 0; loopY < height; loopY++)
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
            
            // Copy the pixels from the buffer to the Wic Bitmap Frame encoder
            bitmapFrameEncode.WritePixels(height, new DataRectangle(outDataStream.DataPointer, rowStride));

            // Commit changes
            bitmapFrameEncode.Commit();
            encoder.Commit();
        }
        catch (Exception e)
        {
            Log.Error($"Screenshot internal image copy failed : {e.Message}");
        }
        finally
        {
            imageStream.Dispose();
            outDataStream.Dispose();
            bitmapFrameEncode.Dispose();
            encoder.Dispose();
            stream.Dispose();
            LastFilename = request.Filepath;
        }
    }

    // skip a certain number of images at the beginning since the
    // final content will only appear after several buffer flips
    public const int SkipImages = 2;
}