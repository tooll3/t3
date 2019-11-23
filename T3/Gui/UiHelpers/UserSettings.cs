using System;
using System.Collections.Generic;
using System.Numerics;
using T3.Gui.Graph;

namespace T3.Gui.UiHelpers
{
    /// <summary>
    /// Saves view layout and currently open node 
    /// </summary>
    public static class UserSettings
    {
        public static Dictionary<Guid, ScalableCanvas.CanvasProperties> CanvasPropertiesForSymbols = new Dictionary<Guid, ScalableCanvas.CanvasProperties>();
    }
}