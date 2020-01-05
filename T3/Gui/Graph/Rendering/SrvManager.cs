using System;
using System.Collections.Generic;
using SharpDX.Direct3D11;
using T3.Core;
//using System.Resources;
//using SharpDX.Direct3D11;
using T3.Core.Logging;
using T3.Operators.Types;

namespace T3.Gui.Graph.Rendering
{
    /// <summary>
    /// A helper class that manages the creating of srv's for textures
    /// </summary>
    public static class SrvManager
    {
        public static ShaderResourceView GetSrvForTexture(Texture2D texture)
        {
            ShaderResourceView srv;
            if (_srvForTextures.TryGetValue(texture, out srv))
            {
                return srv;
            }
            
            srv = CreateNewSrv(texture);
            if (srv == null)
                return null;
            
            _srvForTextures.Add(texture, srv);
            return srv;
        }

        private static ShaderResourceView CreateNewSrv(Texture2D texture)
        {
            ShaderResourceView srv;
            try
            {
                srv = new ShaderResourceView(ResourceManager.Instance().Device, texture);
            }
            catch (Exception e)
            {
                Log.Warning("ImageOutputCanvas::DrawTexture(...) - Could not create ShaderResourceView for texture.");
                Log.Warning(e.Message);
                return null;
            }
            return srv;
        }

        /// <summary>
        /// Todo: optimize this by disposing obsolete SRVs
        /// </summary>
        private static Dictionary<Texture2D, ShaderResourceView> _srvForTextures = new Dictionary<Texture2D, ShaderResourceView>();
    }
}