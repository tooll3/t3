using System;
using System.Collections.Generic;
using SharpDX.Direct3D11;
using T3.Core.Logging;
using T3.Core.Resource;

namespace T3.Editor.Gui.Graph.Rendering
{
    /// <summary>
    /// A helper class that manages the creating of srv's for textures
    /// </summary>
    public static class SrvManager
    {
        public static ShaderResourceView GetSrvForTexture(Texture2D texture)
        {
            if (_srvForTextures.TryGetValue(texture, out ShaderResourceView srv))
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
            try
            {
                if ((texture.Description.BindFlags & BindFlags.DepthStencil) > 0)
                    return null; // skip here for depth/stencil to prevent warning below
            
                return new ShaderResourceView(ResourceManager.Device, texture);
            }
            catch (Exception e)
            {
                Log.Warning("SrvManager::CreateNewSrv(...) - Could not create ShaderResourceView for texture.");
                Log.Warning(e.Message);
                return null;
            }
        }

        
        public static void FreeUnusedTextures()
        {
            var keysForRemoval = new List<KeyValuePair<Texture2D, ShaderResourceView>>();
            foreach (var pair in _srvForTextures)
            {
                if (pair.Key.IsDisposed)
                {
                    keysForRemoval.Add(pair);
                }
            }

            foreach (var (texture,srv) in keysForRemoval)
            {
                _srvForTextures.Remove(texture);
                srv.Dispose();
            }
        }

        /// <summary>
        /// Todo: optimize this by disposing obsolete SRVs
        /// </summary>
        private static Dictionary<Texture2D, ShaderResourceView> _srvForTextures = new();
    }
}