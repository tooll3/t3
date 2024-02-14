using System;
using System.Collections.Generic;

namespace T3.Editor.Gui.OutputUi
{
    public static class OutputUiFactory
    {
        public static Dictionary<Type, Func<IOutputUi>> Entries { get; } = new();
    }
}