using System;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using T3.Core.DataTypes.Vector;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Resource;
using T3.Core.Utils;
using Color = T3.Core.DataTypes.Vector.Color;
using Utilities = T3.Core.Utils.Utilities;
using Vector2 = System.Numerics.Vector2;

namespace T3.Operators.Types.Id_afcd4aad_8c8d_4e59_8e8e_a8c12d312200
{
    public class Edtaa : Instance<Edtaa>
    {
        [Output(Guid = "aa16dd79-5311-4d97-a939-9a8ea82f5996")]
        public readonly Slot<Texture2D> Output = new();


        public Edtaa()
        {
            Output.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var image = InputImage.GetValue(context);
            var imageSrv = InputImageSrv.GetValue(context);

            if (image == null)
            {
                Log.Debug("input not completet", this);
                return;
            }

            var d3DDevice = ResourceManager.Device;
            var immediateContext = d3DDevice.ImmediateContext;

            if (_imageWithCPUAccess == null ||
                _imageWithCPUAccess.Description.Format != image.Description.Format ||
                _imageWithCPUAccess.Description.Width != image.Description.Width ||
                _imageWithCPUAccess.Description.Height != image.Description.Height ||
                _imageWithCPUAccess.Description.MipLevels != image.Description.MipLevels)
            {
                var desc = new Texture2DDescription()
                               {
                                   BindFlags = BindFlags.None,
                                   Format = image.Description.Format,
                                   Width = image.Description.Width,
                                   Height = image.Description.Height,
                                   MipLevels = image.Description.MipLevels,
                                   SampleDescription = new SampleDescription(1, 0),
                                   Usage = ResourceUsage.Staging,
                                   OptionFlags = ResourceOptionFlags.None,
                                   CpuAccessFlags = CpuAccessFlags.Read,
                                   ArraySize = 1
                               };
                Utilities.Dispose(ref _imageWithCPUAccess);
                _imageWithCPUAccess = new Texture2D(d3DDevice, desc);
            }

            if (_distanceFieldImage == null ||
                _distanceFieldImage.Description.Format != image.Description.Format ||
                _distanceFieldImage.Description.Width != image.Description.Width ||
                _distanceFieldImage.Description.Height != image.Description.Height ||
                _distanceFieldImage.Description.MipLevels != image.Description.MipLevels)
            {
                var desc = new Texture2DDescription()
                               {
                                   BindFlags = BindFlags.ShaderResource,
                                   Format = image.Description.Format,
                                   Width = image.Description.Width,
                                   Height = image.Description.Height,
                                   MipLevels = 1,
                                   SampleDescription = new SampleDescription(1, 0),
                                   Usage = ResourceUsage.Dynamic,
                                   OptionFlags = ResourceOptionFlags.None,
                                   CpuAccessFlags = CpuAccessFlags.Write,
                                   ArraySize = 1
                               };
                Utilities.Dispose(ref _distanceFieldImage);
                _distanceFieldImage = new Texture2D(d3DDevice, desc);
            }

            // if (Changed)
            {
                immediateContext.CopyResource(image, _imageWithCPUAccess);
                int width = image.Description.Width;
                int height = image.Description.Height;

                if (_data == null || _data.Length != width * height)
                {
                    _data = new float[width * height];
                    _xDist = new short[width * height];
                    _yDist = new short[width * height];
                    _gradients = new Vector2[width * height];
                }

                DataStream sourceStream;
                var sourceDataBox =
                    immediateContext.MapSubresource(_imageWithCPUAccess, 0, 0, MapMode.Read, SharpDX.Direct3D11.MapFlags.None, out sourceStream);

                // Convert img into float (data)
                using (sourceStream)
                {
                    sourceStream.Position = 0;
                    float minValue = 255, maxValue = -255;
                    for (int y = 0; y < height; ++y)
                    {
                        for (int x = 0; x < width; ++x)
                        {
                            var color = new Color(new Byte4(sourceStream.Read<Int32>()));
                            float v = color.R;
                            _data[y * width + x] = v;
                            if (v > maxValue)
                                maxValue = v;
                            if (v < minValue)
                                minValue = v;
                        }

                        sourceStream.Position += sourceDataBox.RowPitch - width * 4;
                    }

                    // Rescale image levels between 0 and 1
                    for (int i = 0; i < width * height; ++i)
                    {
                        _data[i] = (_data[i] - minValue) / maxValue;
                    }

                    // transform background (black pixels)
                    ComputeGradient(_data, width, height);
                    var outside = Edtaa3(_data, height, width);

                    // transform forground (white pixels)
                    for (int i = 0; i < width * height; ++i)
                        _data[i] = 1 - _data[i]; // invert input
                    ComputeGradient(_data, width, height);
                    var inside = Edtaa3(_data, height, width);

                    // write resulting distance field to target texture
                    DataStream destinationStream;
                    var destinationDataBox = immediateContext.MapSubresource(_distanceFieldImage, 0, 0, MapMode.WriteDiscard,
                                                                             SharpDX.Direct3D11.MapFlags.None, out destinationStream);
                    using (destinationStream)
                    {
                        sourceStream.Position = 0;
                        destinationStream.Position = 0;
                        for (int y = 0; y < height; y++)
                        {
                            for (int x = 0; x < width; x++)
                            {
                                int i = y * width + x;
                                // distmap = outside - inside; % Bipolar distance field
                                _ = sourceStream.Read<Int32>();
                                outside[i] = MathUtils.Clamp(128.0f + (outside[i] - inside[i]) * 16.0f, 0.0f, 255.0f);
                                //color.Alpha = (255 - (byte) outside[i])/255.0f;
                                float f = (255 - (byte)outside[i]) / 255.0f;
                                float alpha = 1 - _data[i];
                                {
                                    // do alpha dilatation
                                    const int range = 1;
                                    int xs = Math.Max(x - range, 0);
                                    int xe = Math.Min(x + range, width - 1);
                                    int ys = Math.Max(y - range, 0);
                                    int ye = Math.Min(y + range, height - 1);
                                    for (int yy = ys; yy <= ye; yy++)
                                    {
                                        for (int xx = xs; xx <= xe; xx++)
                                        {
                                            alpha = Math.Max(alpha, 1 - _data[yy*width + xx]);
                                        }
                                    }
                                }

                                var color = new Color(f, f, f, alpha);

                                destinationStream.Write(color.ToByte4());
                            }

                            destinationStream.Position += destinationDataBox.RowPitch - width * 4;
                        }

                        immediateContext.UnmapSubresource(_distanceFieldImage, 0);
                    }

                    immediateContext.UnmapSubresource(_imageWithCPUAccess, 0);
                }

                // Changed = false;
            }

            Output.Value = _distanceFieldImage;
        }

        // computes gradients of input image into _gradients
        private void ComputeGradient(float[] img, int w, int h)
        {
            const float SQRT2 = 1.4142136f;
            for (int i = 1; i < h - 1; i++)
            {
                // Avoid edges where the kernels would spill over
                for (int j = 1; j < w - 1; j++)
                {
                    int k = i * w + j;
                    if ((img[k] > 0.0) && (img[k] < 1.0))
                    {
                        // Compute gradient for edge pixels only
                        _gradients[k].X = (float)(-img[k - w - 1] - SQRT2 * img[k - 1] - img[k + w - 1] + img[k - w + 1] + SQRT2 * img[k + 1] + img[k + w + 1]);
                        _gradients[k].Y = (float)(-img[k - w - 1] - SQRT2 * img[k - w] - img[k + w - 1] + img[k - w + 1] + SQRT2 * img[k + w] + img[k + w + 1]);
                        var gradientLength = _gradients[k].LengthSquared();
                        if (gradientLength > 0.0)
                        {
                            // Avoid division by zero
                            _gradients[k] /= (float)Math.Sqrt(gradientLength);
                        }
                    }
                }
            }
        }

        private float EdgeDf(Vector2 g, float a)
        {
            float df;

            if ((g.X == 0) || (g.Y == 0))
            {
                // Either A) gu or gv are zero, or B) both
                df = 0.5f - a; // Linear approximation is A) correct or B) a fair guess
            }
            else
            {
                float gradientLength = (float)Math.Sqrt(g.X * g.X + g.Y * g.Y);
                if (gradientLength > 0.0)
                {
                    g.X = g.X / gradientLength;
                    g.Y = g.Y / gradientLength;
                }

                g.X = Math.Abs(g.X);
                g.Y = Math.Abs(g.Y);
                if (g.X < g.Y)
                {
                    Utilities.Swap(ref g.X, ref g.Y);
                }

                float a1 = 0.5f * g.Y / g.X;
                if (a < a1)
                {
                    // 0 <= a < a1
                    df = 0.5f * (g.X + g.Y) - (float)Math.Sqrt(2.0f * g.X * g.Y * a);
                }
                else if (a < (1.0 - a1))
                {
                    // a1 <= a <= 1-a1
                    df = (0.5f - a) * g.X;
                }
                else
                {
                    // 1-a1 < a <= 1
                    df = -0.5f * (g.X + g.Y) + (float)Math.Sqrt(2.0f * g.X * g.Y * (1.0f - a));
                }
            }

            return df;
        }

        private float DistAA3(float[] img, int w, int c, int xc, int yc, int xi, int yi)
        {
            int closest = c - xc - yc * w; // Index to the edge pixel pointed to from c
            float a = img[closest]; // Grayscale value at the edge pixel
            var gx = _gradients[closest]; // gradient component at the edge pixel

            a = MathUtils.Clamp(a, 0.0f, 1.0f); // Clip grayscale values outside the range [0,1]
            if (a == 0.0f)
                return 1000000.0f; // Not an object pixel, return "very far" ("don't know yet")

            var dx = new Vector2(xi, yi);
            float di = dx.Length(); // Length of integer vector, like a traditional EDT
            float df;
            if (di == 0.0f)
            {
                // Use local gradient only at edges
                // Estimate based on local gradient only
                df = EdgeDf(gx, a);
            }
            else
            {
                // Estimate gradient based on direction to edge (accurate for large di)
                df = EdgeDf(dx, a);
            }

            return di + df; // Same metric as edtaa2, except at edges (where di=0)
        }

        private float[] Edtaa3(float[] img, int w, int h)
        {
            // Initialize the distance images
            var dist = new float[w * h];

            for (int i = 0; i < w * h; i++)
            {
                _xDist[i] = 0; // At first, all pixels point to
                _yDist[i] = 0; // themselves as the closest known.
                if (img[i] <= 0.0f)
                {
                    dist[i] = 1000000.0f; // Big value, means "not set yet"
                }
                else if (img[i] < 1.0f)
                {
                    dist[i] = EdgeDf(_gradients[i], img[i]); // Gradient-assisted estimate
                }
                else
                {
                    dist[i] = 0.0f; // Inside the object
                }
            }

            // Perform the transformation
            bool changed;
            do
            {
                changed = false;

                // Scan rows, except first row
                float olddist;
                int x;
                for (int y = 1; y < h; y++)
                {
                    // move index to leftmost pixel of current row
                    int i = y * w;

                    // scan right, propagate distances from above & left 
                    // Leftmost pixel is special, has no left neighbors 
                    olddist = dist[i];
                    if (olddist > 0) // If non-zero distance or not set yet
                    {
                        changed |= UpdateDist(img, dist, i, width: w, xOffset: 0, yOffset: -1, olddist: ref olddist); // pixel up
                        changed |= UpdateDist(img, dist, i, width: w, xOffset: 1, yOffset: -1, olddist: ref olddist); // pixel upper right
                    }

                    i++;

                    // Middle pixels have all neighbors
                    for (x = 1; x < w - 1; x++, i++)
                    {
                        olddist = dist[i];
                        if (olddist <= 0)
                            continue; // No need to update further

                        changed |= UpdateDist(img, dist, i, width: w, xOffset: -1, yOffset: 0, olddist: ref olddist); // pixel left
                        changed |= UpdateDist(img, dist, i, width: w, xOffset: -1, yOffset: -1, olddist: ref olddist); // pixel upper left
                        changed |= UpdateDist(img, dist, i, width: w, xOffset: 0, yOffset: -1, olddist: ref olddist); // pixel upper
                        changed |= UpdateDist(img, dist, i, width: w, xOffset: 1, yOffset: -1, olddist: ref olddist); // pixel upper right
                    }

                    // Rightmost pixel of row is special, has no right neighbors
                    olddist = dist[i];
                    if (olddist > 0) // If not already zero distance
                    {
                        changed |= UpdateDist(img, dist, i, width: w, xOffset: -1, yOffset: 0, olddist: ref olddist); // pixel left
                        changed |= UpdateDist(img, dist, i, width: w, xOffset: -1, yOffset: -1, olddist: ref olddist); // pixel upper left
                        changed |= UpdateDist(img, dist, i, width: w, xOffset: 0, yOffset: -1, olddist: ref olddist); // pixel upper
                    }

                    // Move index to second rightmost pixel of current row.
                    // Rightmost pixel is skipped, it has no right neighbor.
                    i = y * w + w - 2;

                    // scan left, propagate distance from right
                    for (x = w - 2; x >= 0; x--, i--)
                    {
                        olddist = dist[i];
                        if (olddist <= 0)
                            continue; // Already zero distance

                        changed |= UpdateDist(img, dist, i, width: w, xOffset: 1, yOffset: 0, olddist: ref olddist); // pixel right
                    }
                }

                // Scan rows in reverse order, except last row 
                for (int y = h - 2; y >= 0; y--)
                {
                    // move index to rightmost pixel of current row
                    int i = y * w + w - 1;

                    // Scan left, propagate distances from below & right

                    // Rightmost pixel is special, has no right neighbors
                    olddist = dist[i];
                    if (olddist > 0) // If not already zero distance
                    {
                        changed |= UpdateDist(img, dist, i, width: w, xOffset: 0, yOffset: 1, olddist: ref olddist); // pixel down
                        changed |= UpdateDist(img, dist, i, width: w, xOffset: -1, yOffset: 1, olddist: ref olddist); // pixel down left
                    }

                    i--;

                    // Middle pixels have all neighbors
                    for (x = w - 2; x > 0; x--, i--)
                    {
                        olddist = dist[i];
                        if (olddist <= 0)
                            continue; // Already zero distance

                        changed |= UpdateDist(img, dist, i, width: w, xOffset: 1, yOffset: 0, olddist: ref olddist); // pixel right
                        changed |= UpdateDist(img, dist, i, width: w, xOffset: 1, yOffset: 1, olddist: ref olddist); // pixel down right
                        changed |= UpdateDist(img, dist, i, width: w, xOffset: 0, yOffset: 1, olddist: ref olddist); // pixel down
                        changed |= UpdateDist(img, dist, i, width: w, xOffset: -1, yOffset: 1, olddist: ref olddist); // pixel down left
                    }

                    // Leftmost pixel is special, has no left neighbors
                    olddist = dist[i];
                    if (olddist > 0) // If not already zero distance
                    {
                        changed |= UpdateDist(img, dist, i, width: w, xOffset: 1, yOffset: 0, olddist: ref olddist); // pixel right
                        changed |= UpdateDist(img, dist, i, width: w, xOffset: 1, yOffset: 1, olddist: ref olddist); // pixel down right
                        changed |= UpdateDist(img, dist, i, width: w, xOffset: 0, yOffset: 1, olddist: ref olddist); // pixel down
                    }

                    // Move index to second leftmost pixel of current row.
                    // Leftmost pixel is skipped, it has no left neighbor.
                    i = y * w + 1;
                    for (x = 1; x < w; x++, i++)
                    {
                        // scan right, propagate distance from left
                        olddist = dist[i];
                        if (olddist <= 0)
                            continue; // Already zero distance

                        changed |= UpdateDist(img, dist, i, width: w, xOffset: -1, yOffset: 0, olddist: ref olddist); // pixel left
                    }
                }
            }
            while (changed); // Sweep until no more updates are made

            for (int i = 0; i < w * h; ++i)
            {
                if (dist[i] < 0)
                {
                    dist[i] = 0.0f;
                }
            }

            return dist;
        }

        private bool UpdateDist(float[] img, float[] dist, int i, int width, int xOffset, int yOffset, ref float olddist)
        {
            int c = i + yOffset * width + xOffset; // Index of candidate for testing
            int cdistx = _xDist[c];
            int cdisty = _yDist[c];
            int newdistx = cdistx - xOffset;
            int newdisty = cdisty - yOffset;
            float newdist = DistAA3(img, width, c, cdistx, cdisty, newdistx, newdisty);
            if (newdist >= olddist)
                return false;

            _xDist[i] = (short)newdistx;
            _yDist[i] = (short)newdisty;
            dist[i] = newdist;
            olddist = newdist;
            return true;
        }

        Texture2D _imageWithCPUAccess;
        Texture2D _distanceFieldImage;
        private short[] _xDist;
        private short[] _yDist;
        private Vector2[] _gradients;
        private float[] _data;

        private void UpdateOld(EvaluationContext context)
        {
            // var resourceManager = ResourceManager.Instance();
            // if (Path.DirtyFlag.IsDirty)
            // { 
            //     string imagePath = Path.GetValue(context);
            //     try
            //     {
            //         (_textureResId, _srvResId) = resourceManager.CreateTextureFromFile(imagePath, () =>
            //                                                                                       {
            //                                                                                           Texture.DirtyFlag.Invalidate();
            //                                                                                           ShaderResourceView.DirtyFlag.Invalidate();
            //                                                                                       });
            //         if (resourceManager.Resources.TryGetValue(_textureResId, out var resource1) && resource1 is TextureResource textureResource)
            //             Texture.Value = textureResource.Texture;
            //         if (resourceManager.Resources.TryGetValue(_srvResId, out var resource2) && resource2 is ShaderResourceViewResource srvResource)
            //             ShaderResourceView.Value = srvResource.ShaderResourceView;
            //     }
            //     catch (Exception e)
            //     {
            //         Log.Error($"Filed to create texture from file '{imagePath}':" + e.Message);
            //     }
            // }
            // else
            // {
            //     resourceManager.UpdateTextureFromFile(_textureResId, Path.Value, ref Texture.Value);
            //     resourceManager.CreateShaderResourceView(_textureResId, "", ref ShaderResourceView.Value);
            // }
            //
            // try
            // {
            //     if (ShaderResourceView.Value != null)
            //         ResourceManager.Device.ImmediateContext.GenerateMips(ShaderResourceView.Value);
            // }
            // catch (Exception e)
            // {
            //     Log.Error($"Failed to generate mipmaps for texture {Path.GetValue(context)}:" + e);
            // }
            //
            // Texture.DirtyFlag.Clear();
            // ShaderResourceView.DirtyFlag.Clear();
        }

        [Input(Guid = "7b091198-57c7-40b3-8b96-2ab8018c9f6f")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> InputImage = new();

        [Input(Guid = "1b3b0049-1ba2-4341-ae4e-cfdd4ddc7d20")]
        public readonly InputSlot<SharpDX.Direct3D11.ShaderResourceView> InputImageSrv = new();
    }
}