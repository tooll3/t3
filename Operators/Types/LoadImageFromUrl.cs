using System;
using SharpDX.Direct3D11;
using T3.Core;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using System.IO;
using System.Net;
using SharpDX.WIC;

namespace T3.Operators.Types.Id_61ec6355_bd7d_4abb_aa44_b01b7d658e23
{
    public class LoadImageFromUrl : Instance<LoadImageFromUrl>
    {
        [Output(Guid = "316fe874-5178-4068-a233-6c6ecf70c49e")]
        public readonly Slot<Texture2D> Texture = new Slot<Texture2D>();

        [Output(Guid = "a843ab64-0b99-4c4f-9644-cc9bb771981d")]
        public readonly Slot<ShaderResourceView> ShaderResourceView = new Slot<ShaderResourceView>();

        public LoadImageFromUrl()
        {
            Texture.UpdateAction = UpdateShaderResourceView;
            ShaderResourceView.UpdateAction = UpdateShaderResourceView;
        }

        private void UpdateShaderResourceView(EvaluationContext context)
        {
            var url = Url.GetValue(context);
            if (url != _url && !string.IsNullOrEmpty(url))
            {
                _url = url;
                if (_webRequest == null)
                {
                    Dispose();
                    try
                    {
                        _webRequest = WebRequest.Create(url);
                        _url = url;
                        DoGetRequest(_webRequest, HandleResponse);
                    }
                    catch (Exception e)
                    {
                        Log.Error($"failed to start request: {e.Message}", this.SymbolChildId);
                    }
                }
            }

            Texture.Value = _image;
        }

        private void DoGetRequest(WebRequest request, Action<WebResponse> responseAction)
        {
            Action wrapperAction = () =>
                                   {
                                       request.BeginGetResponse((iAsyncResult) =>
                                                                {
                                                                    try
                                                                    {
                                                                        WebRequest req = (WebRequest)iAsyncResult.AsyncState;
                                                                        var response = req.EndGetResponse(iAsyncResult);
                                                                        responseAction(response);
                                                                    }
                                                                    catch (Exception e)
                                                                    {
                                                                        Log.Error("Request failed " + e.Message, SymbolChildId);
                                                                    }

                                                                    _webRequest = null;
                                                                },
                                                                request);
                                   };
            wrapperAction.BeginInvoke((asyncResult) =>
                                      {
                                          var action = (Action)asyncResult.AsyncState;
                                          action.EndInvoke(asyncResult);
                                      },
                                      wrapperAction);
        }

        void HandleResponse(WebResponse response)
        {
            lock (this)
            {
                _image = null;
                using (var memStream = new MemoryStream())
                using (Stream streamResponse = response.GetResponseStream())
                {
                    try
                    {
                        if (streamResponse != null)
                        {
                            streamResponse.CopyTo(memStream);

                            Log.Debug($"Finished loading URL {_url}", SymbolChildId);

                            ImagingFactory factory = new ImagingFactory();
                            memStream.Position = 0;
                            var bitmapDecoder = new BitmapDecoder(factory, memStream, DecodeOptions.CacheOnDemand);
                            var formatConverter = new FormatConverter(factory);
                            var bitmapFrameDecode = bitmapDecoder.GetFrame(0);
                            formatConverter.Initialize(bitmapFrameDecode, SharpDX.WIC.PixelFormat.Format32bppPRGBA, BitmapDitherType.None, null, 0.0,
                                                       BitmapPaletteType.Custom);

                            _image?.Dispose();
                            _image = ResourceManager.CreateTexture2DFromBitmap(ResourceManager.Instance().Device, formatConverter);
                            _image.DebugName = _url;
                            bitmapFrameDecode.Dispose();
                            bitmapDecoder.Dispose();
                            formatConverter.Dispose();
                            factory.Dispose();
                            Texture.DirtyFlag.Invalidate();
                        }
                    }
                    catch (Exception e)
                    {
                        Log.Info($"Failed to load URL : {e.Message}");
                    }

                    response.Close();
                }
            }
        }

        private WebRequest _webRequest;
        private Texture2D _image;
        private string _url;

        [Input(Guid = "21b2e219-0b2a-4323-b288-f39ed791e676")]
        public readonly InputSlot<string> Url = new InputSlot<string>();
    }
}