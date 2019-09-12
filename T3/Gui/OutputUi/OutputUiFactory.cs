using System;
using System.Collections.Generic;

namespace T3.Gui.OutputUi
{
    public static class OutputUiFactory
    {
        public static Dictionary<Type, Func<IOutputUi>> Entries { get; } = new Dictionary<Type, Func<IOutputUi>>();
    }
}